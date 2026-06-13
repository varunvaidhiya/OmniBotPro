"""Hardware abstraction, benchmark suites, and reporters."""

from __future__ import annotations

import json
import os
import tempfile
import unittest
import urllib.request

from learning_engine.benchmarks.ai_benchmark import (
    DatasetIOBenchmark,
    InferenceBenchmark,
    TrainingBenchmark,
    episodes_for_training,
)
from learning_engine.benchmarks.monitors import ResourceMonitor
from learning_engine.benchmarks.reporters import JsonFileReporter, PrometheusReporter
from learning_engine.benchmarks.system_probe import probe
from learning_engine.hardware import (
    BUILTIN_PROFILES,
    AcceleratorType,
    detect_accelerators,
    detect_profile,
    get_profile,
    onnx_providers,
    primary_accelerator,
    resolve_device,
)
from learning_engine.policies.base import RandomPolicy
from learning_engine.policies.trainers import NoOpTrainer


class TestDeviceDetection(unittest.TestCase):
    def test_resolve_device(self):
        self.assertEqual(resolve_device("cpu"), "cpu")
        self.assertEqual(resolve_device("cuda:1"), "cuda:1")  # explicit passthrough
        self.assertIn(resolve_device("auto"), ("cpu", "cuda", "mps"))

    def test_accelerator_detection(self):
        accels = detect_accelerators()
        self.assertGreaterEqual(len(accels), 1)
        self.assertIsInstance(primary_accelerator().type, AcceleratorType)

    def test_onnx_providers_always_has_cpu_fallback(self):
        providers = onnx_providers()
        self.assertTrue(providers)
        self.assertTrue(all(p.endswith("ExecutionProvider") for p in providers))
        # Explicit accelerator override
        self.assertEqual(onnx_providers(AcceleratorType.CPU), ["CPUExecutionProvider"])

    def test_onnx_provider_preference_policy(self):
        # The preference table encodes the deployment-vs-dev intent and is
        # asserted directly (the public onnx_providers() filters by the
        # installed ORT build, which may have no GPU EPs in CI).
        from learning_engine.hardware.device import _ONNX_PROVIDER_PREFERENCE

        # Discrete GPU: CUDA-first, TensorRT opt-in (avoids engine-build cost
        # on a dev/training box).
        gpu = _ONNX_PROVIDER_PREFERENCE[AcceleratorType.NVIDIA_GPU]
        self.assertEqual(gpu[0], "CUDAExecutionProvider")
        self.assertNotIn("TensorrtExecutionProvider", gpu)
        # Jetson is a deployment target: TensorRT-first.
        jetson = _ONNX_PROVIDER_PREFERENCE[AcceleratorType.JETSON]
        self.assertEqual(jetson[0], "TensorrtExecutionProvider")


class TestProfiles(unittest.TestCase):
    def test_builtin_profiles_roles(self):
        for profile in BUILTIN_PROFILES.values():
            self.assertTrue(
                profile.node_for("inference") or profile.node_for("control"),
                f"{profile.name} has no useful roles",
            )
        jetson = get_profile("jetson_single")
        self.assertEqual(jetson.node_for("inference").name, "jetson")
        self.assertIn(jetson.device_for("inference"), ("cpu", "cuda", "mps"))

    def test_unknown_profile_raises(self):
        with self.assertRaises(KeyError):
            get_profile("quantum_mainframe")

    def test_env_override(self):
        os.environ["OMNIBOT_HW_PROFILE"] = "mac_dev"
        try:
            self.assertEqual(detect_profile().name, "mac_dev")
        finally:
            del os.environ["OMNIBOT_HW_PROFILE"]
        self.assertIn(detect_profile().name, BUILTIN_PROFILES)


class TestSystemProbe(unittest.TestCase):
    def test_probe_fields(self):
        info = probe()
        self.assertTrue(info.hostname)
        self.assertTrue(info.cpu)
        self.assertGreaterEqual(info.cpu_cores, 1)
        self.assertIn(info.torch_device, ("cpu", "cuda", "mps"))
        self.assertIn(info.hw_profile, BUILTIN_PROFILES)
        label = info.label()
        self.assertNotIn(" ", label)
        self.assertNotIn("--", label)


