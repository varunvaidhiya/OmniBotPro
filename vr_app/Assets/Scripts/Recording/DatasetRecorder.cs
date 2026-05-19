using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using OmniBot.VR.Core;

namespace OmniBot.VR.Recording
{
    /// <summary>
    /// Records VR observations and actions locally on the headset at 30 Hz.
    ///
    /// Each timestep is stored as a JSON line with the format:
    /// { "t": ..., "obs": { "arm_pos": [...], "base_vel": [...], "odom_pos": [...], "odom_heading": ... },
    ///   "action": { "arm_cmd": [...], "base_cmd": [...] } }
    ///
    /// Lines are buffered in memory and flushed to
    /// Application.persistentDataPath/recordings/{episodeName}.jsonl
    /// when the episode stops.
    /// </summary>
    public class DatasetRecorder : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────
        private const float RecordHz          = 30f;
        private const string RecordingSubDir  = "recordings";

        // ── Cached observations ───────────────────────────────────────────────
        private float[] _latestArmPos    = new float[6];
        private float[] _latestBaseVel   = new float[3]; // vx, vy, omega
        private float[] _latestOdomPos   = new float[3]; // x, y, z
        private float   _latestOdomHeading = 0f;

        // ── Cached actions ────────────────────────────────────────────────────
        private float[] _latestArmCmd  = new float[6];
        private float[] _latestBaseCmd = new float[3]; // vx, vy, omega

        // ── Recording state ───────────────────────────────────────────────────
        private List<string>  _recordBuffer   = new List<string>(4096);
        private string        _currentEpisode = "";
        private string        _currentFilePath = "";
        private int           _frameCount      = 0;
        private float         _startTime       = 0f;

