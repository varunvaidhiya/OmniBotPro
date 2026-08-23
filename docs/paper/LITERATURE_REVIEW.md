# Literature Review: Affordable Distributed Mobile Manipulation with VLA Policies

## Scope and review question

This review supports the first OmniBot manuscript, provisionally titled **“OmniBot: An Affordable, Distributed Embodied-AI Platform for Mobile Manipulation with Vision-Language-Action Models.”** It addresses a specific systems question:

> **What evidence exists for open, affordable mobile manipulators, VLA policy deployment, ROS 2 navigation, safety-constrained learned control, and reproducible robotics-performance benchmarking—and what integrated evidence is still missing for a low-cost edge/GPU mobile-manipulation platform?**

The review is intentionally limited to sources that matter for the first systems paper. It is not a survey of all mobile manipulation or VLA research. It separates three categories that are frequently conflated: **hardware accessibility**, **policy/model capability**, and **end-to-end deployment evidence**. This distinction is essential because success in one category does not demonstrate success in the others.

## 1. Affordable and open mobile manipulators

Open systems have substantially advanced affordable mobile manipulation. **Mobile ALOHA** extends the ALOHA low-cost bimanual teleoperation line to mobile whole-body manipulation. The authors use a differential-drive AgileX Tracer base, onboard laptop compute with an RTX 3070 Ti, three RGB cameras, and four arms; their central contribution is whole-body teleoperation and the data it enables for long-horizon household manipulation [1]. This establishes an important precedent: low-cost components and a consumer GPU can support compelling mobile-manipulation research. It does **not**, however, directly test a Raspberry Pi edge node plus remote GPU policy server, ROS 2 service timing, or SLO-gated end-to-end deployment measurements.

**AhaRobot** is an especially important contemporary comparator because it reports an open bimanual mobile manipulator built from off-the-shelf components. It reports a hardware budget of approximately USD 1,000 without computational resources and approximately USD 2,000 with a Mini-ITX computer and RTX 4060, while emphasizing dual-motor backlash compensation, friction compensation, and its RoboPilot remote teleoperation method [2]. AhaRobot eliminates any defensible “first low-cost open mobile manipulator” claim for OmniBot. A valid comparison must instead describe precise architectural differences and account for whether compute, sensors, battery, and teleoperation hardware are included in each published total.

**TidyBot++** provides the closest base-kinematics comparison. It is an open mobile manipulator designed for robot learning and data collection using powered casters that the authors describe as fully holonomic. The work combines a phone teleoperation interface with learned policies for household tasks and reports a USD 5–6k base cost [3]. OmniBot’s mecanum base is omnidirectional, but the manuscript should not casually treat “mecanum,” “omnidirectional,” and “holonomic” as interchangeable. It should document the wheel-kinematic model, commanded degrees of freedom, and observed control behaviour. TidyBot++ also demonstrates that holonomic/omnidirectional mobility and open hardware alone are insufficient novelty claims.

| Work | Platform focus | Relevant evidence | What it does **not** establish for OmniBot |
|---|---|---|---|
| Mobile ALOHA [1] | Bimanual whole-body teleoperation and learned long-horizon tasks | Open low-cost mobile manipulation with on-board consumer-GPU compute | Pi-to-GPU service split, ROS 2 end-to-end timing, platform-specific SLO protocol |
| AhaRobot [2] | Low-cost bimanual hardware, control compensation, remote teleoperation | Strong contemporary affordability benchmark; explicit hardware/compute accounting distinction | Single-arm mecanum stack, separate edge/GPU topology, released full-pipeline latency data |
| TidyBot++ [3] | Holonomic open mobile manipulator and phone teleoperation | Open holonomic base tailored for data collection and policy learning | Mecanum-specific design, networked VLA serving, system-wide timing/resource comparison |
| OmniBot (proposed) | Single-arm mecanum platform with Pi edge and GPU policy server | Proposed reference implementation and measurement protocol | Must still provide real results; no results can be inferred from architecture alone |

