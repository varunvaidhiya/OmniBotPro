using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using OmniBot.VR.Core;
using OmniBot.VR.Control;

namespace OmniBot.VR.Recording
{
    /// <summary>
    /// Profile-driven dataset recorder — the Phase 4 replacement for the legacy
    /// <see cref="DatasetRecorder"/>. Instead of hardcoded OmniBot topics
    /// (<c>RobotConfig.TOPIC_*</c>) and fixed 6-DOF arm / 3-DOF base arrays, it
    /// reads the ROS topics and state/action dimensions from the
    /// <see cref="RobotProfile"/> so it records correctly for <i>any</i> robot:
    /// a drone records base-only obs/actions, a humanoid records dual-arm + base,
    /// an industrial arm records arm-only with no base velocity.
    ///
    /// Called by <see cref="TeleopController"/> when teleop starts
    /// (<see cref="Configure"/>), and listens to <see cref="EpisodeManager"/>
    /// events for start/stop/discard. Each timestep is a JSON line with:
    /// <code>
    /// { "t": ..., "robot": "...",
    ///   "obs":   { "arm_pos": [...], "base_vel": [...], "odom": {...} },
    ///   "action":{ "arm_cmd": [...], "base_cmd": [...] } }
    /// </code>
    /// </summary>
    public class ProfileDrivenRecorder : MonoBehaviour
    {
        private const float RecordHz = 30f;
        private const string RecordingSubDir = "recordings";

        // ── Profile (set when teleop starts) ────────────────────────────────────
        private RobotProfile _profile;
        private int _armDof;
        private int _baseDof;

        // ── Cached observations ─────────────────────────────────────────────────
        private float[] _latestArmPos;
        private float[] _latestBaseVel;
        private float[] _latestOdomPos;
        private float _latestOdomHeading;

        // ── Cached actions ──────────────────────────────────────────────────────
        private float[] _latestArmCmd;
        private float[] _latestBaseCmd;

        // ── Recording state ─────────────────────────────────────────────────────
        private readonly List<string> _recordBuffer = new List<string>(4096);
        private string _currentEpisode = "";
        private string _currentFilePath = "";
        private int _frameCount;
        private float _startTime;

        private float _recordInterval;
        private float _recordTimer;

        // ── Unity lifecycle ──────────────────────────────────────────────────────
        private void Awake()
        {
            _recordInterval = 1f / RecordHz;
        }

        private void OnDestroy()
        {
            if (EpisodeManager.Instance != null)
            {
                EpisodeManager.Instance.OnEpisodeStarted -= OnEpisodeStarted;
                EpisodeManager.Instance.OnEpisodeStopped -= OnEpisodeStopped;
                EpisodeManager.Instance.OnEpisodeDiscarded -= OnEpisodeDiscarded;
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Configure the recorder for the selected robot's profile. Subscribes to
        /// the profile's telemetry topics + eavesdrops on the command topics.
        /// Called by <see cref="TeleopController.StartTeleop"/>.
        /// </summary>
        public void Configure(RobotProfile profile)
        {
            _profile = profile;
            _armDof = profile?.ArmDof ?? 0;
            _baseDof = profile?.BaseDof ?? 0;

            _latestArmPos = new float[_armDof];
            _latestBaseVel = new float[_baseDof];
            _latestOdomPos = new float[3];
            _latestArmCmd = new float[_armDof];
            _latestBaseCmd = new float[_baseDof];

            var link = RosBridgeLink.Instance;
            if (link == null || !link.IsConnected) return;

            // Subscribe to telemetry topics from the profile
            if (!string.IsNullOrEmpty(profile.Topics.JointStates))
                link.Subscribe(profile.Topics.JointStates, "sensor_msgs/JointState", OnArmState);
            if (!string.IsNullOrEmpty(profile.Topics.Odom))
                link.Subscribe(profile.Topics.Odom, "nav_msgs/Odometry", OnOdom);

            // Eavesdrop on the command topics we publish
            if (!string.IsNullOrEmpty(profile.Topics.CmdVel))
                link.Subscribe(profile.Topics.CmdVel, "geometry_msgs/Twist", OnCmdVel);
            if (!string.IsNullOrEmpty(profile.Topics.ArmCommands))
                link.Subscribe(profile.Topics.ArmCommands, "sensor_msgs/JointState", OnArmCommands);

            // Wire episode events
            EpisodeManager.Instance.OnEpisodeStarted += OnEpisodeStarted;
            EpisodeManager.Instance.OnEpisodeStopped += OnEpisodeStopped;
            EpisodeManager.Instance.OnEpisodeDiscarded += OnEpisodeDiscarded;

            // Ensure recordings directory exists
            string dir = Path.Combine(Application.persistentDataPath, RecordingSubDir);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            Debug.Log($"[ProfileDrivenRecorder] Configured for {profile.Name} — arm {_armDof}-DOF, base {_baseDof}-DOF");
        }

        /// <summary>Returns recording statistics for the current or last episode.</summary>
        public (int frames, float durationSecs, string filePath) GetRecordingStats()
        {
            float duration = _startTime > 0f ? Time.time - _startTime : 0f;
            return (_frameCount, duration, _currentFilePath);
        }

        private void Update()
        {
            if (EpisodeManager.Instance.State != EpisodeManager.EpisodeState.Recording) return;
            _recordTimer += Time.deltaTime;
            if (_recordTimer < _recordInterval) return;
            _recordTimer -= _recordInterval;
            RecordTimestep();
        }

        // ── ROSBridge callbacks ──────────────────────────────────────────────────

        private void OnArmState(string json)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<JointStateMsg>(json);
                if (msg?.position == null) return;
                int count = Mathf.Min(msg.position.Length, _armDof);
                for (int i = 0; i < count; i++) _latestArmPos[i] = msg.position[i];
            }
            catch { }
        }

