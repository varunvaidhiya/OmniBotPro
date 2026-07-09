# Copyright (c) Meta Platforms, Inc. and affiliates.
# All rights reserved.
#
# Licensed under the Oculus SDK License Agreement (the "License");
# you may not use the Oculus SDK except in compliance with the License,
# which is provided at the time of installation or download, or which
# otherwise accompanies this software in either electronic or hard copy form.
#
# You may obtain a copy of the License at
#
# https://developer.oculus.com/licenses/oculussdk/
#
# Unless required by applicable law or agreed to in writing, the Oculus SDK
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

import argparse
import json
import os
import sys
import traceback
from datetime import datetime

import numpy as np
import pandas as pd
import psutil
from perfetto.trace_processor import TraceProcessor, TraceProcessorConfig

# this version must match the version in InsightsMetricRules.json
METRIC_JSON_VERSION = "0.0.1"


def remove_file(file_path):
    if os.path.exists(file_path):
        os.remove(file_path)


def remove_shell_process():
    for proc in psutil.process_iter(["name"]):
        if proc.info["name"] == "trace_processor_shell.exe":
            proc.kill()


def mean_stddev_for_df(data, column_name, do_print=False, result_factor=1000000.0):
    result = (
        data[column_name].astype(float).mean(),
        data[column_name].astype(float).std(),
    )
    if do_print:
        print("entries: %d" % len(data))
        print("mean = %.2f ms" % (result[0] / result_factor))
        print("std = %.2f ms" % (result[1] / result_factor))

    if not isinstance(result[0], float):
        result[0] = result[0].item()
    if not isinstance(result[1], float):
        result[1] = result[1].item()

    # Handle NaN values by replacing them with 0.0
    if np.isnan(result[0]):
        result = (0.0, result[1])
    if np.isnan(result[1]):
        result = (result[0], 0.0)

    json_result = {
        "entries": len(data),
        "mean": (result[0] / result_factor),
        "std": (result[1] / result_factor),
    }
    return json_result


def mean_stddev_for_counter(
    counter, counter_name, do_print=False, result_factor=1000000.0
):
    filterted_counter = counter[counter["name"] == counter_name]
    # MetricAPI.write_to_log(filterted_counter, enable_logging=self.enable_logging, log_prefix=self.jsonName)

    result = (
        filterted_counter["value"].mean(),
        filterted_counter["value"].std(),
        filterted_counter["value"].max(),
        filterted_counter["value"].quantile(0.75),
    )

    if do_print:
        print(counter_name)
        print("entries: %d" % len(filterted_counter))
        print("mean = %.2f " % (result[0]))
        print("std = %.3f " % (result[1]))
        print("max = %.3f " % (result[2]))
        print("quantile_75 = %.3f " % result[3])

    # when counters are invalid, we return some default value
    if len(filterted_counter) < 100:
        json_result = {
            "entries": len(filterted_counter),
            "mean": 1.0,
            "std": 1.0,
            "max": 1.0,
            "quantile_75": 1.0,
        }
        return json_result

    json_result = {
        "entries": len(filterted_counter),
        "mean": (result[0] / result_factor).item(),
        "std": (result[1] / result_factor).item(),
        "max": (result[2] / result_factor),
        "quantile_75": (result[3] / result_factor),
    }
    return json_result


def print_slice_info(df):
    for _index, row in df.iterrows():
        print(row["name"], row["ts"], row["frame_dur"], row["id"])


def print_track_info(dataframe, do_print=False):
    json_result = {"name": [], "ts": [], "dur": []}
    for _index, row in dataframe.iterrows():
        if do_print:
            print(row["name"], row["ts"], row["dur"])
        json_result["name"].append(row["name"])
        json_result["ts"].append(row["ts"])
        json_result["dur"].append(row["dur"])

    return json_result


def get_counter_for_slice_df(counter_df, slice_df, slice_id, frame_count=72):
    sorted_color_pass = slice_df.sort_values(by="ts", ascending=True)

    sorted_color_pass = sorted_color_pass.head(frame_count)

    end_time = sorted_color_pass["ts"].max()

    counter_df[slice_id] = sys.maxsize

    counter_df.sort_values(by="ts", ascending=True, inplace=True)

    # MetricAPI.write_to_log(counter_df[], enable_logging=self.enable_logging, log_prefix=self.jsonName)

    result_counters = []
    for _index, row in sorted_color_pass.iterrows():
        ts_start = row["ts"]
        ts_end = ts_start + row["dur"]
        if ts_start > end_time:
            break

        counters_within_time = counter_df[
            (counter_df["ts"] > ts_start) & (counter_df["ts"] < ts_end)
        ]
        counters_within_time.loc[:, slice_id] = row["id"]

        result_counters.append(counters_within_time)

    counters_within_time = pd.concat(result_counters)
    counters_within_time.sort_values(by=slice_id, ascending=True, inplace=True)

    counter_df = pd.concat([counters_within_time, counter_df])
    return counters_within_time


# this is to find how busy CPUs are during the frame time
def combine_columns(row):
    if row["A"] > 3:
        return row["A"] + row["B"]
    else:
        return row["A"] * row["B"]