The direct implication is that the OmniBot paper must avoid an unqualified “cheapest” or “first” title/claim. Its contribution should be framed as an **open reference configuration and measured integration study**, provided the release includes transparent bill-of-materials (BOM) accounting and real hardware data.

## 2. Vision-language-action policies and accessible inference

The policy ecosystem motivates OmniBot but is not the locus of its novelty. **RT-1** showed that a transformer policy could scale real-world robotic control [4]. **RT-2** then integrated vision-language models and robot trajectories by representing robot actions as tokens, reporting broad real-robot evaluation and emergent semantic generalization [5]. These papers established the VLA research paradigm: model parameters and training mixtures can encode useful visual and linguistic priors for action generation.

Open-source work changes the deployment landscape. **OpenVLA** introduces a 7B open VLA trained on 970k real-world demonstrations from Open X-Embodiment. It evaluates generalist manipulation, fine-tuning, and quantized serving on consumer GPUs [6]. **Octo** provides an open generalist robot policy designed for adaptation across robots and tasks [7]. **SmolVLA** explicitly targets affordable and efficient robotics, describing training on a single GPU, deployment on consumer GPUs or CPUs, and asynchronous inference that separates action prediction from execution [8]. These papers make any claim that OmniBot is the first consumer-hardware VLA deployment untenable.

The defensible systems question is different: given a particular policy/checkpoint and a particular robot, **what happens when the robot-resident computer is responsible for I/O, perception and actuation while policy inference runs on a networked GPU?** This must be answered with matched local-edge and distributed conditions. A paper cannot replace a local OpenVLA measurement with a local SmolVLA measurement and call the result a direct deployment comparison. Model class, checkpoint, quantization, input tensor, action representation, action chunk size, warm-up state and camera rate must be controlled or explicitly reported as different conditions.

| Work | Primary question | Relevance to OmniBot | Boundary OmniBot must respect |
|---|---|---|---|
| RT-1 [4] | Scalable transformer policy for real robot control | Historical foundation for policy scaling | Not a low-cost distributed-systems baseline |
| RT-2 [5] | VLA formulation and web-knowledge transfer to robot actions | Establishes VLA terminology and capability motivation | Different scientific and hardware setting from an OmniBot latency study |
| OpenVLA [6] | Open 7B VLA, fine-tuning and efficient consumer-GPU serving | Candidate model and deployment dependency | Does not validate OmniBot’s Pi, LAN, ROS 2 or actuation behaviour |
| Octo [7] | Open generalist robot policy | Candidate policy family and adaptation reference | Not a complete mobile-manipulation deployment study |
| SmolVLA [8] | Compact, asynchronous, affordable VLA | Most relevant compact-policy reference | Consumer CPU/GPU support is not proof of Pi-to-GPU system performance |

A crucial design choice follows. The manuscript must explicitly state that it is **not a new VLA architecture paper**, and it must avoid comparisons based on task-success results collected under different robots, datasets and task definitions. Its performance results should concern deployment observables under its own pre-specified conditions: latency, control rate, deadlines, resource use, network traffic, failure mode and limited task execution.

## 3. Data resources and robot-learning infrastructure

VLA development is tied to increasingly broad robot datasets. **Open X-Embodiment** aggregates more than one million real-robot trajectories across 22 embodiments and introduces the RT-X family of models [9]. **BridgeData V2** reports 53,896 trajectories across 24 environments using a publicly available low-cost robot, demonstrating how publicly released data can support scalable manipulation learning [10]. **DROID** contributes a large in-the-wild manipulation dataset with 76k demonstrations and a distributed collection setup [11].

These sources establish that data diversity, cross-embodiment transfer and reproducible data collection are central to robot learning. They do not demonstrate that a small new platform has a comparable data release or policy success curve. Consequently, OmniBot’s repository pipeline may be described as **LeRobot-compatible data and training infrastructure**, but the manuscript must not present it as a dataset contribution or claim “learns from approximately 50 demonstrations” until a controlled experiment supplies a dataset card, train/evaluation split, task definition, checkpoint lineage and held-out physical trials.

## 4. ROS 2 navigation, systems benchmarking and the measurement gap