class TestMonitorsAndBenchmarks(unittest.TestCase):
    def test_resource_monitor(self):
        import time

        with ResourceMonitor(interval_s=0.05) as mon:
            time.sleep(0.2)
        summary = mon.summary()
        self.assertGreaterEqual(summary["resource/samples"], 1)

    def test_inference_benchmark(self):
        result = InferenceBenchmark(
            RandomPolicy(action_dim=9), n=20, warmup=2, budget_ms=100.0
        ).run()
        m = result.metrics
        for key in (
            "latency_ms_mean",
            "latency_ms_p95",
            "achievable_hz",
            "cold_latency_ms",
            "within_budget",
        ):
            self.assertIn(key, m)
        self.assertLessEqual(m["latency_ms_p50"], m["latency_ms_max"])
        self.assertEqual(m["within_budget"], 1.0)  # random policy is fast
        self.assertEqual(result.system.to_dict()["hostname"], probe().hostname)

    def test_training_benchmark(self):
        result = TrainingBenchmark(NoOpTrainer(), episodes_for_training(5, 10)).run()
        self.assertEqual(result.metrics["transitions"], 50.0)
        self.assertGreater(result.metrics["transitions_per_s"], 0)

    def test_dataset_io_benchmark(self):
        with tempfile.TemporaryDirectory() as tmp:
            result = DatasetIOBenchmark(
                tmp, episodes=2, steps=5, with_images=False
            ).run()
            self.assertGreater(result.metrics["write_eps_per_s"], 0)
            self.assertGreater(result.metrics["read_steps_per_s"], 0)


class TestReporters(unittest.TestCase):
    def _result(self):
        return InferenceBenchmark(RandomPolicy(action_dim=9), n=5, warmup=0).run()

    def test_json_reporter(self):
        with tempfile.TemporaryDirectory() as tmp:
            reporter = JsonFileReporter(tmp)
            reporter.publish(self._result())
            reporter.log_metrics({"eval_success_rate": 0.5}, step=1)
            files = os.listdir(tmp)
            self.assertTrue(any(f.endswith(".json") for f in files))
            self.assertIn("loop_metrics.jsonl", files)
            doc = json.loads(
                open(
                    os.path.join(tmp, next(f for f in files if f.endswith(".json")))
                ).read()
            )
            self.assertIn("system", doc)
            self.assertIn("latency_ms_p95", doc["metrics"])

    def test_prometheus_http_and_textfile(self):
        with tempfile.TemporaryDirectory() as tmp:
            textfile = os.path.join(tmp, "bench.prom")
            reporter = PrometheusReporter(port=0, textfile=textfile)
            try:
                reporter.publish(self._result())
                reporter.log_metrics({"eval_success_rate": 0.75}, step=3)
                # Textfile mode
                text = open(textfile).read()
                self.assertIn("omnibot_bench_latency_ms_p95", text)
                self.assertIn('benchmark="inference/RandomPolicy"', text)
                self.assertIn("omnibot_bench_loop_eval_success_rate 0.75", text)
                # HTTP /metrics mode (ephemeral port)
                self.assertGreater(reporter.port, 0)
                body = (
                    urllib.request.urlopen(
                        f"http://127.0.0.1:{reporter.port}/metrics", timeout=5
                    )
                    .read()
                    .decode()
                )
                self.assertIn("# TYPE omnibot_bench_latency_ms_p95 gauge", body)
            finally:
                reporter.close()


class TestLoopReporterHook(unittest.TestCase):
    def test_loop_publishes_metrics(self):
        from learning_engine.data.collectors import SimRolloutCollector
        from learning_engine.data.replay_dataset import ReplayDataset
        from learning_engine.evaluation.self_eval import HeuristicSelfEvaluator
        from learning_engine.loop.post_training_loop import (
            LoopComponents,
            PostTrainingLoop,
        )
        from learning_engine.rewards.engine import RewardEngine

        from .helpers import DummyEnv

        with tempfile.TemporaryDirectory() as tmp:
            policy = RandomPolicy(action_dim=9, scale=0.05, seed=0)
            dataset = ReplayDataset(f"{tmp}/ds", store_images=False)
            loop = PostTrainingLoop(
                LoopComponents(
                    collectors=[SimRolloutCollector(DummyEnv(), policy, max_steps=5)],
                    dataset=dataset,
                    reward_engine=RewardEngine.from_config([{"name": "task_success"}]),
                    trainer=NoOpTrainer(),
                    evaluators=[HeuristicSelfEvaluator()],
                ),
                collect_per_iteration=2,
                reporters=[JsonFileReporter(f"{tmp}/reports")],
            )
            loop.run_iteration()
            lines = open(f"{tmp}/reports/loop_metrics.jsonl").read().splitlines()
            doc = json.loads(lines[0])
            self.assertEqual(doc["step"], 1)
            self.assertEqual(doc["episodes_collected"], 2.0)
            self.assertIn("outcome_success", doc)


if __name__ == "__main__":
    unittest.main()