class MetricAPI:
    # Phase 2: Expanded Frame Boundary Patterns
    FRAME_BOUNDARY_PATTERNS = [
        # Existing patterns
        "%xrWaitFrame%",
        "%xrSyncActions%",
        "%XREarlyUpdate%",
        "FrameEvents.XRBeginFrame",
        # Additional Unity patterns
        "PlayerLoop",
    ]

    # Phase 2: Render Thread Detection Patterns
    RENDER_THREAD_PATTERNS = [
        # OpenXR render patterns
        "%xrEndFrame%",
        # Additional OpenXR patterns (note: xrBeginFrame typically on main thread)
        "%CompositorOpenXR::EndFrame%",
        "XR.Display.SubmitCurrentFrame",
    ]

    def __init__(self, trace, jsonPath, shell_bin_path="", enable_logging=False):
        self.trace = trace
        self.enable_logging = enable_logging
        self.jsonName = os.path.splitext(jsonPath)[0]
        self.has_gpu_data = False  # Initialize GPU data availability flag
        self.target_process = None  # Initialize target process
        self.target_main_thread = None  # Initialize target main thread
        print(trace)
        try:
            if not os.path.exists(shell_bin_path):
                self.trace_processor = TraceProcessor(
                    trace=trace,
                )

            else:
                self.trace_processor = TraceProcessor(
                    trace=trace,
                    config=TraceProcessorConfig(
                        bin_path=str(shell_bin_path), verbose=True
                    ),
                )

        except Exception as e:
            self.trace_processor.close()
            for proc in psutil.process_iter(["name"]):
                if proc.info["name"] == "trace_processor_shell.exe":
                    proc.kill()
            raise Exception("error open trace file:" + trace + e)

        # Check if GPU data is available in the trace
        self.has_gpu_data = self.check_gpu_data_availability()

        # Discover target processes
        self.target_process, self.target_main_thread = self.discover_target_process()

        # Phase 2: Discover render thread for the target process
        self.target_render_thread = self.discover_render_thread(self.target_process)

        now = datetime.now()
        # Format the date and time as a string
        datetime_string = now.strftime("%Y-%m-%d %H:%M:%S")
        MetricAPI.write_to_log(
            datetime_string,
            clean=True,
            enable_logging=self.enable_logging,
            log_prefix=self.jsonName,
        )
        MetricAPI.write_to_log(
            f"GPU data available: {self.has_gpu_data}",
            enable_logging=self.enable_logging,
            log_prefix=self.jsonName,
        )
        MetricAPI.write_to_log(
            f"Target process: {self.target_process}",
            enable_logging=self.enable_logging,
            log_prefix=self.jsonName,
        )
        MetricAPI.write_to_log(
            f"Target main thread: {self.target_main_thread}",
            enable_logging=self.enable_logging,
            log_prefix=self.jsonName,
        )
        MetricAPI.write_to_log(
            f"Target render thread: {self.target_render_thread}",
            enable_logging=self.enable_logging,
            log_prefix=self.jsonName,
        )

    def __exit__(self, exc_type, exc_value, traceback):
        if self.trace_processor:
            self.trace_processor.close()

    @staticmethod
    def write_to_log(
        *args,
        sep=" ",
        end="\n",
        file=None,
        flush=False,
        clean=False,
        enable_logging=False,
        log_prefix="",
    ):
        # Detect if running as PyInstaller executable
        if getattr(sys, "frozen", False):
            # Running as PyInstaller executable - use current working directory
            current_dir = os.getcwd()
        else:
            # Running as normal Python script - use script directory
            current_dir = os.path.dirname(__file__)
        # Construct the full path for debug log
        log_file_path = os.path.join(current_dir, log_prefix + "_debug.log")

        if clean:
            remove_file(log_file_path)

        if not enable_logging:
            return

        with open(log_file_path, "a") as log_file:
            print(*args, sep=sep, end=end, file=log_file, flush=flush)

    @staticmethod
    def write_to_error_log(
        *args,
        sep=" ",
        end="\n",
        file=None,
        flush=False,
        clean=False,
        log_prefix="",
    ):
        # Get the directory of the current file
        current_dir = os.path.dirname(__file__)
        # Construct the full path for error log
        error_log_file_path = os.path.join(current_dir, log_prefix + "_error.log")

        if clean:
            remove_file(error_log_file_path)

        # Always write errors regardless of enable_logging flag
        with open(error_log_file_path, "a") as log_file:
            print(*args, sep=sep, end=end, file=log_file, flush=flush)

    def get_gpu_time_df(self):
        return self.gpu_time_df

    def get_main_thread_frame_time_df(self):
        return self.main_thread_frame_time_df

    def get_render_thread_frame_time_df(self):
        return self.render_thread_frame_time_df

    def get_trace_processor(self):
        return self.trace_processor

    def get_main_color_pass_df(self):
        return self.main_color_pass_df

    def get_render_passes_df(self):
        return self.render_passes_df

    def schema_query(self, table_name):
        schema_query = f"SELECT * FROM {table_name} WHERE type='table'"
        schema_result = self.trace_processor.query(schema_query)
        print(f"Schema for table {table_name}")
        print(schema_result.as_pandas_dataframe())

    def gpu_time(self, start_frame=None, end_frame=None):
        sqlGpuTime = """
    SELECT slice.dur, slice.ts, slice.name, thread.name as tname
    FROM slice
    LEFT JOIN track ON slice.track_id=track.id
    LEFT JOIN process_track ON track.parent_id=process_track.id
    LEFT JOIN process ON process_track.upid=process.upid
    LEFT JOIN thread_track on slice.track_id = thread_track.id
    LEFT JOIN thread on thread_track.utid = thread.utid
    WHERE slice.name LIKE'FenceChecker::Wait%'
    ORDER BY slice.name
    """
        # start = time.time()
        qr_it = self.trace_processor.query(sqlGpuTime)
        # end = time.time()
        # MetricAPI.write_to_log(end - start)
        df = qr_it.as_pandas_dataframe()
        df = df.drop(df.index[:5])

        self.gpu_time_df = df

        # end2 = time.time()

        # MetricAPI.write_to_log(end2 - end)
        # MetricAPI.write_to_log(df)
        mean_std_tuple = mean_stddev_for_df(df, "dur", True)

        return mean_std_tuple

    def gpu_time2(
        self, allow_process_name, start_frame=None, end_frame=None, sampleSize=200
    ):
        if not hasattr(self, "render_passes_df"):
            self.get_gpu_stage_trace()

        if not hasattr(self, "render_passes_df"):
            return

        all_passes = self.render_passes_df
        color_passes = self.main_color_pass_df

        # Check if dataframes are empty to avoid indexing errors
        if all_passes.empty or color_passes.empty:
            # Return default values when data is insufficient
            json_result = {
                "entries": 0,
                "mean": 0.0,
                "std": 0.0,
                "max": 0.0,
                "quantile_75": 0.0,
                "render_passes_mean": 0.0,
            }
            return json_result

        all_color_ts = color_passes["ts"]
        start_time = all_color_ts.iloc[0]

        frame_times = []
        render_passes = []

        #        sqlFenceStartTimeByThread = f"""
        #        SELECT thread.name, thread_state.state, thread_state.ts, thread_state.dur, process.name
        #        FROM thread_state
        #        LEFT JOIN thread on thread.utid = thread_state.utid
        #        LEFT JOIN process on thread.upid = process.upid
        #        WHERE thread.name LIKE'FenceChecker%' AND ts > {start_time} AND process.name LIKE 'com.UnityTechnologies.com.unity%'
        #        """

        #        qr_it = self.trace_processor.query(sqlFenceStartTimeByThread)

        sqlFenceStartTime = f"""
        SELECT slice.dur, slice.ts, slice.name, process.name as pname
        FROM slice
        LEFT JOIN thread_track on slice.track_id = thread_track.id
        LEFT JOIN thread on thread_track.utid = thread.utid
        LEFT JOIN process on thread.upid = process.upid
        WHERE slice.name LIKE'FenceChecker::Wait%' AND ts > {start_time} AND ((process.name NOT LIKE 'com.oculus%') OR (process.name LIKE '{allow_process_name}%'))
        ORDER BY slice.name
        """
        sqlFenceStartTime = sqlFenceStartTime.replace(
            "{allow_process_name}", allow_process_name
        )
        qr_it = self.trace_processor.query(sqlFenceStartTime)
        fences_wait_df = qr_it.as_pandas_dataframe()

        # print(list(set(fences_wait_df["tname"].to_list())))

        frame_count = min(len(fences_wait_df) - 1, sampleSize)

        # we have FenceChecker::Wait
        if frame_count > 0:
            all_ts = fences_wait_df["ts"]
            all_dur = fences_wait_df["dur"]
            # all_name = fences_wait_df["name"]
            MetricAPI.write_to_log(
                all_ts, enable_logging=self.enable_logging, log_prefix=self.jsonName
            )

            sum_of_frame_time = 0

            # print(frame_count)
            for i in range(frame_count):
                # end of first checker to the next frame checker
                ts_start = all_ts.iloc[i] + all_dur.iloc[i]
                ts_end = all_ts.iloc[i + 1] + all_dur.iloc[i + 1]
                fence_wait_dur = all_dur.iloc[i + 1]

                passes_within_time = all_passes[
                    (all_passes["ts"] >= ts_start) & (all_passes["ts"] < ts_end)
                ]

                # in GPU bounded case, the fence checker wait might extend into next color pass
                # we are detecting in the case and drop the last color pass to ge the correct frame time
                sum_of_frame_time = passes_within_time["dur"].sum()
                if (sum_of_frame_time - fence_wait_dur) > (fence_wait_dur / 3):
                    passes_within_time = passes_within_time.drop(
                        passes_within_time.tail(1).index
                    )

                    sum_of_frame_time = passes_within_time["dur"].sum()

                # if sum_of_frame_time > 21189000:
                #    print("passes a frame")
                #    print(all_name.iloc[i])
                #    print(ts_start)
                #    print(ts_end)
                #    print(ts_end - ts_start)
                #    print(len(passes_within_time))
                #    print(passes_within_time)
                #    print(sum_of_frame_time)

                frame_times.append(sum_of_frame_time)
                render_passes.append(len(passes_within_time))
            MetricAPI.write_to_log(
                frame_times,
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
        else:
            frame_count = min(len(all_color_ts) - 1, sampleSize)
            for i in range(frame_count):
                ts_start = all_color_ts.iloc[i]
                ts_end = all_color_ts.iloc[i + 1]
                passes_within_time = all_passes[
                    (all_passes["ts"] >= ts_start) & (all_passes["ts"] < ts_end)
                ]

                sum_of_frame_time = passes_within_time["dur"].sum()

                frame_times.append(sum_of_frame_time)
                render_passes.append(len(passes_within_time))

        # print(frame_times)
        json_result = {
            "entries": len(frame_times),
            "mean": (np.mean(frame_times) / 1000000).item(),
            "std": (np.std(frame_times) / 1000000).item(),
            "max": (np.max(frame_times) / 1000000).item(),
            "quantile_75": (np.quantile(frame_times, 0.75) / 1000000).item(),
            "render_passes_mean": np.mean(render_passes),
        }
        return json_result

    def detect_frame_boundaries_adaptive(self, allow_process_name):
        """
        Phase 2: Try multiple frame detection strategies in order of likelihood.
        Return the first successful detection method with quality assessment.
        """
        frame_detection_results = []

        MetricAPI.write_to_log(
            f"Starting adaptive frame boundary detection for process: {allow_process_name}",
            enable_logging=self.enable_logging,
            log_prefix=self.jsonName,
        )

        # Try each pattern in order of priority
        for i, pattern in enumerate(self.FRAME_BOUNDARY_PATTERNS):
            try:
                MetricAPI.write_to_log(
                    f"Trying pattern {i + 1}/{len(self.FRAME_BOUNDARY_PATTERNS)}: {pattern}",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )

                # Build SQL query for this pattern
                # Only include slices from the main thread if identified
                main_thread_filter = ""
                if self.target_main_thread:
                    main_thread_filter = (
                        f"AND thread.name = '{self.target_main_thread}'"
                    )

                sqlFramTime = f"""
                SELECT slice.ts, slice.name, slice.id, slice.dur, thread.name as tname, thread.utid as utid
                FROM slice
                LEFT JOIN track ON slice.track_id=track.id
                LEFT JOIN thread_track on slice.track_id = thread_track.id
                LEFT JOIN thread on thread_track.utid = thread.utid
                LEFT JOIN process on thread.upid = process.upid
                WHERE ((process.name NOT LIKE 'com.oculus%') OR (process.name LIKE '{allow_process_name}%'))
                  AND slice.name LIKE '{pattern}'
                  {main_thread_filter}
                ORDER BY ts
                """

                qr_it = self.trace_processor.query(sqlFramTime)
                df = qr_it.as_pandas_dataframe()

                if df.empty:
                    MetricAPI.write_to_log(
                        f"Pattern '{pattern}' returned no results",
                        enable_logging=self.enable_logging,
                        log_prefix=self.jsonName,
                    )
                    continue

                # Validate frame detection quality
                quality_score, quality_reason = self.assess_frame_quality(df, pattern)

                frame_detection_results.append(
                    {
                        "pattern": pattern,
                        "dataframe": df,
                        "quality_score": quality_score,
                        "quality_reason": quality_reason,
                        "entry_count": len(df),
                    }
                )

                MetricAPI.write_to_log(
                    f"Pattern '{pattern}': {len(df)} entries, quality: {quality_score:.2f} ({quality_reason})",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )

                # If we found a good enough pattern, we can use it
                if quality_score >= 0.6:  # Minimum quality threshold
                    MetricAPI.write_to_log(
                        f"Found acceptable pattern: {pattern} (quality: {quality_score:.2f})",
                        enable_logging=self.enable_logging,
                        log_prefix=self.jsonName,
                    )
                    break

            except Exception as e:
                MetricAPI.write_to_log(
                    f"Error trying pattern '{pattern}': {e}",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )
                continue

        # Sort by quality score and select the best one
        if frame_detection_results:
            frame_detection_results.sort(key=lambda x: x["quality_score"], reverse=True)
            best_result = frame_detection_results[0]

            MetricAPI.write_to_log(
                f"Selected best frame detection: {best_result['pattern']} (quality: {best_result['quality_score']:.2f})",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )

            return (
                best_result["dataframe"],
                best_result["pattern"],
                best_result["quality_score"],
            )
        else:
            MetricAPI.write_to_log(
                "No frame boundary patterns found any results",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return pd.DataFrame(), "none", 0.0

    def assess_frame_quality(self, df, pattern):
        """
        Phase 2: Determine if detected frames are suitable for analysis.
        Return confidence score and quality metrics.
        """
        if df.empty:
            return 0.0, "empty_dataframe"

        # Basic requirements
        if len(df) < 10:
            return 0.1, f"insufficient_data({len(df)}_frames)"

        # Create frame intervals for analysis
        df_copy = df.copy().sort_values(by="ts")
        df_copy["ts_shift"] = df_copy["ts"].shift(-1)
        df_copy["frame_dur"] = df_copy["ts_shift"] - df_copy["ts"] - df_copy["dur"]

        # Remove invalid intervals
        valid_frames = df_copy[df_copy["frame_dur"] > 0]
        if len(valid_frames) < 5:
            return 0.2, f"insufficient_valid_frames({len(valid_frames)})"

        frame_times = valid_frames["frame_dur"].values / 1000000.0  # Convert to ms

        # Check frame time consistency (should be roughly 16-33ms for 30-60 FPS)
        mean_frame_time = np.mean(frame_times)
        std_frame_time = np.std(frame_times)

        quality_score = 0.5  # Base score
        reasons = []

        # Frame time range check
        if 10.0 <= mean_frame_time <= 50.0:  # 20-100 FPS range
            quality_score += 0.2
            reasons.append("reasonable_frame_time")
        else:
            reasons.append(f"unusual_frame_time({mean_frame_time:.1f}ms)")

        # Consistency check
        if (
            std_frame_time < mean_frame_time * 0.5
        ):  # Standard deviation less than 50% of mean
            quality_score += 0.2
            reasons.append("consistent_timing")
        else:
            reasons.append(f"inconsistent_timing(std:{std_frame_time:.1f}ms)")

        # Check for reasonable frame count
        if len(valid_frames) >= 30:
            quality_score += 0.1
            reasons.append("sufficient_frames")

        # Pattern-specific bonuses
        if any(
            preferred in pattern
            for preferred in ["xrWaitFrame", "vrapi_WaitFrame", "XRUpdate"]
        ):
            quality_score += 0.15
            reasons.append("preferred_xr_pattern")
        elif any(unity in pattern for unity in ["Unity", "Player", "Application"]):
            quality_score += 0.1
            reasons.append("unity_pattern")

        # Penalize if too many identical frame times (likely invalid)
        try:
            unique_frame_times = len(np.unique(np.round(frame_times, 1)))
            if unique_frame_times < len(frame_times) * 0.1:
                quality_score -= 0.3
                reasons.append("too_many_identical_times")
        except Exception:
            # Skip this check if there are issues with the frame times array
            pass

        quality_reason = "+".join(reasons)
        return min(1.0, max(0.0, quality_score)), quality_reason

    def main_thread_frame_time(
        self, allow_process_name, start_frame=None, end_frame=None
    ):
        # Phase 2: Use adaptive frame detection instead of hardcoded patterns
        MetricAPI.write_to_log(
            f"Phase 2: Using adaptive frame boundary detection for {allow_process_name}",
            enable_logging=self.enable_logging,
            log_prefix=self.jsonName,
        )

        # Try adaptive detection first
        df, detected_pattern, quality_score = self.detect_frame_boundaries_adaptive(
            allow_process_name
        )

        # If adaptive detection failed, fall back to original method
        if df.empty or quality_score < 0.3:
            MetricAPI.write_to_log(
                f"Adaptive detection failed (quality: {quality_score:.2f}), falling back to original method",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return self.main_thread_frame_time_original(
                allow_process_name, start_frame, end_frame
            )

        MetricAPI.write_to_log(
            f"Using adaptive detection result: pattern='{detected_pattern}', quality={quality_score:.2f}, frames={len(df)}",
            enable_logging=self.enable_logging,
            log_prefix=self.jsonName,
        )

        # Process the detected frame data
        # top rows might have partial data, so drop
        df = df.drop(df.index[:5])

        if df.empty:
            MetricAPI.write_to_log(
                "No data remaining after dropping initial rows",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            # Fall back to original method
            return self.main_thread_frame_time_original(
                allow_process_name, start_frame, end_frame
            )

        # create a new colunme that is ts of next WaitFrame
        df["ts_shift"] = df["ts"].shift(-1)
        # the duration excludes the WaitFrame time = next_ts - ts - dur_waitFrame
        df["frame_dur"] = df["ts_shift"] - df["ts"] - df["dur"]
        # shift ts to be after WaitFrame
        df["ts"] = df["ts"] + df["dur"]
        df.drop("ts_shift", axis=1, inplace=True)

        # last raw duration is 0, so drop
        df.drop(df.tail(2).index, inplace=True)

        self.main_thread_frame_time_df = df

        MetricAPI.write_to_log(
            df, enable_logging=self.enable_logging, log_prefix=self.jsonName
        )

        if df.empty:
            MetricAPI.write_to_log(
                "No valid frame intervals found",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            # Fall back to original method
            return self.main_thread_frame_time_original(
                allow_process_name, start_frame, end_frame
            )

        mean_std_tuple = mean_stddev_for_df(df, "frame_dur", False)

        df_sorted = df.sort_values(by="frame_dur", ascending=False)
        df_dropped = df_sorted.drop(df_sorted.index[:3])

        MetricAPI.write_to_log(
            "Remove Top 3 outliers",
            enable_logging=self.enable_logging,
            log_prefix=self.jsonName,
        )

        mean_std_tuple = mean_stddev_for_df(df_dropped, "frame_dur", False)

        # Add new metadata to the result
        mean_std_tuple["detected_pattern"] = detected_pattern
        mean_std_tuple["frame_detection_quality"] = quality_score

        # Phase 2: Extract render thread from frame data if not already found
        if not self.target_render_thread and not df.empty:
            # Check if the frame detection data reveals the render thread
            unique_threads = df["tname"].unique()
            MetricAPI.write_to_log(
                f"Phase 2: Extracting render thread from frame data. Threads found: {unique_threads}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )

            for thread_name in unique_threads:
                # Skip main thread if it's already identified
                if thread_name != self.target_main_thread:
                    # Count frame activities for this thread
                    thread_frame_count = len(df[df["tname"] == thread_name])
                    if thread_frame_count > 10:  # Minimum activity threshold
                        self.target_render_thread = thread_name
                        MetricAPI.write_to_log(
                            f"Phase 2: Found render thread from frame data: {thread_name} ({thread_frame_count} frame activities)",
                            enable_logging=self.enable_logging,
                            log_prefix=self.jsonName,
                        )
                        break
                elif thread_name == self.target_main_thread:
                    # Single-threaded case - main thread is also render thread
                    thread_frame_count = len(df[df["tname"] == thread_name])
                    if thread_frame_count > 10:
                        self.target_render_thread = thread_name
                        MetricAPI.write_to_log(
                            f"Phase 2: Single-threaded rendering detected: {thread_name} is both main and render thread",
                            enable_logging=self.enable_logging,
                            log_prefix=self.jsonName,
                        )

        return mean_std_tuple

    def main_thread_frame_time_original(
        self, allow_process_name, start_frame=None, end_frame=None
    ):
        """Original main_thread_frame_time method as fallback"""
        # Only include slices from the main thread if identified
        main_thread_filter = ""
        if self.target_main_thread:
            main_thread_filter = f"AND thread.name = '{self.target_main_thread}'"

        sqlFramTime = f"""
    SELECT slice.ts, slice.name, slice.id, slice.dur, thread.name as tname, thread.utid as utid
    FROM slice
    LEFT JOIN track ON slice.track_id=track.id
    LEFT JOIN thread_track on slice.track_id = thread_track.id
    LEFT JOIN thread on thread_track.utid = thread.utid
    LEFT JOIN process on thread.upid = process.upid
    WHERE ((process.name NOT LIKE 'com.oculus%') OR (process.name LIKE '{allow_process_name}%')) AND (slice.name LIKE'%xrWaitFrame%' OR slice.name LIKE'%vrapi_WaitFrame%' )
    {main_thread_filter}
    ORDER BY ts
    """

        sqlFramTime = sqlFramTime.replace("{allow_process_name}", allow_process_name)

        qr_it = self.trace_processor.query(sqlFramTime)
        df = qr_it.as_pandas_dataframe()
        if df.empty:
            sqlFramTime2 = f"""
            SELECT slice.ts, slice.name, slice.id, slice.dur, thread.name as tname, thread.utid as utid
            FROM slice
            LEFT JOIN track ON slice.track_id=track.id
            LEFT JOIN thread_track on slice.track_id = thread_track.id
            LEFT JOIN thread on thread_track.utid = thread.utid
            LEFT JOIN process on thread.upid = process.upid
            WHERE ((process.name NOT LIKE 'com.oculus%') OR (process.name LIKE '{allow_process_name}%')) AND (slice.name LIKE'EarlyUpdate.XRUpdate' OR slice.name LIKE'%vrapi_WaitFrame%' )
            {main_thread_filter}
            ORDER BY ts
            """
            sqlFramTime2 = sqlFramTime2.replace(
                "{allow_process_name}", allow_process_name
            )
            qr_it = self.trace_processor.query(sqlFramTime2)
            df = qr_it.as_pandas_dataframe()

        # top rows might have partial data, so drop
        df = df.drop(df.index[:5])

        # create a new colunme that is ts of next WaitFrame
        df["ts_shift"] = df["ts"].shift(-1)
        # the duration excludes the WaitFrame time = next_ts - ts - dur_waitFrame
        df["frame_dur"] = df["ts_shift"] - df["ts"] - df["dur"]
        # shift ts to be after WaitFrame
        df["ts"] = df["ts"] + df["dur"]
        df.drop("ts_shift", axis=1, inplace=True)

        # last raw duration is 0, so drop
        df.drop(df.tail(1).index, inplace=True)

        self.main_thread_frame_time_df = df

        MetricAPI.write_to_log(
            df, enable_logging=self.enable_logging, log_prefix=self.jsonName
        )

        mean_std_tuple = mean_stddev_for_df(df, "frame_dur", False)

        df_sorted = df.sort_values(by="frame_dur", ascending=False)
        df_dropped = df_sorted.drop(df_sorted.index[:3])

        MetricAPI.write_to_log(
            "Remove Top 3 outliers",
            enable_logging=self.enable_logging,
            log_prefix=self.jsonName,
        )

        mean_std_tuple = mean_stddev_for_df(df_dropped, "frame_dur", False)

        return mean_std_tuple

    # this is the actual FPS time inclusing xrEndFrame time.
    def render_thread_frame_time(self, start_frame=None, end_frame=None):
        sqlFramTime = """
        SELECT slice.ts, slice.name, slice.id, thread.name as tname, thread.utid as utid
        FROM slice
        LEFT JOIN track ON slice.track_id=track.id
        LEFT JOIN process_track ON track.parent_id=process_track.id
        LEFT JOIN process ON process_track.upid=process.upid
        LEFT JOIN thread_track on slice.track_id = thread_track.id
        LEFT JOIN thread on thread_track.utid = thread.utid
        WHERE slice.name LIKE'!xrEndFrame%' OR slice.name LIKE'%CompositorVRAPI::EndFrame%'
        ORDER BY ts
        """

        qr_it = self.trace_processor.query(sqlFramTime)
        # end = time.time()
        # MetricAPI.write_to_log(end - start, enable_logging=self.enable_logging, log_prefix=self.jsonName)
        df = qr_it.as_pandas_dataframe()

        if df.empty:
            sqlFramTime = """
            SELECT slice.ts, slice.name, slice.id, thread.name as tname, thread.utid as utid
            FROM slice
            LEFT JOIN track ON slice.track_id=track.id
            LEFT JOIN process_track ON track.parent_id=process_track.id
            LEFT JOIN process ON process_track.upid=process.upid
            LEFT JOIN thread_track on slice.track_id = thread_track.id
            LEFT JOIN thread on thread_track.utid = thread.utid
            WHERE slice.name LIKE'%CompositorOpenXR::EndFrame%' OR slice.name LIKE'%CompositorVRAPI::EndFrame%'
            ORDER BY ts
            """

            qr_it = self.trace_processor.query(sqlFramTime)
            df = qr_it.as_pandas_dataframe()

        # top rows might have partial data, so drop
        df = df.drop(df.index[:5])

        df["ts_shift"] = df["ts"].shift(-1)
        df["frame_dur"] = df["ts_shift"] - df["ts"]
        df.drop("ts_shift", axis=1, inplace=True)

        # last raw duration is 0, so drop
        df.drop(df.tail(1).index, inplace=True)

        self.render_thread_frame_time_df = df

        mean_stddev_for_df(df, "frame_dur", False)

        MetricAPI.write_to_log(
            "Remove Top 5 outliers",
            enable_logging=self.enable_logging,
            log_prefix=self.jsonName,
        )

        df_sorted = df.sort_values(by="frame_dur", ascending=False)
        row_to_drop = 5 if len(df_sorted) > 5 else 0
        df_dropped = df_sorted.drop(df_sorted.index[:row_to_drop])

        MetricAPI.write_to_log(
            df, enable_logging=self.enable_logging, log_prefix=self.jsonName
        )

        json_result = mean_stddev_for_df(df_dropped, "frame_dur", False)

        return json_result

    def get_top_main_thread_frame_time(self, count, start_frame=None, end_frame=None):
        df = self.get_main_thread_frame_time_df()
        df_sorted = df.sort_values(by="frame_dur", ascending=False)
        df_top = df_sorted.head(count)

        return df_top

    def get_slices_within_time_range(
        self, start_time, end_time, keyword="", sort_by="ts", thread_utid=None
    ):
        # Build the base query with thread utid included in results
        query = f"""
        SELECT slice.name, ts, dur, slice.id, thread.utid
        FROM slice
        LEFT JOIN thread_track on slice.track_id = thread_track.id
        LEFT JOIN thread on thread_track.utid = thread.utid
        WHERE ts >= {start_time} AND (ts + dur) <= {end_time} AND slice.name LIKE'%{keyword}%'
        """

        # Add thread utid filter if provided
        if thread_utid is not None:
            query += f" AND thread.utid = {thread_utid}"

        qr_it = self.trace_processor.query(query)
        df = qr_it.as_pandas_dataframe()
        df_sorted = df.sort_values(by=sort_by, ascending=False)

        return df_sorted

    def get_threads_running_within_time_range(
        self, start_time, end_time, keyword="", sort_by="dur"
    ):
        query = f"""
        SELECT thread.name, thread_state.state, thread_state.ts, thread_state.dur
        FROM thread_state
        LEFT JOIN thread on thread.utid = thread_state.utid
        WHERE thread_state.ts >= {start_time} AND (thread_state.ts + thread_state.dur) <= {end_time} AND thread_state.state = 'Running' AND thread.name LIKE'%{keyword}%'
        """
        qr_it = self.trace_processor.query(query)

        df = qr_it.as_pandas_dataframe()
        df_sorted = df.sort_values(by=sort_by, ascending=False)

        return df_sorted

    def combine_arg_as_column(self, src_df, arg_name):
        queryArg = f"""
        SELECT key, arg_set_id, display_value as {arg_name}
        FROM args
        WHERE key LIKE '{arg_name}'
        """

        queryArg = queryArg.replace("{arg_name}", arg_name)

        qr2_it = self.trace_processor.query(queryArg)
        argument_df = qr2_it.as_pandas_dataframe()
        # key exist in different argument_df, so we need to drop the column before merge
        argument_df.drop("key", axis=1, inplace=True, errors="ignore")

        # MetricAPI.write_to_log(argument_df["numberOfBins"], enable_logging=self.enable_logging, log_prefix=self.jsonName)
        return pd.merge(src_df, argument_df, on="arg_set_id")

    def get_gpu_stage_trace(self):
        """
        Initialize GPU dataframes. Always creates render_passes_df and main_color_pass_df,
        either populated with data or as empty DataFrames with proper structure.
        """
        # Define the expected columns for proper DataFrame structure
        expected_columns = [
            "name",
            "ts",
            "dur",
            "id",
            "arg_set_id",
            "numberOfBins",
            "renderMode",
            "processName",
            "width",
            "height",
            "MSAA",
        ]

        # Initialize empty DataFrames with proper structure
        self.render_passes_df = pd.DataFrame(columns=expected_columns)
        self.main_color_pass_df = pd.DataFrame(columns=expected_columns)

        MetricAPI.write_to_log(
            f"Initializing GPU dataframes, GPU data available: {self.has_gpu_data}",
            enable_logging=self.enable_logging,
            log_prefix=self.jsonName,
        )

        # Only try to populate with data if GPU data is available
        if not self.has_gpu_data:
            MetricAPI.write_to_log(
                "No GPU data available, using empty DataFrames",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return self.render_passes_df

        try:
            query = """
            SELECT slice.name, slice.ts, slice.dur, slice.id, slice.arg_set_id
            FROM slice
            LEFT JOIN track ON slice.track_id=track.id
            WHERE slice.name LIKE 'surface#%bit color%MSAA%' AND track.name LIKE 'Gpu%'
            ORDER BY ts
            """
            qr_it = self.trace_processor.query(query)
            df = qr_it.as_pandas_dataframe()

            if len(df) == 0:
                MetricAPI.write_to_log(
                    "GPU query returned no results, keeping empty DataFrames",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )
                return self.render_passes_df

            combined_df = self.combine_arg_as_column(df, "numberOfBins")
            combined_df = self.combine_arg_as_column(combined_df, "renderMode")
            combined_df = self.combine_arg_as_column(combined_df, "processName")
            combined_df = self.combine_arg_as_column(combined_df, "width")
            combined_df = self.combine_arg_as_column(combined_df, "height")
            combined_df = self.combine_arg_as_column(combined_df, "MSAA")
            avg_msaa = combined_df["MSAA"].astype(float).mean()

            self.render_passes_df = combined_df[
                ~(
                    combined_df["processName"].str.contains("com.oculus.vr")
                    | combined_df["processName"].str.contains("com.oculus.sh")
                )
            ]

            MetricAPI.write_to_log(
                self.render_passes_df,
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            # remove all 0 bit color AND 0 bit depth
            self.main_color_pass_df = combined_df[
                ~(
                    combined_df["name"].str.contains("0 bit color")
                    | combined_df["name"].str.contains("0 bit depth")
                    | (combined_df["renderMode"].str == "Direct")
                    | combined_df["processName"].str.contains("com.oculus.vr")
                    | combined_df["processName"].str.contains("com.oculus.sh")
                    | (combined_df["width"].astype(float) < 1024.0)
                    | (combined_df["height"].astype(float) < 1024.0)
                    | (combined_df["MSAA"].astype(float) < avg_msaa)
                )
            ]

            MetricAPI.write_to_log(
                self.main_color_pass_df,
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            # mean_stddev_for_df(self.main_color_pass_df, "dur", False)

            return df

        except Exception as e:
            MetricAPI.write_to_log(
                f"Error processing GPU stage trace: {e}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            # Keep empty DataFrames on error
            return self.render_passes_df

    def get_binning_time(self, surface_track_df=None):
        if not hasattr(self, "main_color_pass_df"):
            return None

        # Return None if GPU data is not available
        if not self.has_gpu_data:
            MetricAPI.write_to_log(
                "No GPU data available for binning time calculation",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return None

        # Step 1: Get main color pass frames and sort by timestamp
        base_surface_df = self.get_main_color_pass_df()
        if surface_track_df is not None:
            base_surface_df = surface_track_df

        if base_surface_df is None or base_surface_df.empty:
            MetricAPI.write_to_log(
                "Empty main color pass DataFrame, cannot calculate binning time",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return None

        # Step 2: Sort by timestamp and get top 200 frames
        sorted_color_pass = base_surface_df.sort_values(by="ts", ascending=True)
        top_200_frames = sorted_color_pass.head(200)

        if top_200_frames.empty:
            MetricAPI.write_to_log(
                "No frames available for binning time calculation",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return None

        # Step 3: Get all binning slices for these frames
        frame_ids = top_200_frames["id"].tolist()

        try:
            # Debug: First, let's see what binning slices exist at all
            debug_query = """
            SELECT slice.name, slice.parent_id, slice.ts, slice.dur, track.name as track_name, COUNT(*) as count
            FROM slice
            LEFT JOIN track ON slice.track_id=track.id
            WHERE slice.name LIKE '%inning%' AND track.name LIKE 'Gpu%'
            GROUP BY slice.name, track.name
            ORDER BY count DESC
            LIMIT 10
            """
            debug_qr = self.trace_processor.query(debug_query)
            debug_df = debug_qr.as_pandas_dataframe()

            MetricAPI.write_to_log(
                "Debug: All binning-related slices in trace:",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            MetricAPI.write_to_log(
                debug_df,
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )

            # Debug: Check a sample of frame IDs we're looking for
            sample_frame_ids = frame_ids[:5]  # First 5 frame IDs
            MetricAPI.write_to_log(
                f"Debug: Sample frame IDs we're looking for: {sample_frame_ids}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )

            # Since binning slices don't have parent relationships, we need to match them temporally
            if not debug_df.empty:
                most_common_binning_name = debug_df["name"].iloc[0]
                MetricAPI.write_to_log(
                    f"Debug: Most common binning slice name: '{most_common_binning_name}'",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )

                # NEW APPROACH: Match binning slices to frames temporally
                # Get all binning slices and match them to the closest frame
                binning_query = f"""
                SELECT slice.id as binning_id, slice.ts as binning_ts, slice.dur as binning_dur, slice.name
                FROM slice
                LEFT JOIN track ON slice.track_id=track.id
                WHERE slice.name = '{most_common_binning_name}' AND track.name LIKE 'Gpu%'
                ORDER BY slice.ts
                """

                binning_qr_it = self.trace_processor.query(binning_query)
                all_binning_df = binning_qr_it.as_pandas_dataframe()

                MetricAPI.write_to_log(
                    f"Debug: Found {len(all_binning_df)} total binning slices",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )

                if not all_binning_df.empty:
                    # Match binning slices to frames based on temporal proximity
                    # For each frame, find binning slices that occur within the frame's time window
                    matched_binning_data = []

                    for _, frame in top_200_frames.iterrows():
                        frame_start = frame["ts"]
                        frame_end = frame["ts"] + frame["dur"]
                        frame_id = frame["id"]

                        # Find binning slices that overlap with this frame's time window
                        # Allow some tolerance before and after the frame
                        tolerance = 5000000  # 5ms tolerance in nanoseconds
                        window_start = frame_start - tolerance
                        window_end = frame_end + tolerance

                        frame_binning_slices = all_binning_df[
                            (all_binning_df["binning_ts"] >= window_start)
                            & (all_binning_df["binning_ts"] <= window_end)
                        ].copy()

                        if not frame_binning_slices.empty:
                            # Add frame_id to each matching binning slice
                            frame_binning_slices["frame_id"] = frame_id
                            matched_binning_data.append(frame_binning_slices)

                    if matched_binning_data:
                        binning_df = pd.concat(matched_binning_data, ignore_index=True)

                        MetricAPI.write_to_log(
                            f"Debug: Matched {len(binning_df)} binning slices to frames using temporal matching",
                            enable_logging=self.enable_logging,
                            log_prefix=self.jsonName,
                        )
                    else:
                        binning_df = pd.DataFrame()
                else:
                    binning_df = pd.DataFrame()
            else:
                binning_df = pd.DataFrame()

            MetricAPI.write_to_log(
                f"Found {len(binning_df)} binning slices for {len(top_200_frames)} frames using temporal matching",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )

            if binning_df.empty:
                MetricAPI.write_to_log(
                    "No binning data found for the top 200 frames using temporal matching",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )
                return None

            # Step 4: Sum up binning slices per frame (multiple binning slices per frame)
            binning_per_frame = (
                binning_df.groupby("frame_id")
                .agg(
                    {
                        "binning_dur": "sum",  # Sum all binning durations for each frame
                        "binning_ts": "min",  # Keep earliest timestamp for reference
                    }
                )
                .reset_index()
            )

            # Step 5: Merge with frame data and calculate statistics
            merged_df = top_200_frames.merge(
                binning_per_frame, left_on="id", right_on="frame_id", how="inner"
            )

            if merged_df.empty:
                MetricAPI.write_to_log(
                    "No matching binning and frame data found",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )
                return None

            MetricAPI.write_to_log(
                f"Final merged data has {len(merged_df)} frames with binning data",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )

            # Step 6: Use mean_stddev_for_df to produce result for binning time per frame
            json_result = mean_stddev_for_df(merged_df, "binning_dur", False)
            return json_result

        except Exception as e:
            MetricAPI.write_to_log(
                f"Error calculating binning time: {e}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return None
            return None

    def get_counter_df(self):
        query = """
        SELECT counter.ts, counter.value, track.name
        FROM counter
        LEFT JOIN track ON counter.track_id=track.id
        ORDER BY ts
        """

        qr_it = self.trace_processor.query(query)
        df = qr_it.as_pandas_dataframe()

        return df

    def frame_slices_by_ts_on_thread(self, src1, thread_utid, order_by):
        ts_src1_start = src1["ts"]
        ts_src1_end = ts_src1_start + src1["frame_dur"]
        sqlTrackSrc1 = f"""
        SELECT slice.dur, slice.ts, slice.name, thread.name as tname
        FROM slice
        LEFT JOIN track ON slice.track_id=track.id
        LEFT JOIN process_track ON track.parent_id=process_track.id
        LEFT JOIN process ON process_track.upid=process.upid
        LEFT JOIN thread_track on slice.track_id = thread_track.id
        LEFT JOIN thread on thread_track.utid = thread.utid
        WHERE slice.ts > {ts_src1_start} AND slice.ts < {ts_src1_end} AND thread_track.utid = {thread_utid}
        ORDER BY {order_by}
        """

        return self.trace_processor.query(sqlTrackSrc1).as_pandas_dataframe()

    def diff_frame_slices_by_ts_on_thread(self, base, dst, thread_utid):
        base_tracks = self.frame_slices_by_ts_on_thread(base, thread_utid, "slice.name")

        dst_tracks = self.frame_slices_by_ts_on_thread(dst, thread_utid, "slice.name")

        MetricAPI.write_to_log(
            "src1", enable_logging=self.enable_logging, log_prefix=self.jsonName
        )

        base_tracks_names = base_tracks["name"]
        filtered_df = dst_tracks[~dst_tracks["name"].isin(base_tracks_names)]
        MetricAPI.write_to_log(
            filtered_df, enable_logging=self.enable_logging, log_prefix=self.jsonName
        )

        return filtered_df

    def content_core_occupancy_in_ts(self, start_time, end_time, content_core=None):
        if content_core is None:
            content_core = [3, 4, 5]
        # turn into 3,4,5
        cores = ",".join(str(x) for x in content_core)

        query = f"""
        SELECT ts, dur, cpu, priority, process.name as pname, thread.name as tname
        FROM sched_slice
        LEFT JOIN thread using(utid) LEFT JOIN process using(upid)
        WHERE ts >= {start_time} AND ts < {end_time} AND cpu IN ({cores}) AND tname != 'swapper'
        ORDER BY cpu
        """

        df = self.trace_processor.query(query).as_pandas_dataframe()

        df["name"] = df["tname"] + " " + df["pname"]
        # df["hash"] = df.apply(lambda x: hash(x["name"]), axis=1)

        df = df.drop(["tname", "pname"], axis=1)

        df["dur_ex"] = df.apply(
            lambda row: (
                row["dur"]
                if (row["dur"] + row["ts"]) < end_time
                else end_time - row["ts"]
            ),
            axis=1,
        )

        cpu_groups = df.groupby("cpu")

        MetricAPI.write_to_log(
            df, enable_logging=self.enable_logging, log_prefix=self.jsonName
        )

        json_result = {"cpu_occupency": []}

        time_dur = end_time - start_time
        for cpu, cpu_group in cpu_groups:
            cpu_group.sort_values(by="dur_ex", ascending=False, inplace=True)
            json_core = {
                "cpu": cpu,
                "total_%": (cpu_group["dur_ex"].sum() * 100.0 / time_dur).item(),
                "top_3": [],
            }

            json_result["cpu_occupency"].append(json_core)

            top_exe = (
                cpu_group.groupby("name")["dur_ex"]
                .sum()
                .reset_index()
                .sort_values(by="dur_ex", ascending=False)
            )

            top_3_tname = top_exe.head(3)
            MetricAPI.write_to_log(
                top_3_tname,
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            for _index, row in top_3_tname.iterrows():
                top_enry = {
                    "name": row["name"],
                    "total_%": row["dur_ex"] * 100.0 / time_dur,
                }
                json_core["top_3"].append(top_enry)

        return json_result

    # TODO can we answaer how much the tread is runable vs running meaning scheduler is not able to schedule the thread
    # and also what the thread is doing during this time(?)
    def thread_running_ratio_in_ts(
        self,
        thread_utid,
        start_time,
        end_time,
        function_name=None,
        enhanced_analysis=False,
    ):
        """
        Analyze thread states during a time interval.

        Args:
            thread_utid: Thread unique ID
            start_time: Start timestamp
            end_time: End timestamp
            function_name: Optional function name for logging (Phase 4 enhancement)
            enhanced_analysis: If True, return enhanced analysis with detailed breakdown (Phase 4 enhancement)
        """
        query = f"""
        SELECT thread.name, thread_state.state as state, thread_state.ts, thread_state.dur
        FROM thread_state
        LEFT JOIN thread on thread.utid = thread_state.utid
        WHERE ( {start_time} <= (thread_state.ts + thread_state.dur) AND thread_state.ts <= {end_time}) AND thread.utid = {thread_utid}
        """
        df = self.trace_processor.query(query).as_pandas_dataframe()

        if df.empty:
            if enhanced_analysis:
                return {
                    "running_time_ms": 0.0,
                    "runnable_time_ms": 0.0,
                    "blocked_time_ms": 0.0,
                    "other_time_ms": 0.0,
                    "running_percentage": 0.0,
                    "runnable_percentage": 0.0,
                    "blocked_percentage": 0.0,
                    "thread_state_analysis": "no_thread_state_data",
                    # Legacy fields for compatibility
                    "run_ratio": 0.0,
                    "runable_ratio": 0.0,
                    "running_time": 0.0,
                }
            else:
                return {"run_ratio": 0.0, "runable_ratio": 0.0, "running_time": 0.0}

        # Calculate clipped durations for each thread state within the time interval
        clipped_state_durations = {}

        for _, row in df.iterrows():
            state = row["state"]
            state_start = row["ts"]
            state_end = state_start + row["dur"]

            # Calculate the overlap between thread state and our time interval
            overlap_start = max(state_start, start_time)
            overlap_end = min(state_end, end_time)

            # Only count if there's actual overlap
            if overlap_end > overlap_start:
                overlap_duration = overlap_end - overlap_start
                if state not in clipped_state_durations:
                    clipped_state_durations[state] = 0
                clipped_state_durations[state] += overlap_duration

        MetricAPI.write_to_log(
            f"Clipped state durations: {clipped_state_durations}",
            enable_logging=self.enable_logging,
            log_prefix=self.jsonName,
        )

        duration_ns = end_time - start_time
        duration_percent = 100.0 / duration_ns

        # Calculate basic metrics using clipped durations
        running_time_ns = clipped_state_durations.get("Running", 0)
        runratio = running_time_ns * duration_percent

        runable_time_ns = 0
        if "R" in clipped_state_durations:
            runable_time_ns += clipped_state_durations["R"]
        if "R+" in clipped_state_durations:
            runable_time_ns += clipped_state_durations["R+"]
        runable = runable_time_ns * duration_percent

        # Basic result for legacy compatibility
        jason_result = {
            "run_ratio": runratio,
            "runable_ratio": runable,
            "running_time": running_time_ns / 1000000,
        }

        if enhanced_analysis:
            # Phase 4 Enhancement: Detailed thread state analysis using clipped durations
            blocked_time_ns = 0
            other_time_ns = 0

            # Categorize all thread states using clipped durations
            for state, clipped_duration in clipped_state_durations.items():
                if state == "Running":
                    continue  # Already counted
                elif state in ["R", "R+"]:
                    continue  # Already counted as runnable
                elif state in [
                    "S",
                    "D",
                    "T",
                    "Z",
                ]:  # Sleeping, Disk sleep, Stopped, Zombie
                    blocked_time_ns += clipped_duration
                else:
                    other_time_ns += clipped_duration

            # Convert to milliseconds
            running_ms = running_time_ns / 1000000.0
            runnable_ms = runable_time_ns / 1000000.0
            blocked_ms = blocked_time_ns / 1000000.0
            other_ms = other_time_ns / 1000000.0
            duration_ms = duration_ns / 1000000.0

            # Calculate percentages - should now be properly bounded to 100%
            if duration_ms > 0:
                running_pct = (running_ms / duration_ms) * 100.0
                runnable_pct = (runnable_ms / duration_ms) * 100.0
                blocked_pct = (blocked_ms / duration_ms) * 100.0
            else:
                running_pct = runnable_pct = blocked_pct = 0.0

            # Sanity check - percentages should not exceed 100%
            total_accounted_time = running_ms + runnable_ms + blocked_ms + other_ms
            if (
                total_accounted_time > duration_ms * 1.01
            ):  # Allow 1% tolerance for rounding
                MetricAPI.write_to_log(
                    f"Warning: Thread state time exceeds function duration by {((total_accounted_time / duration_ms) - 1.0) * 100:.1f}% for {function_name or 'unknown function'}",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )

            # Analyze thread state characteristics
            analysis_result = "unknown"
            if running_pct >= 80:
                analysis_result = "busy_running"  # Function is actively executing
            elif blocked_pct >= 50:
                analysis_result = "waiting_for_lock"  # Function is blocked waiting
            elif runnable_pct >= 30:
                analysis_result = (
                    "preempted"  # Function is being interrupted by scheduler
                )
            elif running_pct >= 40 and runnable_pct >= 20:
                analysis_result = (
                    "mixed_execution"  # Mix of running and being preempted
                )
            elif blocked_pct >= 20 and running_pct >= 30:
                analysis_result = "io_bound"  # Mix of running and waiting (likely I/O)
            else:
                analysis_result = "low_activity"  # Very little execution time

            if function_name:
                MetricAPI.write_to_log(
                    f"Thread state analysis for {function_name}: running={running_pct:.1f}%, runnable={runnable_pct:.1f}%, blocked={blocked_pct:.1f}%, analysis={analysis_result} (duration={duration_ms:.2f}ms)",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )

            # Enhanced result with detailed breakdown
            jason_result.update(
                {
                    "running_time_ms": running_ms,
                    "runnable_time_ms": runnable_ms,
                    "blocked_time_ms": blocked_ms,
                    "other_time_ms": other_ms,
                    "running_percentage": running_pct,
                    "runnable_percentage": runnable_pct,
                    "blocked_percentage": blocked_pct,
                    "thread_state_analysis": analysis_result,
                }
            )

        MetricAPI.write_to_log(
            "run ratio: %.2f" % runratio,
            enable_logging=self.enable_logging,
            log_prefix=self.jsonName,
        )
        return jason_result

    def discover_target_process(self):
        """
        Discover all processes in the trace and identify potential target processes
        based on main thread activity patterns and heuristics.
        Returns a tuple of (target_process, target_main_thread).
        """
        try:
            # First, query processes with thread information to get candidates
            # Filter out com.oculus processes except com.oculus.UOIAssets early for performance
            # Also prioritize processes that have xrWaitFrame slices (likely Unity/game apps)
            process_query = """
            SELECT
                process.name as process_name,
                process.upid,
                COUNT(DISTINCT thread.utid) as thread_count,
                CASE WHEN EXISTS (
                    SELECT 1
                    FROM thread t2
                    LEFT JOIN thread_track tt2 ON t2.utid = tt2.utid
                    LEFT JOIN slice s2 ON tt2.id = s2.track_id
                    WHERE t2.upid = process.upid
                      AND (s2.name LIKE '%xrWaitFrame%'
                           OR s2.name LIKE '%vrapi_WaitFrame%'
                           OR s2.name LIKE '%XRUpdate%')
                ) THEN 1 ELSE 0 END as has_xr_wait_frame
            FROM process
            LEFT JOIN thread ON process.upid = thread.upid
            WHERE process.name NOT LIKE 'kworker%'
              AND process.name != ''
              AND process.name IS NOT NULL
              AND (process.name NOT LIKE 'com.oculus%' OR process.name LIKE 'com.oculus.UOIAssets%')
            GROUP BY process.name, process.upid
            ORDER BY has_xr_wait_frame DESC, thread_count DESC
            LIMIT 30
            """

            qr_it = self.trace_processor.query(process_query)
            processes_df = qr_it.as_pandas_dataframe()

            MetricAPI.write_to_log(
                f"Discovered processes: {len(processes_df)}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )

            # Known system vendor prefixes to exclude from "third-party app" bonus
            system_vendor_prefixes = [
                "com.meta.",
                "com.oculus.",
                "com.android.",
                "com.google.",
                "com.facebook.",
                "com.qualcomm.",
                "android.",
                "system",
            ]

            # Now analyze main thread activity for each promising process
            candidates = []

            for _, process_row in processes_df.iterrows():
                process_name = process_row["process_name"]
                upid = process_row["upid"]
                thread_count = process_row["thread_count"]

                MetricAPI.write_to_log(
                    f"Analyzing process: {process_name} (upid: {upid}, threads: {thread_count})",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )

                # Query threads for this specific process
                # Only check threads containing "main" for performance optimization
                thread_query = f"""
                SELECT
                    thread.name as thread_name,
                    thread.utid,
                    thread.upid,
                    COUNT(DISTINCT slice.id) as slice_count
                FROM thread
                LEFT JOIN thread_track ON thread.utid = thread_track.utid
                LEFT JOIN slice ON thread_track.id = slice.track_id
                WHERE thread.upid = {upid}
                  AND thread.name IS NOT NULL
                  AND thread.name != ''
                  AND (thread.name LIKE '%main%' OR thread.name LIKE '%Main%')
                GROUP BY thread.name, thread.utid, thread.upid
                ORDER BY slice_count DESC
                """

                thread_qr_it = self.trace_processor.query(thread_query)
                threads_df = thread_qr_it.as_pandas_dataframe()

                # Calculate process score based on multiple heuristics
                score = 0
                reasons = []
                main_thread_candidate = None

                # Basic thread count scoring
                if thread_count >= 10:
                    score += 30
                    reasons.append(f"high_thread_count({thread_count})")
                elif thread_count >= 5:
                    score += 20
                    reasons.append(f"medium_thread_count({thread_count})")

                # Check for specific game/app patterns first (highest priority)
                if any(
                    pattern in process_name
                    for pattern in [
                        "Unity",
                        "UnrealEngine",
                        "UE4",
                        "UE5",
                    ]
                ):
                    score += 100
                    reasons.append("game_application_pattern")

                # HIGH PRIORITY: Third-party app package name detection
                # Apps like com.twistedpixelgames.PILO, com.beatgames.beatsaber, etc.
                # These are likely the target app even in release mode without frame markers
                is_third_party_app = (
                    process_name.startswith("com.")
                    and process_name.count(".") >= 2
                    and not any(
                        process_name.startswith(prefix)
                        for prefix in system_vendor_prefixes
                    )
                    and not process_name.startswith("/")
                )

                if is_third_party_app:
                    # Strong bonus for third-party apps - likely the target even in release mode
                    score += 150
                    reasons.append("third_party_app_package")

                    # Extra bonus for high thread count in third-party apps (indicates active app)
                    if thread_count >= 20:
                        score += 50
                        reasons.append("high_activity_third_party")
                    elif thread_count >= 10:
                        score += 30
                        reasons.append("active_third_party")

                # System process filtering
                if process_name.startswith("com.oculus."):
                    if process_name == "com.oculus.UOIAssets":
                        score += 100
                        reasons.append("allowed_oculus_process")
                    else:
                        score -= 30
                        reasons.append("system_oculus_process")
                elif (
                    not process_name.startswith("com.android.")
                    and not process_name.startswith("system")
                    and not process_name.startswith("/")
                ):
                    score += 20
                    reasons.append("non_system_process")
                else:
                    # Heavily penalize system paths
                    if process_name.startswith("/"):
                        score -= 50
                        reasons.append("system_path_process")

                # Analyze main thread patterns
                main_thread_score = 0
                for _, thread_row in threads_df.iterrows():
                    thread_name = thread_row["thread_name"]
                    slice_count = thread_row["slice_count"]

                    MetricAPI.write_to_log(
                        f"  Thread: {thread_name}, slices: {slice_count}",
                        enable_logging=self.enable_logging,
                        log_prefix=self.jsonName,
                    )

                    thread_score = 0

                    # Look for main thread indicators
                    if thread_name and thread_name.lower() in [
                        "main",
                        "unitythread",
                        "unity main thread",
                    ]:
                        thread_score += 50
                    elif "Unity" in thread_name or "unity" in thread_name:
                        thread_score += 40
                    elif "UE" in thread_name or "Unreal" in thread_name:
                        thread_score += 40
                    elif "main" in thread_name.lower():
                        thread_score += 30
                    elif (
                        thread_name == process_name
                        or thread_name == process_name.split(".")[-1]
                    ):
                        thread_score += 25

                    # High activity indicates main thread
                    if slice_count > 1000:
                        thread_score += 20
                    elif slice_count > 500:
                        thread_score += 15
                    elif slice_count > 100:
                        thread_score += 10

                    # Check for frame-related activity
                    frame_activity_query = f"""
                    SELECT COUNT(*) as frame_activity_count
                    FROM slice
                    LEFT JOIN thread_track ON slice.track_id = thread_track.id
                    WHERE thread_track.utid = {thread_row["utid"]}
                      AND (slice.name LIKE '%xrWaitFrame%'
                           OR slice.name LIKE '%vrapi_WaitFrame%'
                           OR slice.name LIKE '%EarlyUpdate.XRUpdate%'
                           OR slice.name LIKE '%Frame%')
                    LIMIT 1
                    """

                    frame_qr_it = self.trace_processor.query(frame_activity_query)
                    frame_df = frame_qr_it.as_pandas_dataframe()

                    if (
                        not frame_df.empty
                        and frame_df["frame_activity_count"].iloc[0] > 0
                    ):
                        thread_score += 30
                        MetricAPI.write_to_log(
                            f"    Found frame activity: {frame_df['frame_activity_count'].iloc[0]}",
                            enable_logging=self.enable_logging,
                            log_prefix=self.jsonName,
                        )

                    if thread_score > main_thread_score:
                        main_thread_score = thread_score
                        main_thread_candidate = thread_name

                    MetricAPI.write_to_log(
                        f"    Thread score: {thread_score}",
                        enable_logging=self.enable_logging,
                        log_prefix=self.jsonName,
                    )

                # Add main thread score to process score
                if main_thread_score > 0:
                    score += main_thread_score
                    reasons.append(f"main_thread_activity({main_thread_score})")

                # Reasonable thread count for apps
                if 5 <= thread_count <= 50:
                    score += 10
                    reasons.append("reasonable_thread_count")

                candidates.append(
                    {
                        "process_name": process_name,
                        "score": score,
                        "thread_count": thread_count,
                        "main_thread": main_thread_candidate,
                        "reasons": reasons,
                    }
                )

            # Sort by score (highest first)
            candidates.sort(key=lambda x: x["score"], reverse=True)

            # Log candidate ranking
            MetricAPI.write_to_log(
                "Process ranking:",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )

            for i, candidate in enumerate(candidates[:10]):
                MetricAPI.write_to_log(
                    f"Rank {i + 1}: {candidate['process_name']} (score: {candidate['score']}, main_thread: {candidate['main_thread']}, reasons: {candidate['reasons']})",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )

            # Select the highest scoring candidate
            if candidates and candidates[0]["score"] > 0:
                selected = candidates[0]
                MetricAPI.write_to_log(
                    f"Selected target process: {selected['process_name']} (score: {selected['score']}, main_thread: {selected['main_thread']})",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )
                return (
                    selected["process_name"],
                    selected["main_thread"],
                )
            else:
                MetricAPI.write_to_log(
                    "No suitable target process found, using fallback",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )
                # Fallback to the first non-system process
                for candidate in candidates:
                    if not candidate["process_name"].startswith(
                        "com.android."
                    ) and not candidate["process_name"].startswith("system"):
                        return (
                            candidate["process_name"],
                            candidate["main_thread"],
                        )

                # Final fallback
                return "com.Unknown", None

        except Exception as e:
            MetricAPI.write_to_log(
                f"Error discovering target process: {e}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            # Fallback to default
            return "com.Unknown", None

    def check_gpu_data_availability(self):
        """
        Check if GPU data is available in the Perfetto trace.
        Returns True if GPU trace data is found, False otherwise.
        """
        try:
            query = """
            SELECT COUNT(*) as gpu_count
            FROM slice
            LEFT JOIN track ON slice.track_id=track.id
            WHERE slice.name LIKE 'surface#%bit color%MSAA%' AND track.name LIKE 'Gpu%'
            LIMIT 1
            """
            qr_it = self.trace_processor.query(query)
            result_df = qr_it.as_pandas_dataframe()

            if not result_df.empty and result_df["gpu_count"].iloc[0] > 0:
                return True
            return False

        except Exception as e:
            MetricAPI.write_to_log(
                f"Error checking GPU data availability: {e}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return False

    def discover_render_thread(self, target_process):
        """
        Phase 2: Discover the best render thread within the target process by looking for specific
        slice patterns that indicate rendering operations.
        """
        if not target_process or target_process == "com.Unknown":
            MetricAPI.write_to_log(
                "No valid target process for render thread discovery",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return None

        MetricAPI.write_to_log(
            f"Phase 2: Starting render thread discovery for process: {target_process}",
            enable_logging=self.enable_logging,
            log_prefix=self.jsonName,
        )

        best_render_thread = None
        all_candidates = []

        try:
            # Enhanced render thread detection - check for any XR/rendering operations
            # First, check for threads with XR operations (main clue for render threads)
            xr_patterns = [
                "xrWaitFrame%",
                "xrBeginFrame%",
                "xrEndFrame%",
                "%CompositorOpenXR::%",
                "%CompositorVRAPI::%",
                "RCBeginFrame%",
                "RCEndFrame%",
                "!xrEndFrame%",  # The actual render completion marker
            ]

            MetricAPI.write_to_log(
                f"Phase 2: Enhanced render thread detection with {len(xr_patterns)} XR patterns",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )

            for pattern_idx, pattern in enumerate(xr_patterns):
                try:
                    MetricAPI.write_to_log(
                        f"Checking XR pattern {pattern_idx + 1}/{len(xr_patterns)}: {pattern}",
                        enable_logging=self.enable_logging,
                        log_prefix=self.jsonName,
                    )

                    # Query slices matching this XR pattern
                    render_query = f"""
                    SELECT DISTINCT
                        thread.name as thread_name,
                        thread.utid,
                        process.name as process_name,
                        COUNT(DISTINCT slice.id) as slice_count
                    FROM slice
                    LEFT JOIN thread_track ON slice.track_id = thread_track.id
                    LEFT JOIN thread ON thread_track.utid = thread.utid
                    LEFT JOIN process ON thread.upid = process.upid
                    WHERE slice.name LIKE '{pattern}'
                      AND ((process.name NOT LIKE 'com.oculus%') OR (process.name LIKE '{target_process}%'))
                      AND thread.name IS NOT NULL
                      AND thread.name != ''
                    GROUP BY thread.name, thread.utid, process.name
                    HAVING COUNT(DISTINCT slice.id) > 0
                    ORDER BY slice_count DESC
                    """

                    qr_it = self.trace_processor.query(render_query)
                    pattern_results = qr_it.as_pandas_dataframe()

                    if not pattern_results.empty:
                        for _, thread_row in pattern_results.iterrows():
                            thread_candidate = {
                                "thread_name": thread_row["thread_name"],
                                "utid": thread_row["utid"],
                                "process_name": thread_row["process_name"],
                                "pattern": pattern,
                                "slice_count": thread_row["slice_count"],
                            }

                            # Calculate score for this candidate
                            score = thread_row["slice_count"]

                            # Boost score for preferred patterns
                            if "!xrEndFrame" in pattern:
                                score += (
                                    2000  # Highest priority - actual render completion
                                )
                            elif "CompositorOpenXR" in pattern:
                                score += 1500
                            elif "xrEndFrame" in pattern:
                                score += 1000
                            elif "xrBeginFrame" in pattern or "xrWaitFrame" in pattern:
                                score += 800
                            elif "RCBeginFrame" in pattern or "RCEndFrame" in pattern:
                                score += 600
                            elif "Compositor" in pattern:
                                score += 400

                            # Thread name heuristics
                            thread_name = thread_candidate["thread_name"]
                            if "render" in thread_name.lower():
                                score += 300
                            elif (
                                thread_name.startswith("Thread-")
                                and thread_name != "Thread-1"
                            ):
                                score += (
                                    200  # Unity worker threads often handle rendering
                                )

                            all_candidates.append({**thread_candidate, "score": score})

                            MetricAPI.write_to_log(
                                f"Found candidate: {thread_name} (pattern: {pattern}, slices: {thread_row['slice_count']}, score: {score})",
                                enable_logging=self.enable_logging,
                                log_prefix=self.jsonName,
                            )

                except Exception as e:
                    MetricAPI.write_to_log(
                        f"Error checking XR pattern '{pattern}': {e}",
                        enable_logging=self.enable_logging,
                        log_prefix=self.jsonName,
                    )
                    continue

            # Sort all candidates by score and select the best
            if all_candidates:
                # Remove duplicates by thread name, keeping highest score
                unique_candidates = {}
                for candidate in all_candidates:
                    thread_name = candidate["thread_name"]
                    if (
                        thread_name not in unique_candidates
                        or candidate["score"] > unique_candidates[thread_name]["score"]
                    ):
                        unique_candidates[thread_name] = candidate

                # Sort by score
                sorted_candidates = sorted(
                    unique_candidates.values(), key=lambda x: x["score"], reverse=True
                )

                MetricAPI.write_to_log(
                    f"Phase 2: Found {len(sorted_candidates)} unique render thread candidates:",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )

                for i, candidate in enumerate(sorted_candidates[:5]):  # Log top 5
                    MetricAPI.write_to_log(
                        f"  {i + 1}. {candidate['thread_name']} (score: {candidate['score']}, pattern: {candidate['pattern']}, slices: {candidate['slice_count']})",
                        enable_logging=self.enable_logging,
                        log_prefix=self.jsonName,
                    )

                # Validate the top candidate
                best_candidate = sorted_candidates[0]
                if self.validate_render_thread(best_candidate, target_process):
                    best_render_thread = best_candidate["thread_name"]
                    MetricAPI.write_to_log(
                        f"Phase 2: Selected render thread: {best_render_thread} (score: {best_candidate['score']})",
                        enable_logging=self.enable_logging,
                        log_prefix=self.jsonName,
                    )
                else:
                    MetricAPI.write_to_log(
                        f"Phase 2: Top candidate {best_candidate['thread_name']} failed validation",
                        enable_logging=self.enable_logging,
                        log_prefix=self.jsonName,
                    )

            if not best_render_thread:
                MetricAPI.write_to_log(
                    "Phase 2: No valid render thread found with XR patterns",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )

            return best_render_thread

        except Exception as e:
            MetricAPI.write_to_log(
                f"Error in render thread discovery: {e}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return None

    def validate_render_thread(self, render_thread_candidate, target_process):
        """
        Phase 2: Validate that the discovered render thread is legitimate by checking:
        - Thread belongs to target process
        - Contains expected rendering operations
        - Has reasonable activity patterns during frame intervals
        """
        try:
            thread_name = render_thread_candidate["thread_name"]
            process_name = render_thread_candidate["process_name"]
            slice_count = render_thread_candidate["slice_count"]

            # Basic validation: process name matching
            if target_process not in process_name and not process_name.startswith(
                target_process
            ):
                # Allow some flexibility for process name matching
                if not (
                    target_process == "com.oculus.UOIAssets"
                    and "oculus" in process_name.lower()
                ):
                    MetricAPI.write_to_log(
                        f"Render thread candidate rejected: process mismatch ({process_name} vs {target_process})",
                        enable_logging=self.enable_logging,
                        log_prefix=self.jsonName,
                    )
                    return False

            # Minimum activity threshold
            if slice_count < 10:
                MetricAPI.write_to_log(
                    f"Render thread candidate rejected: insufficient activity ({slice_count} slices)",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )
                return False

            # Thread name heuristics - boost confidence for render-related names
            confidence_score = 0.5  # Base score

            render_name_indicators = [
                "render",
                "Render",
                "RENDER",
                "CompositorOpenXR",
                "CompositorVRAPI",
                "Compositor",
                "EndFrame",
                "SubmitFrame",
                "Present",
                "GPU",
                "Graphics",
                "VSync",
            ]

            for indicator in render_name_indicators:
                if indicator in thread_name:
                    confidence_score += 0.2
                    break

            # High activity boost
            if slice_count > 100:
                confidence_score += 0.1
            elif slice_count > 50:
                confidence_score += 0.05

            # Final validation threshold
            if confidence_score >= 0.6:
                MetricAPI.write_to_log(
                    f"Render thread validated: {thread_name} (confidence: {confidence_score:.2f})",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )
                return True
            else:
                MetricAPI.write_to_log(
                    f"Render thread candidate rejected: low confidence ({confidence_score:.2f})",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )
                return False

        except Exception as e:
            MetricAPI.write_to_log(
                f"Error validating render thread: {e}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return False

    def correlate_render_thread_frames(self, main_thread_frames, render_thread_utid):
        """
        Phase 2: Correlate render thread activities with main thread frame boundaries
        to improve frame detection accuracy.
        """
        if main_thread_frames.empty or not render_thread_utid:
            MetricAPI.write_to_log(
                "Cannot correlate render thread frames: insufficient data",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return {"correlation_score": 0.0, "aligned_frames": 0}

        try:
            MetricAPI.write_to_log(
                f"Phase 2: Starting render thread correlation for utid: {render_thread_utid}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )

            # Get render thread activities during main thread frame intervals
            correlation_results = []
            aligned_frame_count = 0

            for i, (_, main_frame) in enumerate(main_thread_frames.head(20).iterrows()):
                frame_start = main_frame["ts"]
                frame_end = frame_start + main_frame.get(
                    "frame_dur", main_frame.get("dur", 0)
                )

                # Query render thread activities in this frame interval
                render_activity_query = f"""
                SELECT COUNT(*) as activity_count, SUM(slice.dur) as total_duration
                FROM slice
                LEFT JOIN thread_track ON slice.track_id = thread_track.id
                WHERE thread_track.utid = {render_thread_utid}
                  AND slice.ts >= {frame_start}
                  AND slice.ts <= {frame_end}
                """

                qr_it = self.trace_processor.query(render_activity_query)
                activity_df = qr_it.as_pandas_dataframe()

                if not activity_df.empty and activity_df["activity_count"].iloc[0] > 0:
                    activity_count = activity_df["activity_count"].iloc[0]
                    total_duration = activity_df["total_duration"].iloc[0] or 0

                    correlation_results.append(
                        {
                            "frame_index": i,
                            "activity_count": activity_count,
                            "total_duration": total_duration,
                            "frame_duration": frame_end - frame_start,
                        }
                    )

                    if activity_count > 0:
                        aligned_frame_count += 1

            # Calculate correlation quality
            total_frames_checked = min(20, len(main_thread_frames))
            correlation_score = (
                aligned_frame_count / total_frames_checked
                if total_frames_checked > 0
                else 0.0
            )

            # Log correlation results
            MetricAPI.write_to_log(
                f"Render thread correlation: {aligned_frame_count}/{total_frames_checked} frames aligned (score: {correlation_score:.2f})",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )

            return {
                "correlation_score": correlation_score,
                "aligned_frames": aligned_frame_count,
                "total_frames_checked": total_frames_checked,
                "correlation_details": correlation_results[
                    :5
                ],  # Keep first 5 for logging
            }

        except Exception as e:
            MetricAPI.write_to_log(
                f"Error correlating render thread frames: {e}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return {"correlation_score": 0.0, "aligned_frames": 0}

    @staticmethod
    def get_category_case_sql():
        """
        Get the SQL CASE statement for categorizing slices by function type.
        Centralized to avoid duplication across queries.

        Returns:
            SQL CASE statement as a string
        """
        return """
            CASE
                WHEN slice.name LIKE '%Render%' OR slice.name LIKE '%Graphics%' OR slice.name LIKE '%Draw%' OR slice.name LIKE '%Camera%' THEN 'rendering'
                WHEN slice.name LIKE '%Physics%' OR slice.name LIKE '%Collision%' THEN 'physics'
                WHEN slice.name LIKE '%Script%' OR slice.name LIKE '%Update%' OR slice.name LIKE '%Behaviour%' THEN 'scripting'
                WHEN slice.name LIKE '%I/O%' OR slice.name LIKE '%File%' OR slice.name LIKE '%Load%' THEN 'io'
                WHEN slice.name LIKE '%Memory%' OR slice.name LIKE '%GC%' OR slice.name LIKE '%Alloc%' THEN 'memory'
                WHEN slice.name LIKE '%Audio%' OR slice.name LIKE '%Sound%' THEN 'audio'
                WHEN slice.name LIKE '%Network%' OR slice.name LIKE '%Socket%' THEN 'network'
                ELSE 'other'
            END as category
        """.strip()

    def find_slice_ancestors_and_descendants(
        self, main_thread_utid, slice_start, slice_end, slice_name
    ):
        """
        Phase 4 Enhancement: Find ancestor and descendant slices for a given function slice.

        Ancestor: A slice that starts before and ends after the target slice (contains it)
        Descendant: A slice that starts after and ends before the target slice (contained within it)
        """
        try:
            # Query for potential ancestor slices (contain the target slice)
            ancestor_query = f"""
            SELECT slice.name
            FROM slice
            LEFT JOIN thread_track ON slice.track_id = thread_track.id
            WHERE thread_track.utid = {main_thread_utid}
              AND slice.ts < {slice_start}
              AND (slice.ts + slice.dur) > {slice_end}
              AND slice.name != '{slice_name.replace("'", "''")}'
            ORDER BY slice.dur DESC
            LIMIT 1
            """

            ancestor_qr = self.trace_processor.query(ancestor_query)
            ancestor_df = ancestor_qr.as_pandas_dataframe()
            ancestor_name = (
                ancestor_df["name"].iloc[0] if not ancestor_df.empty else None
            )

            # Query for potential descendant slices (contained within the target slice)
            descendant_query = f"""
            SELECT slice.name
            FROM slice
            LEFT JOIN thread_track ON slice.track_id = thread_track.id
            WHERE thread_track.utid = {main_thread_utid}
              AND slice.ts > {slice_start}
              AND (slice.ts + slice.dur) < {slice_end}
              AND slice.name != '{slice_name.replace("'", "''")}'
            ORDER BY slice.dur DESC
            LIMIT 1
            """

            descendant_qr = self.trace_processor.query(descendant_query)
            descendant_df = descendant_qr.as_pandas_dataframe()
            descendant_name = (
                descendant_df["name"].iloc[0] if not descendant_df.empty else None
            )

            return {"ancestor": ancestor_name, "descendant": descendant_name}

        except Exception as e:
            MetricAPI.write_to_log(
                f"Error finding slice ancestors/descendants for {slice_name}: {e}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return {"ancestor": None, "descendant": None}

    def build_hierarchical_slice_breakdown(
        self, main_thread_utid, frame_start, frame_end, max_depth=3, top_n=10
    ):
        """
        Build a hierarchical breakdown of slices on the main thread with top N slices at each depth level.
        Groups slices by name, showing call counts and accumulated durations.

        Args:
            main_thread_utid: The unique thread ID for the main thread
            frame_start: Frame start timestamp in nanoseconds
            frame_end: Frame end timestamp in nanoseconds
            max_depth: Maximum depth to traverse (default 3)
            top_n: Number of top slices to include at each level (default 10)

        Returns:
            Dictionary containing hierarchical slice breakdown with call counts
        """
        try:
            frame_duration = frame_end - frame_start
            if frame_duration <= 0:
                MetricAPI.write_to_log(
                    "Invalid frame duration for hierarchical breakdown",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )
                return {"slices": [], "total_depth": 0}

            def get_child_slices_aggregated(parent_start, parent_end, parent_id=None):
                """
                Get direct child slices grouped by name with call counts and accumulated durations.
                """
                # Build query based on whether we're looking for children of a specific parent
                # or top-level slices with no parent
                if parent_id is not None:
                    # Use parent_id to find direct children
                    parent_filter = f"AND slice.parent_id = {parent_id}"
                else:
                    # Top-level slices (no parent, start within time bounds)
                    parent_filter = f"""
                      AND slice.ts >= {parent_start}
                      AND slice.ts <= {parent_end}
                      AND slice.parent_id IS NULL
                    """.strip()

                # Query with aggregation by name
                # Note: We need to use a subquery to calculate category per slice, then group
                child_query = f"""
                WITH categorized_slices AS (
                    SELECT
                        slice.id,
                        slice.name,
                        slice.ts,
                        slice.dur,
                        slice.parent_id,
                        {MetricAPI.get_category_case_sql()}
                    FROM slice
                    LEFT JOIN thread_track ON slice.track_id = thread_track.id
                    WHERE thread_track.utid = {main_thread_utid}
                      {parent_filter}
                      AND slice.dur > 0
                )
                SELECT
                    name,
                    COUNT(*) as call_count,
                    GROUP_CONCAT(dur) as all_dur,
                    GROUP_CONCAT(id) as all_id,
                    GROUP_CONCAT(ts) as all_ts,
                    MIN(category) as category
                FROM categorized_slices
                GROUP BY name
                ORDER BY ts ASC
                LIMIT {top_n}
                """

                qr_it = self.trace_processor.query(child_query)
                pdf = qr_it.as_pandas_dataframe()

                pdf["all_id"] = pdf["all_id"].str.split(",")
                pdf["all_dur"] = pdf["all_dur"].str.split(",")
                pdf["all_ts"] = pdf["all_ts"].str.split(",")

                # Verify lengths
                pdf["lengths_match"] = (
                    pdf["all_id"].str.len() == pdf["all_ts"].str.len()
                ) & (pdf["all_ts"].str.len() == pdf["all_dur"].str.len())

                assert pdf["lengths_match"].all(), (
                    "get_child_slices_aggregated rows have mismatched lengths"
                )

                MetricAPI.write_to_log(
                    "get_child_slices_aggregated",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )

                MetricAPI.write_to_log(
                    pdf,
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )

                return pdf

            def build_hierarchy(
                parent_start, parent_end, parent_id, current_depth, frame_duration_ns
            ):
                """Recursively build hierarchy up to max_depth with aggregated slice data."""
                if current_depth > max_depth:
                    return []

                children_df = get_child_slices_aggregated(
                    parent_start, parent_end, parent_id
                )

                if children_df.empty:
                    return []

                MetricAPI.write_to_log(
                    f"get_child_slices_aggregated post {current_depth} parent id: {parent_id}",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )

                result = []
                for _, child in children_df.iterrows():
                    total_dur = sum(int(x) for x in child["all_dur"])
                    call_count = int(child["call_count"])

                    child_info = {
                        "name": child["name"],
                        "call_count": call_count,
                        "total_duration_ns": total_dur,
                        "total_duration_ms": total_dur / 1000000.0,
                        "frame_percentage": (total_dur / frame_duration_ns) * 100.0,
                        "category": child["category"],
                        "depth": current_depth,
                        "children": [],
                    }

                    # Recursively get children if not at max depth
                    # Use the representative_id (first occurrence) to find children
                    if current_depth < max_depth:
                        ids = child["all_id"]
                        dur_list = child["all_dur"]
                        ts_list = child["all_ts"]

                        for id_val, ts_val, dur_val in zip(ids, ts_list, dur_list):
                            representative_id = id_val
                            first_ts = int(ts_val)

                            # For finding children, we use the representative slice's time bounds
                            # but we'll aggregate all children across all instances of this parent
                            child_info["children"] = build_hierarchy(
                                first_ts,
                                first_ts
                                + int(dur_val),  # Use average duration as approximation
                                representative_id,
                                current_depth + 1,
                                frame_duration_ns,
                            )

                    result.append(child_info)

                return result

            # Start building hierarchy from frame boundaries
            hierarchy = build_hierarchy(
                frame_start,
                frame_end,
                None,  # No parent for top-level
                1,  # Start at depth 1
                frame_duration,
            )

            MetricAPI.write_to_log(
                f"Built hierarchical breakdown with {len(hierarchy)} top-level slice groups, max depth {max_depth}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )

            return {
                "slices": hierarchy,
                "max_depth": max_depth,
                "top_n_per_level": top_n,
                "frame_duration_ms": frame_duration / 1000000.0,
            }

        except Exception as e:
            MetricAPI.write_to_log(
                f"Error building hierarchical slice breakdown: {e}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return {"slices": [], "max_depth": 0}

    def summarize_thread_states(self, function_analysis):
        """
        Phase 4 Enhancement: Summarize thread state patterns across all analyzed functions.
        """
        try:
            if not function_analysis:
                return {
                    "busy_running_count": 0,
                    "preempted_count": 0,
                    "waiting_for_lock_count": 0,
                    "mixed_execution_count": 0,
                    "io_bound_count": 0,
                    "other_count": 0,
                }

            summary = {
                "busy_running_count": 0,
                "preempted_count": 0,
                "waiting_for_lock_count": 0,
                "mixed_execution_count": 0,
                "io_bound_count": 0,
                "other_count": 0,
            }

            for func in function_analysis:
                analysis = func.get("thread_state", {}).get("analysis", "unknown")

                if analysis == "busy_running":
                    summary["busy_running_count"] += 1
                elif analysis == "preempted":
                    summary["preempted_count"] += 1
                elif analysis == "waiting_for_lock":
                    summary["waiting_for_lock_count"] += 1
                elif analysis == "mixed_execution":
                    summary["mixed_execution_count"] += 1
                elif analysis == "io_bound":
                    summary["io_bound_count"] += 1
                else:
                    summary["other_count"] += 1

            return summary

        except Exception as e:
            MetricAPI.write_to_log(
                f"Error summarizing thread states: {e}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return {
                "busy_running_count": 0,
                "preempted_count": 0,
                "waiting_for_lock_count": 0,
                "mixed_execution_count": 0,
                "io_bound_count": 0,
                "other_count": 0,
            }

    def assess_frame_function_quality(self, all_slices, long_functions, frame_duration):
        """
        Phase 4: Assess the quality of function analysis within a frame.
        Returns confidence score based on data completeness and consistency.
        """
        try:
            if all_slices.empty:
                return 0.0

            quality_score = 0.5  # Base score

            # Data completeness check
            total_slice_time = all_slices["dur"].sum()
            coverage_percentage = (total_slice_time / frame_duration) * 100.0

            if coverage_percentage >= 80.0:
                quality_score += 0.2
            elif coverage_percentage >= 60.0:
                quality_score += 0.1

            # Function diversity check
            unique_functions = len(all_slices["name"].unique())
            if unique_functions >= 20:
                quality_score += 0.1
            elif unique_functions >= 10:
                quality_score += 0.05

            # Long function analysis quality
            if not long_functions.empty:
                long_function_coverage = (
                    long_functions["dur"].sum() / frame_duration
                ) * 100.0
                if 10.0 <= long_function_coverage <= 70.0:  # Reasonable range
                    quality_score += 0.15
                elif long_function_coverage > 0:
                    quality_score += 0.05

            # Category distribution check
            categories = all_slices.groupby("category").size()
            if len(categories) >= 3:  # Multiple categories present
                quality_score += 0.05

            return min(1.0, quality_score)

        except Exception as e:
            MetricAPI.write_to_log(
                f"Error assessing frame function quality: {e}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return 0.0

    def categorize_functions(self, functions_df):
        """
        Phase 4: Categorize functions by type and return summary statistics.
        """
        try:
            if functions_df.empty:
                return {}

            category_stats = {}
            for category in functions_df["category"].unique():
                category_funcs = functions_df[functions_df["category"] == category]
                category_stats[category] = {
                    "count": len(category_funcs),
                    "total_time_ms": (category_funcs["dur"].sum() / 1000000.0),
                    "avg_time_ms": (category_funcs["dur"].mean() / 1000000.0),
                    "max_time_ms": (category_funcs["dur"].max() / 1000000.0),
                }

            return category_stats

        except Exception as e:
            MetricAPI.write_to_log(
                f"Error categorizing functions: {e}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return {}

    def analyze_high_frequency_functions(self, target_threads, frame_start, frame_end):
        """
        Phase 4.2: Detect functions with high call frequency that may cause performance issues.
        Reuses existing thread identification and frame boundary detection.

        Args:
            target_threads: Dictionary with thread types and their utids {'main': utid, 'render': utid}
            frame_start: Start timestamp of frame interval
            frame_end: End timestamp of frame interval
        """
        try:
            MetricAPI.write_to_log(
                "Phase 4.2: Starting high frequency function analysis",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )

            high_freq_results = {}
            frame_duration_ms = (frame_end - frame_start) / 1000000.0

            # Analyze both main thread and render thread for high frequency calls
            for thread_type, thread_utid in target_threads.items():
                if thread_type in ["main", "render"] and thread_utid is not None:
                    MetricAPI.write_to_log(
                        f"Phase 4.2: Analyzing {thread_type} thread (utid: {thread_utid})",
                        enable_logging=self.enable_logging,
                        log_prefix=self.jsonName,
                    )

                    # Get function frequency statistics for this thread
                    function_stats = self.query_function_frequency(
                        thread_utid, frame_start, frame_end
                    )

                    if function_stats:
                        # Classify functions by frequency severity
                        classified_functions = self.classify_frequency_issues(
                            function_stats
                        )
                        high_freq_results[thread_type] = classified_functions

                        # Log summary for this thread
                        total_critical = len(classified_functions["critical"])
                        total_high = len(classified_functions["high"])
                        total_warning = len(classified_functions["warning"])

                        MetricAPI.write_to_log(
                            f"Phase 4.2: {thread_type} thread high frequency summary: "
                            f"Critical={total_critical}, High={total_high}, Warning={total_warning}",
                            enable_logging=self.enable_logging,
                            log_prefix=self.jsonName,
                        )

                        # Log top critical functions for debugging
                        if total_critical > 0:
                            for i, func in enumerate(
                                classified_functions["critical"][:3]
                            ):
                                MetricAPI.write_to_log(
                                    f"  Critical {i + 1}: {func['function_name']} - {func['call_count']} calls, {func['total_duration_ms']:.2f}ms total",
                                    enable_logging=self.enable_logging,
                                    log_prefix=self.jsonName,
                                )
                    else:
                        # No high frequency functions found for this thread
                        high_freq_results[thread_type] = {
                            "critical": [],
                            "high": [],
                            "warning": [],
                            "normal": [],
                        }
                        MetricAPI.write_to_log(
                            f"Phase 4.2: No high frequency functions found for {thread_type} thread",
                            enable_logging=self.enable_logging,
                            log_prefix=self.jsonName,
                        )

            # Calculate overall analysis quality
            analysis_quality = self.assess_high_frequency_quality(
                high_freq_results, frame_duration_ms
            )

            MetricAPI.write_to_log(
                f"Phase 4.2: High frequency analysis completed with quality {analysis_quality:.2f}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )

            return {
                "high_frequency_functions": high_freq_results,
                "analysis_quality": analysis_quality,
                "frame_duration_ms": frame_duration_ms,
                "threads_analyzed": list(target_threads.keys()),
            }

        except Exception as e:
            MetricAPI.write_to_log(
                f"Error in high frequency function analysis: {e}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return {
                "high_frequency_functions": {},
                "analysis_quality": 0.0,
                "frame_duration_ms": 0.0,
                "threads_analyzed": [],
            }

    def query_function_frequency(self, thread_utid, frame_start, frame_end):
        """
        Phase 4.2: Get all slices in specified thread within time period, dedup by slice name,
        and sort by most occurrence (highest call frequency).
        """
        try:
            query = """
            -- Get all slices in specified thread within frame time period
            WITH thread_slices AS (
                SELECT
                    s.name as function_name,
                    s.dur,
                    s.depth,
                    s.ts,
                    thread.name as thread_name,
                    process.name as process_name
                FROM slice s
                JOIN thread_track tt ON s.track_id = tt.id
                JOIN thread ON tt.utid = thread.utid
                JOIN process ON thread.upid = process.upid
                WHERE tt.utid = ?                    -- Filter by specific thread utid
                    AND s.ts >= ? AND s.ts < ?       -- Filter by frame time period
                    AND s.dur > 0                    -- Only meaningful durations
                    AND s.name IS NOT NULL           -- Only named functions
                    AND s.name != ''                 -- Exclude empty names
                    AND s.depth <= 3                 -- Limit to top 3 call stack levels
            ),
            -- Aggregate and dedup by function name, sort by occurrence count
            function_frequency_stats AS (
                SELECT
                    function_name,
                    COUNT(*) as call_count,                    -- Total number of calls
                    SUM(dur) / 1e6 as total_duration_ms,      -- Total time spent in function
                    AVG(dur) / 1e6 as avg_duration_ms,        -- Average time per call
                    MIN(dur) / 1e6 as min_duration_ms,        -- Fastest call
                    MAX(dur) / 1e6 as max_duration_ms,        -- Slowest call
                    AVG(depth) as avg_depth,                   -- Average call stack depth
                    MIN(thread_name) as thread_name,    -- Thread name for reference (using MIN instead of ANY_VALUE)
                    MIN(process_name) as process_name   -- Process name for reference (using MIN instead of ANY_VALUE)
                FROM thread_slices
                GROUP BY function_name                         -- Dedup by function name
                HAVING call_count > 10                         -- Lower threshold to capture more functions
            )
            SELECT
                function_name,
                call_count,
                total_duration_ms,
                avg_duration_ms,
                min_duration_ms,
                max_duration_ms,
                avg_depth,
                thread_name,
                process_name,
                -- Calculate frequency severity classification
                CASE
                    WHEN call_count > 1000 THEN 'CRITICAL'
                    WHEN call_count > 500 THEN 'HIGH'
                    WHEN call_count > 100 THEN 'WARNING'
                    ELSE 'NORMAL'
                END as frequency_severity
            FROM function_frequency_stats
            ORDER BY call_count DESC                          -- Sort by most occurrences first
            LIMIT 50;                                         -- Top 50 most frequent functions
            """

            # Format the query with parameters directly (Perfetto doesn't support parameterized queries)
            formatted_query = query.replace("?", "{}").format(
                thread_utid, frame_start, frame_end
            )
            qr_it = self.trace_processor.query(formatted_query)
            results_df = qr_it.as_pandas_dataframe()

            if results_df.empty:
                MetricAPI.write_to_log(
                    f"No high frequency functions found for thread utid {thread_utid}",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )
                return []

            # Convert DataFrame to list of dictionaries for easier processing
            function_stats = []
            for _, row in results_df.iterrows():
                function_stats.append(
                    {
                        "function_name": row["function_name"],
                        "call_count": int(row["call_count"]),
                        "total_duration_ms": float(row["total_duration_ms"]),
                        "avg_duration_ms": float(row["avg_duration_ms"]),
                        "min_duration_ms": float(row["min_duration_ms"]),
                        "max_duration_ms": float(row["max_duration_ms"]),
                        "avg_depth": float(row["avg_depth"]),
                        "thread_name": row["thread_name"],
                        "process_name": row["process_name"],
                        "frequency_severity": row["frequency_severity"],
                    }
                )

            MetricAPI.write_to_log(
                f"Found {len(function_stats)} high frequency functions for thread utid {thread_utid}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )

            return function_stats

        except Exception as e:
            MetricAPI.write_to_log(
                f"Error querying function frequency for thread {thread_utid}: {e}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return []

    def classify_frequency_issues(self, function_stats):
        """
        Phase 4.2: Classify functions by frequency severity and add frequency status.
        """
        try:
            classified = {
                "critical": [],  # >1000 calls
                "high": [],  # >500 calls
                "warning": [],  # >100 calls
                "normal": [],  # 50-100 calls
            }

            for func in function_stats:
                call_count = func["call_count"]

                # Add frequency status based on call count
                func["frequency_status"] = self.get_frequency_status(call_count)

                # Classify into severity buckets
                if call_count > 1000:
                    classified["critical"].append(func)
                elif call_count > 500:
                    classified["high"].append(func)
                elif call_count > 100:
                    classified["warning"].append(func)
                else:
                    classified["normal"].append(func)

            return classified

        except Exception as e:
            MetricAPI.write_to_log(
                f"Error classifying frequency issues: {e}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return {"critical": [], "high": [], "warning": [], "normal": []}

    def get_frequency_status(self, call_count):
        """
        Phase 4.2: Get descriptive status for function call frequency.
        """
        if call_count > 1000:
            return "critical_frequency"
        elif call_count > 500:
            return "high_frequency"
        elif call_count > 100:
            return "warning_frequency"
        else:
            return "normal_frequency"

    def assess_high_frequency_quality(self, high_freq_results, frame_duration_ms):
        """
        Phase 4.2: Assess the quality of high frequency function analysis.
        """
        try:
            if not high_freq_results:
                return 0.0

            quality_score = 0.5  # Base score

            # Check if we have data from both main and render threads
            threads_with_data = 0
            total_functions_found = 0

            for _thread_type, results in high_freq_results.items():
                thread_total = sum(len(results[severity]) for severity in results)
                if thread_total > 0:
                    threads_with_data += 1
                    total_functions_found += thread_total

            # Multi-thread analysis bonus
            if threads_with_data >= 2:
                quality_score += 0.2
            elif threads_with_data >= 1:
                quality_score += 0.1

            # Function count quality
            if total_functions_found >= 20:
                quality_score += 0.2
            elif total_functions_found >= 10:
                quality_score += 0.1

            # Frame duration quality (reasonable frame time)
            if 10.0 <= frame_duration_ms <= 50.0:  # 20-100 FPS range
                quality_score += 0.1

            return min(1.0, quality_score)

        except Exception as e:
            MetricAPI.write_to_log(
                f"Error assessing high frequency quality: {e}",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )
            return 0.0

    def get_main_buffer_pixel_count(self):
        base_surface_df = self.get_main_color_pass_df()
        json_result = {
            "entries": 0,
            "mean": 0.0,
            "std": 0.0,
            "max": 0.0,
            "quantile_75": 0.0,
        }

        if base_surface_df is not None and not base_surface_df.empty:
            try:
                # Check if  columns exist and are not empty
                if (
                    "width" in base_surface_df.columns
                    and "height" in base_surface_df.columns
                ):
                    base_surface_df["pixel_count"] = base_surface_df.width.astype(
                        int
                    ) * base_surface_df.height.astype(int)

                    json_result = {
                        "entries": len(base_surface_df["pixel_count"]),
                        "mean": (np.mean(base_surface_df["pixel_count"])).item(),
                        "std": (np.std(base_surface_df["pixel_count"])).item(),
                        "max": (np.max(base_surface_df["pixel_count"])).item(),
                        "quantile_75": (
                            np.quantile(base_surface_df["pixel_count"], 0.75)
                        ).item(),
                    }
                else:
                    MetricAPI.write_to_log(
                        "Missing width/height columns for pixel count calculation",
                        enable_logging=self.enable_logging,
                        log_prefix=self.jsonName,
                    )
            except Exception as e:
                MetricAPI.write_to_log(
                    f"Error calculating pixel count: {e}",
                    enable_logging=self.enable_logging,
                    log_prefix=self.jsonName,
                )
        else:
            MetricAPI.write_to_log(
                "Empty or None surface DataFrame, using default pixel count",
                enable_logging=self.enable_logging,
                log_prefix=self.jsonName,
            )

        return json_result


def get_instructions_per_unit(unit_per_second, instruction_per_second):
    """Calculate instructions per unit with safe division handling.

    Returns None if the calculation fails, allowing callers to skip adding this to the result.
    """
    try:
        # Check for required keys
        if not all(
            key in unit_per_second for key in ["entries", "mean", "max", "quantile_75"]
        ):
            return None
        if not all(
            key in instruction_per_second
            for key in ["entries", "mean", "max", "quantile_75"]
        ):
            return None

        # Check for zero divisors
        if (
            unit_per_second["mean"] == 0
            or unit_per_second["max"] == 0
            or unit_per_second["quantile_75"] == 0
        ):
            return None

        json_result = {
            "entries": min(
                instruction_per_second["entries"], unit_per_second["entries"]
            ),
            "mean": instruction_per_second["mean"] / unit_per_second["mean"],
            "std": max(instruction_per_second["entries"], unit_per_second["entries"]),
            "max": instruction_per_second["max"] / unit_per_second["max"],
            "quantile_75": instruction_per_second["quantile_75"]
            / unit_per_second["quantile_75"],
        }
        return json_result
    except Exception:
        return None


# this returns negative result where we can assume vertex shaded per-
def get_instructions_per_vertex(unit_per_second, instruction_per_second, reused_vertex):
    """Calculate instructions per vertex with safe division handling.

    Returns None if the calculation fails, allowing callers to skip adding this to the result.
    """
    try:
        # Check for required keys
        required_keys = ["entries", "mean", "max", "quantile_75"]
        if not all(key in unit_per_second for key in required_keys):
            return None
        if not all(key in instruction_per_second for key in required_keys):
            return None
        if not all(key in reused_vertex for key in required_keys):
            return None

        # Calculate divisors
        mean_divisor = unit_per_second["mean"] - reused_vertex["mean"]
        max_divisor = unit_per_second["max"] - reused_vertex["mean"]
        q75_divisor = unit_per_second["quantile_75"] - reused_vertex["quantile_75"]

        # Check for zero divisors
        if mean_divisor == 0 or max_divisor == 0 or q75_divisor == 0:
            return None

        json_result = {
            "entries": min(
                instruction_per_second["entries"],
                unit_per_second["entries"] - reused_vertex["entries"],
            ),
            "mean": instruction_per_second["mean"] / mean_divisor,
            "std": max(
                instruction_per_second["entries"],
                unit_per_second["entries"] - reused_vertex["mean"],
            ),
            "max": instruction_per_second["max"] / max_divisor,
            "quantile_75": instruction_per_second["quantile_75"] / q75_divisor,
        }
        return json_result
    except Exception:
        return None


def process_trace(tracePath, jsonPath, binPath="", enable_logging=False):
    """
    Process a Perfetto trace and return analysis results as a dictionary.

    This function can be called programmatically from other Python scripts to
    analyze Perfetto traces without using the main() function or command-line interface.

    Args:
        tracePath (str): Path to the Perfetto trace file
        jsonPath (str): Path where JSON output will be written (also used for log file naming)
        binPath (str, optional): Path to the shell bin. Defaults to "".
        enable_logging (bool, optional): Enable logging to file. Defaults to False.

    Returns:
        dict: JSON-serializable dictionary containing all metrics and analysis results.
              Returns empty dict if errors occur during validation.

    Example:
        >>> from MetricAPI import process_trace
        >>> result = process_trace(
        ...     tracePath="/path/to/trace.perfetto-trace",
        ...     jsonPath="/path/to/output.json",
        ...     enable_logging=True
        ... )
        >>> print(result['main_thread_frame_time'])
    """
    logPath = os.path.splitext(jsonPath)[0]

    if not os.path.exists(tracePath):
        MetricAPI.write_to_error_log(
            "%s is not a directory or file" % tracePath,
            clean=True,
            log_prefix=logPath,
        )
        return {}

    if not os.path.exists(os.path.dirname(jsonPath)):
        MetricAPI.write_to_error_log(
            "%s is not a directory or file" % jsonPath,
            clean=True,
            log_prefix=logPath,
        )
        return {}

    try:
        metric = MetricAPI(
            trace=tracePath,
            shell_bin_path=binPath,
            jsonPath=jsonPath,
            enable_logging=enable_logging,
        )

        json_result = {}

        main_thread_frame_time = metric.main_thread_frame_time(
            allow_process_name=metric.target_process
        )
        json_result["main_thread_frame_time"] = main_thread_frame_time

        # this basically matches the frame time
        MetricAPI.write_to_log(
            "App Render Thread Frame Time: ",
            enable_logging=metric.enable_logging,
            log_prefix=metric.jsonName,
        )

        frame_time_render = metric.render_thread_frame_time()

        json_result["render_thread_frame_time"] = frame_time_render

        MetricAPI.write_to_log(
            "App GPU Time2: ",
            enable_logging=metric.enable_logging,
            log_prefix=metric.jsonName,
        )
        gpu_time_tuple = metric.gpu_time2(allow_process_name=metric.target_process)

        json_result["app_gpu_time"] = gpu_time_tuple
        MetricAPI.write_to_log(
            gpu_time_tuple,
            enable_logging=metric.enable_logging,
            log_prefix=metric.jsonName,
        )

        top_frames_df = metric.get_top_main_thread_frame_time(
            int(len(metric.main_thread_frame_time_df) / 2)
        )

        # Check if top_frames_df is empty to avoid indexing errors
        if not top_frames_df.empty:
            json_result["top_main_thread_frame_time"] = (
                top_frames_df["frame_dur"].iloc[0] / 1000000
            )

            top_start_time = top_frames_df["ts"].iloc[0]
            top_end_time = (
                top_frames_df["ts"].iloc[0] + top_frames_df["frame_dur"].iloc[0]
            )

            MetricAPI.write_to_log(
                "Top Frame Time:  %.2f "
                % (top_frames_df["frame_dur"].iloc[0] / 1000000),
                enable_logging=metric.enable_logging,
                log_prefix=metric.jsonName,
            )
            MetricAPI.write_to_log(
                top_frames_df,
                enable_logging=metric.enable_logging,
                log_prefix=metric.jsonName,
            )
            ratio = metric.thread_running_ratio_in_ts(
                top_frames_df["utid"].iloc[0], top_start_time, top_end_time
            )

            mid_start_time = top_frames_df["ts"].tail(1).iloc[0]
            mid_end_time = (
                top_frames_df["ts"].tail(1).iloc[0]
                + top_frames_df["frame_dur"].tail(1).iloc[0]
            )

            ratio_mid = metric.thread_running_ratio_in_ts(
                top_frames_df["utid"].iloc[0], mid_start_time, mid_end_time
            )

            top_frame_core_use = metric.content_core_occupancy_in_ts(
                top_start_time, top_end_time
            )

            ratio["tname"] = top_frames_df["tname"].iloc[0]
            json_result["main_thread_top_frame_core"] = top_frame_core_use
            json_result["_main_thread_top_frame_core_description"] = (
                "CPU core occupancy breakdown showing which threads ran on which CPU cores during longest main thread frame"
            )
            json_result["main_thread_top_frame_run_ratio"] = ratio
            json_result["_main_thread_top_frame_run_ratio_description"] = (
                "CPU utilization statistics for main thread during the longest (worst) frame"
            )
            json_result["main_thread_mid_frame_run_ratio"] = ratio_mid
            json_result["_main_thread_mid_frame_run_ratio_description"] = (
                "CPU utilization statistics for main thread during a middle-performance frame"
            )

            tracks = metric.get_slices_within_time_range(
                top_start_time,
                top_end_time,
                keyword="",
                sort_by="dur",
                thread_utid=top_frames_df["utid"].iloc[0],
            )

            top5 = tracks.head(5)
            json_tracks = print_track_info(top5)

            json_result["top_frame_tracks"] = json_tracks
            json_result["_top_frame_tracks_description"] = (
                "Top 5 longest slices/operations during the worst (longest) main thread frame (sorted by duration)"
            )

            json_result["mid_frame_tracks"] = print_track_info(
                metric.get_slices_within_time_range(
                    mid_start_time,
                    mid_end_time,
                    keyword="",
                    sort_by="dur",
                ).head(5)
            )
            json_result["_mid_frame_tracks_description"] = (
                "Top 5 longest slices/operations during a middle-performance frame (sorted by duration)"
            )
            MetricAPI.write_to_log(
                json_tracks,
                enable_logging=metric.enable_logging,
                log_prefix=metric.jsonName,
            )

            # Phase 4: Enhanced Frame Analysis - Analyze long functions in top frame
            try:
                top_frame_main_thread_utid = top_frames_df["utid"].iloc[0]

                # Phase 4.3: Build hierarchical slice breakdown for Unity main thread

                try:
                    MetricAPI.write_to_log(
                        "Phase 4.3: Building hierarchical slice breakdown (depth=5, top_n=10)",
                        enable_logging=metric.enable_logging,
                        log_prefix=metric.jsonName,
                    )

                    hierarchical_breakdown = metric.build_hierarchical_slice_breakdown(
                        top_frame_main_thread_utid,
                        top_start_time,
                        top_end_time,
                        max_depth=5,
                        top_n=10,
                    )
                    json_result["main_thread_hierarchical_slice_breakdown"] = (
                        hierarchical_breakdown
                    )
                    json_result[
                        "_main_thread_hierarchical_slice_breakdown_description"
                    ] = "Hierarchical breakdown of function calls in the longest main thread frame"

                    # Log summary of hierarchical breakdown
                    total_slices = len(hierarchical_breakdown.get("slices", []))
                    MetricAPI.write_to_log(
                        f"Phase 4.3: Built hierarchical breakdown with {total_slices} top-level slices",
                        enable_logging=metric.enable_logging,
                        log_prefix=metric.jsonName,
                    )

                    # Log top 3 slices for debugging
                    for i, slice_info in enumerate(
                        hierarchical_breakdown.get("slices", [])[:3]
                    ):
                        child_count = len(slice_info.get("children", []))
                        # Handle both old and new format
                        if "total_duration_ms" in slice_info:
                            duration_ms = slice_info["total_duration_ms"]
                            call_count = slice_info.get("call_count", 1)
                            MetricAPI.write_to_log(
                                f"  Level 1 Slice {i + 1}: {slice_info['name']} - {duration_ms:.2f}ms ({slice_info['frame_percentage']:.1f}%) [{slice_info['category']}] - {call_count} calls, {child_count} children",
                                enable_logging=metric.enable_logging,
                                log_prefix=metric.jsonName,
                            )
                        else:
                            duration_ms = slice_info.get("duration_ms", 0)
                            MetricAPI.write_to_log(
                                f"  Level 1 Slice {i + 1}: {slice_info['name']} - {duration_ms:.2f}ms ({slice_info['frame_percentage']:.1f}%) [{slice_info['category']}] - {child_count} children",
                                enable_logging=metric.enable_logging,
                                log_prefix=metric.jsonName,
                            )

                except Exception as e:
                    MetricAPI.write_to_log(
                        f"Phase 4.3: Error building hierarchical breakdown: {e}",
                        enable_logging=metric.enable_logging,
                        log_prefix=metric.jsonName,
                    )
                    json_result["main_thread_hierarchical_slice_breakdown"] = {
                        "slices": [],
                        "max_depth": 0,
                        "top_n_per_level": 0,
                        "frame_duration_ms": 0.0,
                    }
                    json_result[
                        "_main_thread_hierarchical_slice_breakdown_description"
                    ] = "Hierarchical breakdown of function calls in the longest main thread frame"

                # Phase 4.4: Build hierarchical slice breakdown for render thread (if identified)
                try:
                    # Check if we have a render thread identified
                    if not metric.render_thread_frame_time_df.empty:
                        # Get render thread UTID and frame info
                        top_render_frames = metric.render_thread_frame_time_df.head(1)
                        render_thread_utid = top_render_frames["utid"].iloc[0]
                        render_start_time = top_render_frames["ts"].iloc[0]
                        render_end_time = (
                            top_render_frames["ts"].iloc[0]
                            + top_render_frames["frame_dur"].iloc[0]
                        )

                        MetricAPI.write_to_log(
                            "Phase 4.4: Building render thread hierarchical breakdown (depth=5, top_n=10)",
                            enable_logging=metric.enable_logging,
                            log_prefix=metric.jsonName,
                        )

                        render_hierarchical_breakdown = (
                            metric.build_hierarchical_slice_breakdown(
                                render_thread_utid,
                                render_start_time,
                                render_end_time,
                                max_depth=5,
                                top_n=10,
                            )
                        )
                        json_result["render_thread_hierarchical_breakdown"] = (
                            render_hierarchical_breakdown
                        )
                        json_result[
                            "_render_thread_hierarchical_breakdown_description"
                        ] = "Hierarchical breakdown of function calls in the longest render thread frame"

                        # Log summary
                        total_slices = len(
                            render_hierarchical_breakdown.get("slices", [])
                        )
                        MetricAPI.write_to_log(
                            f"Phase 4.4: Built render thread hierarchical breakdown with {total_slices} top-level slices",
                            enable_logging=metric.enable_logging,
                            log_prefix=metric.jsonName,
                        )

                        # Log top 3 slices for debugging
                        for i, slice_info in enumerate(
                            render_hierarchical_breakdown.get("slices", [])[:3]
                        ):
                            child_count = len(slice_info.get("children", []))
                            # Handle both old and new format
                            if "total_duration_ms" in slice_info:
                                duration_ms = slice_info["total_duration_ms"]
                                call_count = slice_info.get("call_count", 1)
                                MetricAPI.write_to_log(
                                    f"  Level 1 Slice {i + 1}: {slice_info['name']} - {duration_ms:.2f}ms ({slice_info['frame_percentage']:.1f}%) [{slice_info['category']}] - {call_count} calls, {child_count} children",
                                    enable_logging=metric.enable_logging,
                                    log_prefix=metric.jsonName,
                                )
                            else:
                                duration_ms = slice_info.get("duration_ms", 0)
                                MetricAPI.write_to_log(
                                    f"  Level 1 Slice {i + 1}: {slice_info['name']} - {duration_ms:.2f}ms ({slice_info['frame_percentage']:.1f}%) [{slice_info['category']}] - {child_count} children",
                                    enable_logging=metric.enable_logging,
                                    log_prefix=metric.jsonName,
                                )
                    else:
                        MetricAPI.write_to_log(
                            "Phase 4.4: No render thread identified, skipping render thread hierarchical breakdown",
                            enable_logging=metric.enable_logging,
                            log_prefix=metric.jsonName,
                        )
                        json_result["render_thread_hierarchical_breakdown"] = {
                            "slices": [],
                            "max_depth": 0,
                            "top_n_per_level": 0,
                            "frame_duration_ms": 0.0,
                        }

                except Exception as e:
                    MetricAPI.write_to_log(
                        f"Phase 4.4: Error building render thread hierarchical breakdown: {e}",
                        enable_logging=metric.enable_logging,
                        log_prefix=metric.jsonName,
                    )
                    json_result["render_thread_hierarchical_breakdown"] = {
                        "slices": [],
                        "max_depth": 0,
                        "top_n_per_level": 0,
                        "frame_duration_ms": 0.0,
                    }
                    json_result["_render_thread_hierarchical_breakdown_description"] = (
                        "Hierarchical breakdown of function calls in the longest render thread frame"
                    )

                # Phase 4.2: High Frequency Function Detection - Reuse existing thread and frame infrastructure
                try:
                    # Get UTID for main thread based on thread name (more robust approach)
                    main_thread_utid = None
                    if metric.target_main_thread:
                        main_thread_query = f"""
                            SELECT thread.utid
                            FROM thread
                            JOIN process ON thread.upid = process.upid
                            WHERE thread.name = '{metric.target_main_thread}'
                              AND ((process.name NOT LIKE 'com.oculus%') OR (process.name LIKE '{metric.target_process}%'))
                            LIMIT 1
                            """
                        main_qr_it = metric.trace_processor.query(main_thread_query)
                        main_df = main_qr_it.as_pandas_dataframe()
                        if not main_df.empty:
                            main_thread_utid = main_df["utid"].iloc[0]
                            MetricAPI.write_to_log(
                                f"Phase 4.2: Found main thread UTID {main_thread_utid} from name '{metric.target_main_thread}'",
                                enable_logging=metric.enable_logging,
                                log_prefix=metric.jsonName,
                            )
                        else:
                            # Fallback to frame data UTID if thread name query fails
                            main_thread_utid = top_frame_main_thread_utid
                            MetricAPI.write_to_log(
                                f"Phase 4.2: Using fallback main thread UTID {main_thread_utid} from frame data",
                                enable_logging=metric.enable_logging,
                                log_prefix=metric.jsonName,
                            )
                    else:
                        # Fallback to frame data UTID if no target main thread identified
                        main_thread_utid = top_frame_main_thread_utid
                        MetricAPI.write_to_log(
                            f"Phase 4.2: No target main thread identified, using frame data UTID {main_thread_utid}",
                            enable_logging=metric.enable_logging,
                            log_prefix=metric.jsonName,
                        )

                    # Get UTID for render thread if available
                    render_thread_utid = None
                    if metric.target_render_thread:
                        # Query to get render thread UTID
                        render_thread_query = f"""
                            SELECT thread.utid
                            FROM thread
                            JOIN process ON thread.upid = process.upid
                            WHERE thread.name = '{metric.target_render_thread}'
                              AND ((process.name NOT LIKE 'com.oculus%') OR (process.name LIKE '{metric.target_process}%'))
                            LIMIT 1
                            """
                        render_qr_it = metric.trace_processor.query(render_thread_query)
                        render_df = render_qr_it.as_pandas_dataframe()
                        if not render_df.empty:
                            render_thread_utid = render_df["utid"].iloc[0]
                            MetricAPI.write_to_log(
                                f"Phase 4.2: Found render thread UTID {render_thread_utid} from name '{metric.target_render_thread}'",
                                enable_logging=metric.enable_logging,
                                log_prefix=metric.jsonName,
                            )

                    # Prepare target threads dictionary - reuse existing thread identification infrastructure
                    target_threads = {"main": main_thread_utid}
                    if render_thread_utid is not None:
                        target_threads["render"] = render_thread_utid

                    MetricAPI.write_to_log(
                        f"Phase 4.2: Target threads configured - Main: {main_thread_utid}, Render: {render_thread_utid}",
                        enable_logging=metric.enable_logging,
                        log_prefix=metric.jsonName,
                    )

                    # Perform high frequency function analysis - reuse frame boundaries
                    high_frequency_analysis = metric.analyze_high_frequency_functions(
                        target_threads, top_start_time, top_end_time
                    )
                    json_result["high_frequency_analysis"] = high_frequency_analysis
                    json_result["_high_frequency_analysis_description"] = (
                        "Analysis of functions called frequently within a single frame that may impact performance"
                    )

                    MetricAPI.write_to_log(
                        f"Phase 4.2: High frequency analysis completed with quality {high_frequency_analysis['analysis_quality']:.2f}",
                        enable_logging=metric.enable_logging,
                        log_prefix=metric.jsonName,
                    )

                    # Log summary of high frequency functions found
                    high_freq_functions = high_frequency_analysis.get(
                        "high_frequency_functions", {}
                    )
                    for thread_type, functions in high_freq_functions.items():
                        critical_count = len(functions.get("critical", []))
                        high_count = len(functions.get("high", []))
                        warning_count = len(functions.get("warning", []))
                        total_count = critical_count + high_count + warning_count

                        if total_count > 0:
                            MetricAPI.write_to_log(
                                f"Phase 4.2: {thread_type} thread high frequency functions: {total_count} (Critical: {critical_count}, High: {high_count}, Warning: {warning_count})",
                                enable_logging=metric.enable_logging,
                                log_prefix=metric.jsonName,
                            )

                except Exception as e:
                    MetricAPI.write_to_log(
                        f"Phase 4.2: Error in high frequency function analysis: {e}",
                        enable_logging=metric.enable_logging,
                        log_prefix=metric.jsonName,
                    )
                    # Provide default high frequency analysis data
                    json_result["high_frequency_analysis"] = {
                        "high_frequency_functions": {},
                        "analysis_quality": 0.0,
                        "frame_duration_ms": 0.0,
                        "threads_analyzed": [],
                    }

            except Exception as e:
                MetricAPI.write_to_log(
                    f"Phase 4: Error in enhanced frame analysis: {e}",
                    enable_logging=metric.enable_logging,
                    log_prefix=metric.jsonName,
                )
                # Provide default high frequency analysis data
                json_result["high_frequency_analysis"] = {
                    "high_frequency_functions": {},
                    "analysis_quality": 0.0,
                    "frame_duration_ms": 0.0,
                    "threads_analyzed": [],
                }
                json_result["_high_frequency_analysis_description"] = (
                    "Analysis of functions called frequently within a single frame that may impact performance"
                )
        else:
            json_result["top_main_thread_frame_time"] = 0.0
            json_result["main_thread_top_frame_core"] = {"cpu_occupency": []}
            json_result["main_thread_top_frame_run_ratio"] = {
                "run_ratio": 0.0,
                "runable_ratio": 0.0,
                "running_time": 0.0,
                "tname": "",
            }
            json_result["main_thread_mid_frame_run_ratio"] = {
                "run_ratio": 0.0,
                "runable_ratio": 0.0,
                "running_time": 0.0,
            }
            json_result["top_frame_tracks"] = {"name": [], "ts": [], "dur": []}
            json_result["mid_frame_tracks"] = {"name": [], "ts": [], "dur": []}

            # Phase 4.2: Add default high frequency analysis data when no frames available
            json_result["high_frequency_analysis"] = {
                "high_frequency_functions": {},
                "analysis_quality": 0.0,
                "frame_duration_ms": 0.0,
                "threads_analyzed": [],
            }

            MetricAPI.write_to_log(
                "Warning: No frame time data available, using default values",
                enable_logging=metric.enable_logging,
                log_prefix=metric.jsonName,
            )

        counter_to_track = [
            "% Time Shading Fragments",
            "% Anisotropic Filtered",
            "% Vertex Fetch Stall",
            "% Texture Fetch Stall",
            "% Time ALUs Working",
            "% Shader ALU Capacity Utilized",
            "Fragment Instructions / Second",
            "Fragments Shaded / Second",
            "Textures / Fragment",
            "Vertices Shaded / Second",
            "Vertex Instructions / Second",
            "GPU % Bus Busy",
            "Avg Bytes / Vertex",
            "% Time Shading Vertices",
            "Reused Vertices / Second",
        ]

        default_gpu_metrics = {
            "entries": 0,
            "mean": 0.0,
            "std": 0.0,
            "max": 0.0,
            "quantile_75": 0.0,
        }
        # GPU-related processing - wrapped in conditional check
        if metric.has_gpu_data:
            binning_time = metric.get_binning_time()
            json_result["binning"] = binning_time

            all_counter = metric.get_counter_df()

            if (
                hasattr(metric, "main_color_pass_df")
                and not metric.get_main_color_pass_df().empty
            ):
                slice_counters = get_counter_for_slice_df(
                    all_counter,
                    metric.get_main_color_pass_df(),
                    "main_color_pass_id",
                    frame_count=100,
                )
                MetricAPI.write_to_log(
                    slice_counters,
                    enable_logging=metric.enable_logging,
                    log_prefix=metric.jsonName,
                )

                # Process each counter individually - skip if it fails
                for counter_name in counter_to_track:
                    try:
                        json_event = mean_stddev_for_counter(
                            slice_counters, counter_name, False, result_factor=1.0
                        )
                        json_result[counter_name] = json_event
                    except Exception as e:
                        MetricAPI.write_to_log(
                            f"Skipping counter '{counter_name}': {e}",
                            enable_logging=metric.enable_logging,
                            log_prefix=metric.jsonName,
                        )

                # Fragment Instructions / Fragments Shaded - skip if it fails
                try:
                    if (
                        "Fragments Shaded / Second" in json_result
                        and "Fragment Instructions / Second" in json_result
                    ):
                        result = get_instructions_per_unit(
                            json_result["Fragments Shaded / Second"],
                            json_result["Fragment Instructions / Second"],
                        )
                        if result is not None:
                            json_result["Fragment Instructions / Fragments Shaded"] = (
                                result
                            )
                except Exception as e:
                    MetricAPI.write_to_log(
                        f"Skipping 'Fragment Instructions / Fragments Shaded': {e}",
                        enable_logging=metric.enable_logging,
                        log_prefix=metric.jsonName,
                    )

                # Vertex Instructions / Vertex Shaded - skip if it fails
                try:
                    if (
                        "Vertices Shaded / Second" in json_result
                        and "Vertex Instructions / Second" in json_result
                    ):
                        result = get_instructions_per_unit(
                            json_result["Vertices Shaded / Second"],
                            json_result["Vertex Instructions / Second"],
                        )
                        if result is not None:
                            json_result["Vertex Instructions / Vertex Shaded"] = result
                except Exception as e:
                    MetricAPI.write_to_log(
                        f"Skipping 'Vertex Instructions / Vertex Shaded': {e}",
                        enable_logging=metric.enable_logging,
                        log_prefix=metric.jsonName,
                    )

                # Main Buffer Pixel Count - skip if it fails
                try:
                    result = metric.get_main_buffer_pixel_count()
                    if result is not None:
                        json_result["Main Buffer Pixel Count"] = result
                except Exception as e:
                    MetricAPI.write_to_log(
                        f"Skipping 'Main Buffer Pixel Count': {e}",
                        enable_logging=metric.enable_logging,
                        log_prefix=metric.jsonName,
                    )

                # Fragments Shaded / Pixel Second - skip if it fails
                try:
                    if (
                        "Main Buffer Pixel Count" in json_result
                        and "Fragments Shaded / Second" in json_result
                    ):
                        result = get_instructions_per_unit(
                            json_result["Main Buffer Pixel Count"],
                            json_result["Fragments Shaded / Second"],
                        )
                        if result is not None:
                            json_result["Fragments Shaded / Pixel Second"] = result
                except Exception as e:
                    MetricAPI.write_to_log(
                        f"Skipping 'Fragments Shaded / Pixel Second': {e}",
                        enable_logging=metric.enable_logging,
                        log_prefix=metric.jsonName,
                    )
            else:
                # GPU data available but main_color_pass_df is empty - skip GPU metrics
                MetricAPI.write_to_log(
                    "GPU data available but main_color_pass_df is empty, skipping GPU metrics",
                    enable_logging=metric.enable_logging,
                    log_prefix=metric.jsonName,
                )
        else:
            # No GPU data available - skip GPU metrics entirely
            MetricAPI.write_to_log(
                "No GPU data available, skipping all GPU metrics",
                enable_logging=metric.enable_logging,
                log_prefix=metric.jsonName,
            )

        run_track_diff = True
        if run_track_diff and not metric.render_thread_frame_time_df.empty:
            metric.render_thread_frame_time_df.sort_values(
                by="frame_dur", ascending=False, inplace=True
            )

            mid_frame = int(len(metric.render_thread_frame_time_df) / 2)
            middle_row = metric.render_thread_frame_time_df.iloc[mid_frame]
            MetricAPI.write_to_log(
                middle_row,
                enable_logging=metric.enable_logging,
                log_prefix=metric.jsonName,
            )

            top_3_frames = metric.render_thread_frame_time_df.head(3)
            MetricAPI.write_to_log(
                top_3_frames,
                enable_logging=metric.enable_logging,
                log_prefix=metric.jsonName,
            )

            frame_track_diff = metric.diff_frame_slices_by_ts_on_thread(
                middle_row, top_3_frames.iloc[0], middle_row["utid"]
            )

            json_result["render_thread_frame_diffs"] = print_track_info(
                frame_track_diff
            )
        else:
            json_result["render_thread_frame_diffs"] = {
                "name": [],
                "ts": [],
                "dur": [],
            }

        if not metric.render_thread_frame_time_df.empty:
            top_render_frames = metric.render_thread_frame_time_df.head(1)
            top_start_time = top_render_frames["ts"].iloc[0]
            top_end_time = (
                top_render_frames["ts"].iloc[0] + top_render_frames["frame_dur"].iloc[0]
            )
            ratio = metric.thread_running_ratio_in_ts(
                top_render_frames["utid"].iloc[0], top_start_time, top_end_time
            )

            top_render_frame_core_use = metric.content_core_occupancy_in_ts(
                top_start_time, top_end_time
            )

            ratio["tname"] = top_render_frames["tname"].iloc[0]

            json_result["render_thread_top_frame_run_ratio"] = ratio
            json_result["render_thread_top_frame_core"] = top_render_frame_core_use
        else:
            json_result["render_thread_top_frame_run_ratio"] = {
                "run_ratio": 0.0,
                "runable_ratio": 0.0,
                "running_time": 0.0,
                "tname": "",
            }
            json_result["render_thread_top_frame_core"] = {"cpu_occupency": []}

        if json_result["render_thread_frame_time"]["mean"] > 0:
            fps_from_render_thread = (
                1000.0 / json_result["render_thread_frame_time"]["mean"]
            )
        else:
            fps_from_render_thread = 0.0

        if (
            "Fragments Shaded / Pixel Second" in json_result
            and "Fragment Instructions / Fragments Shaded" in json_result
        ):
            json_result["fragment_per_pixel_per_frame"] = (
                json_result["Fragments Shaded / Pixel Second"]["mean"]
                / fps_from_render_thread
                if fps_from_render_thread > 0
                else 0.0
            )

            json_result["fragment_instructions_per_pixel_per_frame"] = (
                json_result["Fragment Instructions / Fragments Shaded"]["mean"]
                * json_result["fragment_per_pixel_per_frame"]
            )
        else:
            json_result["fragment_per_pixel_per_frame"] = 0.0
            json_result["fragment_instructions_per_pixel_per_frame"] = 0.0

        # GPU-related MSAA and NumberOfBins metrics - wrapped in conditional check
        if metric.has_gpu_data and not metric.get_main_color_pass_df().empty:
            json_result["MSAA"] = mean_stddev_for_df(
                metric.get_main_color_pass_df(), "MSAA", False, 1.0
            )

            json_result["NumberOfBins"] = mean_stddev_for_df(
                metric.get_main_color_pass_df(), "numberOfBins", False, 1.0
            )
        else:
            # Provide default values when GPU data is not available
            default_gpu_metrics = {"entries": 0, "mean": 0.0, "std": 0.0}
            json_result["MSAA"] = default_gpu_metrics
            json_result["NumberOfBins"] = default_gpu_metrics

        MetricAPI.write_to_log(
            "Top Render Thread Frame CPU thread run ratio",
            enable_logging=metric.enable_logging,
            log_prefix=metric.jsonName,
        )

        MetricAPI.write_to_log(
            json_result.get("render_thread_top_frame_run_ratio", {}),
            enable_logging=metric.enable_logging,
            log_prefix=metric.jsonName,
        )

    except Exception as e:
        traceback_string = traceback.format_exc()
        MetricAPI.write_to_error_log(e, log_prefix=metric.jsonName)
        MetricAPI.write_to_error_log(traceback_string, log_prefix=metric.jsonName)
        remove_shell_process()
        remove_file(jsonPath)
        return
    # print(json.dumps(json_result))

    try:
        # remove old json
        remove_file(jsonPath)
        with open(jsonPath, "a") as json_file:
            json_result["version"] = METRIC_JSON_VERSION
            json_result["GPUData"] = metric.has_gpu_data
            json_result["TargetProcess"] = metric.target_process
            json_result["TargetMainThread"] = metric.target_main_thread
            json_result["TargetRenderThread"] = metric.target_render_thread
            print(json.dumps(json_result), file=json_file, flush=True)
    except Exception as e:
        traceback_string = traceback.format_exc()
        MetricAPI.write_to_error_log(e, log_prefix=metric.jsonName)
        MetricAPI.write_to_error_log(traceback_string, log_prefix=metric.jsonName)

    metric.trace_processor.subprocess.kill()
    metric.trace_processor.subprocess.wait()
    metric.trace_processor.close()
    remove_shell_process()

    return json_result


def main(argv):
    """
    Main function for command-line interface.
    Parses arguments and calls process_trace.
    """
    parser = argparse.ArgumentParser(description="Process some metrics.")
    parser.add_argument("tracePath", type=str, help="Path to the trace file")
    parser.add_argument(
        "binPath",
        type=str,
        nargs="?",
        default="",
        help="Path to the shell bin (optional)",
    )
    parser.add_argument("jsonPath", type=str, help="Path to the JSON output")
    parser.add_argument("--log", action="store_true", help="Enable logging")

    args = parser.parse_args(argv)

    # Call the new process_trace function
    process_trace(
        tracePath=args.tracePath,
        binPath=args.binPath,
        jsonPath=args.jsonPath,
        enable_logging=args.log,
    )


if __name__ == "__main__":
    main(sys.argv[1:])