For navigation and platform integration, OmniBot builds on rather than replaces ROS 2 infrastructure. The Nav2 ecosystem supplies modular localization, planning, control, recovery and behaviour-tree composition for multiple robot kinematics, including holonomic robots [12]. This means Nav2 must appear as a dependency in the system description, not as an OmniBot algorithmic contribution. The paper’s own contribution is limited to the integration boundary: how Nav2-generated base commands are selected or sequenced relative to learned policy commands, and how that transition is recorded and tested.

The benchmarking contribution requires the same discipline. **RobotPerf** is an open, vendor-agnostic ROS 2 benchmarking suite for robotics-computing performance [13]. It shows that reproducible performance evaluation is already an active concern and prevents OmniBot from claiming generic robotics benchmarking novelty. OmniBot can nevertheless contribute a **platform-specific SLO-aware evaluation protocol** if it connects benchmarks across the full embodied path: motor protocol, wheel kinematics, BEV generation, policy preprocessing, policy service latency, ROS 2/topic timing, actuator publication and hardware telemetry. That contribution is only credible when the protocol releases raw runs, machine metadata, declared thresholds and at least one regression-detection demonstration.

| Layer | Existing foundation | OmniBot-specific evidence required |
|---|---|---|
| Navigation | Nav2 planning, control, recovery, behaviour-tree framework [12] | Configured holonomic navigation, command interface and stage-transition logs |
| Computing benchmarks | RobotPerf ROS 2 performance methodology [13] | SLO rationale, Pi/GPU results, raw artefacts, repeatability and seeded regression |
| Policy services | OpenVLA/SmolVLA serving and adaptation pathways [6] [8] | Client/server/request timing, model identity, network payloads, resource telemetry |
| Task execution | Mobile-manipulation platform evaluations [1] [2] [3] | Pre-registered small-N task protocol, success CIs and explicit failure taxonomy |

## 5. Safety terminology and control arbitration

The paper plan’s use of “safety arbitration” is potentially misleading. Formal **shielding** is a specific safe-learning concept. Alshiekh et al. define a shield as a reactive system that monitors a learner’s action and corrects it if it would violate a temporal-logic safety specification [14]. Control-barrier-function work similarly requires a specified safety set and mechanism with a demonstrable relationship to system dynamics [15].

A command multiplexer that selects sources, clamps velocity/joint changes and supports an emergency stop is useful engineering, but it is not automatically a formal shield or a safety guarantee. The correct present-tense terminology for OmniBot is **safety-bounded command mux**. The paper may state the concrete bounds and e-stop path, measure interventions and report failures. It should reserve “verified,” “guaranteed,” “shield,” or “safe autonomy” for a later study that supplies the required formal assumptions and verification evidence.

## 6. Synthesised gap and research implications

The literature does not support the broad claim that no comparable system exists. It does support the more precise research opportunity below:

> **Synthesised gap.** Open low-cost mobile manipulators, open VLA policies, robot-learning datasets, ROS 2 navigation frameworks and robotics-computing benchmark suites are all available. There is still an opportunity for an open, empirically grounded systems paper that documents and measures a particular low-cost mobile-manipulation composition: constrained edge-side robot I/O and perception, networked GPU policy serving, explicit classical-to-learned command transitions, and raw reproducibility artefacts covering full-pipeline timing and resource use.

This gap has four strict implications.

| Requirement | Why it is necessary | Failure mode if omitted |
|---|---|---|
| Match M1 local and M2 distributed policy conditions | Separates compute-topology effect from model/configuration effect | A reviewer can reject the comparison as confounded |
| Synchronize/characterize clocks before decomposing cross-host latency | Makes uplink/downlink inference physically meaningful | Invalid latency decomposition from unsynchronized timestamps |
| Report three cost boundaries | Existing systems include different combinations of compute/sensing/accessories | Misleading affordability claim |
| Release raw JSON/CSV, exact tag, telemetry and task log | Converts a platform description into reproducible systems evidence | “We built a robot” rather than a research contribution |

## 7. Assessment of evidence status