        private void OnOdom(string json)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<OdometryMsg>(json);
                if (msg?.pose?.pose == null) return;
                _latestOdomPos[0] = msg.pose.pose.position?.x ?? 0f;
                _latestOdomPos[1] = msg.pose.pose.position?.y ?? 0f;
                _latestOdomPos[2] = msg.pose.pose.position?.z ?? 0f;

                float qx = msg.pose.pose.orientation?.x ?? 0f;
                float qy = msg.pose.pose.orientation?.y ?? 0f;
                float qz = msg.pose.pose.orientation?.z ?? 0f;
                float qw = msg.pose.pose.orientation?.w ?? 1f;
                _latestOdomHeading = Mathf.Atan2(2f * (qw * qz + qx * qy),
                                                  1f - 2f * (qy * qy + qz * qz));

                if (msg.twist?.twist != null)
                {
                    _latestBaseVel[0] = msg.twist.twist.linear?.x ?? 0f;
                    if (_baseDof > 1) _latestBaseVel[1] = msg.twist.twist.linear?.y ?? 0f;
                    if (_baseDof > 2) _latestBaseVel[2] = msg.twist.twist.angular?.z ?? 0f;
                }
            }
            catch { }
        }

        private void OnCmdVel(string json)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<TwistMsg>(json);
                if (msg == null) return;
                _latestBaseCmd[0] = msg.linear?.x ?? 0f;
                if (_baseDof > 1) _latestBaseCmd[1] = msg.linear?.y ?? 0f;
                if (_baseDof > 2) _latestBaseCmd[2] = msg.angular?.z ?? 0f;
            }
            catch { }
        }

        private void OnArmCommands(string json)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<JointStateMsg>(json);
                if (msg?.position == null) return;
                int count = Mathf.Min(msg.position.Length, _armDof);
                for (int i = 0; i < count; i++) _latestArmCmd[i] = msg.position[i];
            }
            catch { }
        }

        // ── Episode lifecycle ────────────────────────────────────────────────────

        private void OnEpisodeStarted(string episodeName)
        {
            _currentEpisode = episodeName;
            _currentFilePath = Path.Combine(Application.persistentDataPath, RecordingSubDir, episodeName + ".jsonl");
            _recordBuffer.Clear();
            _frameCount = 0;
            _startTime = Time.time;
            _recordTimer = 0f;
            Debug.Log($"[ProfileDrivenRecorder] Recording to: {_currentFilePath}");
        }

        private void OnEpisodeStopped(string episodeName)
        {
            FlushBufferToFile(_currentFilePath);
            Debug.Log($"[ProfileDrivenRecorder] Saved {_frameCount} frames to {_currentFilePath}");
        }

        private void OnEpisodeDiscarded(string episodeName)
        {
            _recordBuffer.Clear();
            _frameCount = 0;
            Debug.Log($"[ProfileDrivenRecorder] Episode discarded ({episodeName}).");
        }

        // ── Recording ────────────────────────────────────────────────────────────

        private void RecordTimestep()
        {
            double timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

            var record = new TimestepRecord
            {
                t = timestamp,
                robot = _profile?.RobotId ?? "unknown",
                obs = new ObsRecord
                {
                    arm_pos = CopyArray(_latestArmPos, _armDof),
                    base_vel = CopyArray(_latestBaseVel, _baseDof),
                    odom_pos = CopyArray(_latestOdomPos, 3),
                    odom_heading = _latestOdomHeading,
                },
                action = new ActionRecord
                {
                    arm_cmd = CopyArray(_latestArmCmd, _armDof),
                    base_cmd = CopyArray(_latestBaseCmd, _baseDof),
                }
            };

            _recordBuffer.Add(JsonConvert.SerializeObject(record, Formatting.None));
            _frameCount++;
        }

        private void FlushBufferToFile(string filePath)
        {
            if (_recordBuffer.Count == 0) return;
            try
            {
                string dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                using (var sw = new StreamWriter(filePath, append: false, encoding: Encoding.UTF8))
                    foreach (var line in _recordBuffer) sw.WriteLine(line);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileDrivenRecorder] Failed to write {filePath}: {ex.Message}");
            }
            finally
            {
                _recordBuffer.Clear();
            }
        }

        private static float[] CopyArray(float[] src, int length)
        {
            if (length == 0) return new float[0];
            float[] dst = new float[length];
            int count = Mathf.Min(src.Length, length);
            Array.Copy(src, dst, count);
            return dst;
        }

        // ── JSON data classes ─────────────────────────────────────────────────────

        private class TimestepRecord
        {
            [JsonProperty("t")] public double t;
            [JsonProperty("robot")] public string robot;
            [JsonProperty("obs")] public ObsRecord obs;
            [JsonProperty("action")] public ActionRecord action;
        }

        private class ObsRecord
        {
            [JsonProperty("arm_pos")] public float[] arm_pos;
            [JsonProperty("base_vel")] public float[] base_vel;
            [JsonProperty("odom_pos")] public float[] odom_pos;
            [JsonProperty("odom_heading")] public float odom_heading;
        }

        private class ActionRecord
        {
            [JsonProperty("arm_cmd")] public float[] arm_cmd;
            [JsonProperty("base_cmd")] public float[] base_cmd;
        }
    }
}