        // Publish timer
        private float _recordInterval;
        private float _recordTimer = 0f;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            _recordInterval = 1f / RecordHz;
        }

        private void Start()
        {
            // Subscribe to ROSBridge topics for observations
            ROSBridgeClient.Instance.Subscribe(
                RobotConfig.TOPIC_ARM_STATES, "sensor_msgs/JointState", OnArmState);
            ROSBridgeClient.Instance.Subscribe(
                RobotConfig.TOPIC_ODOM, "nav_msgs/Odometry", OnOdom);
            // Subscribe to outgoing command topics for action recording
            // (We eavesdrop on the same topics we publish to)
            ROSBridgeClient.Instance.Subscribe(
                RobotConfig.TOPIC_CMD_VEL, "geometry_msgs/Twist", OnCmdVel);
            ROSBridgeClient.Instance.Subscribe(
                RobotConfig.TOPIC_ARM_COMMANDS, "sensor_msgs/JointState", OnArmCommands);

            // Listen to EpisodeManager events
            EpisodeManager.Instance.OnEpisodeStarted  += OnEpisodeStarted;
            EpisodeManager.Instance.OnEpisodeStopped  += OnEpisodeStopped;
            EpisodeManager.Instance.OnEpisodeDiscarded += OnEpisodeDiscarded;

            // Ensure recordings directory exists
            string dir = Path.Combine(Application.persistentDataPath, RecordingSubDir);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        private void OnDestroy()
        {
            if (EpisodeManager.Instance != null)
            {
                EpisodeManager.Instance.OnEpisodeStarted   -= OnEpisodeStarted;
                EpisodeManager.Instance.OnEpisodeStopped   -= OnEpisodeStopped;
                EpisodeManager.Instance.OnEpisodeDiscarded -= OnEpisodeDiscarded;
            }
        }

        private void Update()
        {
            if (EpisodeManager.Instance.State != EpisodeManager.EpisodeState.Recording) return;

            _recordTimer += Time.deltaTime;
            if (_recordTimer < _recordInterval) return;
            _recordTimer -= _recordInterval;

            RecordTimestep();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Returns recording statistics for the current or last episode.</summary>
        public (int frames, float durationSecs, string filePath) GetRecordingStats()
        {
            float duration = _startTime > 0f ? Time.time - _startTime : 0f;
            return (_frameCount, duration, _currentFilePath);
        }

        // ── ROSBridge callbacks ──────────────────────────────────────────────

        private void OnArmState(string json)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<JointStateMsg>(json);
                if (msg?.position == null) return;
                int count = Mathf.Min(msg.position.Length, 6);
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
                    _latestBaseVel[0] = msg.twist.twist.linear?.x  ?? 0f;
                    _latestBaseVel[1] = msg.twist.twist.linear?.y  ?? 0f;
                    _latestBaseVel[2] = msg.twist.twist.angular?.z ?? 0f;
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
                _latestBaseCmd[0] = msg.linear?.x  ?? 0f;
                _latestBaseCmd[1] = msg.linear?.y  ?? 0f;
                _latestBaseCmd[2] = msg.angular?.z ?? 0f;
            }
            catch { }
        }

        private void OnArmCommands(string json)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<JointStateMsg>(json);
                if (msg?.position == null) return;
                int count = Mathf.Min(msg.position.Length, 6);
                for (int i = 0; i < count; i++) _latestArmCmd[i] = msg.position[i];
            }
            catch { }
        }

        // ── Episode lifecycle handlers ────────────────────────────────────────

        private void OnEpisodeStarted(string episodeName)
        {
            _currentEpisode  = episodeName;
            _currentFilePath = Path.Combine(Application.persistentDataPath, RecordingSubDir, episodeName + ".jsonl");
            _recordBuffer.Clear();
            _frameCount = 0;
            _startTime  = Time.time;
            _recordTimer = 0f;
            Debug.Log($"[DatasetRecorder] Recording to: {_currentFilePath}");
        }

        private void OnEpisodeStopped(string episodeName)
        {
            FlushBufferToFile(_currentFilePath);
            Debug.Log($"[DatasetRecorder] Saved {_frameCount} frames to {_currentFilePath}");
        }

        private void OnEpisodeDiscarded(string episodeName)
        {
            _recordBuffer.Clear();
            _frameCount = 0;
            Debug.Log($"[DatasetRecorder] Episode discarded, buffer cleared ({episodeName}).");
        }

        // ── Recording helpers ─────────────────────────────────────────────────

        private void RecordTimestep()
        {
            double timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

            var record = new TimestepRecord
            {
                t = timestamp,
                obs = new ObsRecord
                {
                    arm_pos      = CopyArray(_latestArmPos, 6),
                    base_vel     = CopyArray(_latestBaseVel, 3),
                    odom_pos     = CopyArray(_latestOdomPos, 3),
                    odom_heading = _latestOdomHeading,
                },
                action = new ActionRecord
                {
                    arm_cmd  = CopyArray(_latestArmCmd,  6),
                    base_cmd = CopyArray(_latestBaseCmd, 3),
                }
            };

            string line = JsonConvert.SerializeObject(record, Formatting.None);
            _recordBuffer.Add(line);
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
                {
                    foreach (var line in _recordBuffer)
                        sw.WriteLine(line);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatasetRecorder] Failed to write {filePath}: {ex.Message}");
            }
            finally
            {
                _recordBuffer.Clear();
            }
        }

        private static float[] CopyArray(float[] src, int length)
        {
            float[] dst = new float[length];
            int count = Mathf.Min(src.Length, length);
            Array.Copy(src, dst, count);
            return dst;
        }

        // ── JSON data classes ─────────────────────────────────────────────────

        private class TimestepRecord
        {
            [JsonProperty("t")]      public double       t;
            [JsonProperty("obs")]    public ObsRecord    obs;
            [JsonProperty("action")] public ActionRecord action;
        }

        private class ObsRecord
        {
            [JsonProperty("arm_pos")]      public float[] arm_pos;
            [JsonProperty("base_vel")]     public float[] base_vel;
            [JsonProperty("odom_pos")]     public float[] odom_pos;
            [JsonProperty("odom_heading")] public float   odom_heading;
        }

        private class ActionRecord
        {
            [JsonProperty("arm_cmd")]  public float[] arm_cmd;
            [JsonProperty("base_cmd")] public float[] base_cmd;
        }
    }
}