The repository already contains architecture, deployment and benchmark-harness evidence. It does **not** yet contain measured benchmark results in the expected results directory. Therefore the current manuscript can accurately describe the platform and preregister a methodology, but it cannot claim real-time performance, superior latency, reliability, task success, safety, portability or a final cost comparison. The critical path is experimental evidence, not prose polishing.

The seed bibliography currently covers the key comparison classes. Before external submission, it should grow through forward/backward citation review to approximately 35–50 verified citations, prioritizing: additional open mobile manipulators, end-to-end networked robotics profiling, ROS 2 real-time/deployment studies, VLA deployment/quantization studies, and precise safety/arbitration literature. Bibliographic expansion must not change the core claim boundary described above.

## References

[1] Z. Fu, T. Z. Zhao and C. Finn, “Mobile ALOHA: Learning Bimanual Mobile Manipulation with Low-Cost Whole-Body Teleoperation,” 2024. [arXiv](https://arxiv.org/abs/2401.02117)

[2] H. Cui, Y. Yuan, Y. Zheng and J. Hao, “AhaRobot: A Low-Cost Open-Source Bimanual Mobile Manipulator for Embodied AI,” 2025. [arXiv](https://arxiv.org/abs/2503.10070)

[3] J. Wu *et al.*, “TidyBot++: An Open-Source Holonomic Mobile Manipulator for Robot Learning,” 2025. [arXiv](https://arxiv.org/abs/2412.10447)

[4] A. Brohan *et al.*, “RT-1: Robotics Transformer for Real-World Control at Scale,” 2023. [arXiv](https://arxiv.org/abs/2212.06817)

[5] B. Zitkovich *et al.*, “RT-2: Vision-Language-Action Models Transfer Web Knowledge to Robotic Control,” CoRL, 2023. [PMLR](https://proceedings.mlr.press/v229/zitkovich23a.html)

[6] M. J. Kim *et al.*, “OpenVLA: An Open-Source Vision-Language-Action Model,” ICML, 2025. [PMLR](https://proceedings.mlr.press/v270/kim25c.html)

[7] Octo Model Team *et al.*, “Octo: An Open-Source Generalist Robot Policy,” 2024. [arXiv](https://arxiv.org/abs/2405.12213)

[8] M. Shukor *et al.*, “SmolVLA: A Vision-Language-Action Model for Affordable and Efficient Robotics,” 2025. [arXiv](https://arxiv.org/abs/2506.01844)

[9] Open X-Embodiment Collaboration, “Open X-Embodiment: Robotic Learning Datasets and RT-X Models,” 2024. [arXiv](https://arxiv.org/abs/2310.08864)

[10] H. R. Walke *et al.*, “BridgeData V2: A Dataset for Robot Learning at Scale,” CoRL, 2023. [PMLR](https://proceedings.mlr.press/v229/walke23a.html)

[11] A. Khazatsky *et al.*, “DROID: A Large-Scale In-The-Wild Robot Manipulation Dataset,” 2024. [arXiv](https://arxiv.org/abs/2403.12945)

[12] S. Macenski, T. Moore, F. Martin and R. White, “From the Desks of ROS Maintainers: A Survey of Modern & Capable Mobile Robotics Algorithms in the Robot Operating System 2,” *Robotics and Autonomous Systems*, 2023. [arXiv](https://arxiv.org/abs/2307.15236)

[13] V. Mayoral-Vilches *et al.*, “RobotPerf: An Open-Source, Vendor-Agnostic, Benchmarking Suite for Evaluating Robotics Computing System Performance,” 2024. [arXiv](https://arxiv.org/abs/2309.09212)

[14] M. Alshiekh *et al.*, “Safe Reinforcement Learning via Shielding,” AAAI, 2018. [DOI](https://doi.org/10.1609/aaai.v32i1.11797)

[15] R. Cheng, G. Orosz, R. M. Murray and J. W. Burdick, “End-to-End Safe Reinforcement Learning through Barrier Functions for Safety-Critical Continuous Control Tasks,” AAAI, 2019. [DOI](https://doi.org/10.1609/aaai.v33i01.33013387)
