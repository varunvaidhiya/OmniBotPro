using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using OmniBot.VR.Core;

namespace OmniBot.VR.UI
{
    /// <summary>
    /// Displays live robot telemetry in a VR panel:
    ///   - Odometry (position, heading, velocity)
    ///   - IMU (roll, pitch, yaw, linear acceleration)
    ///   - Arm joint angles (bar-chart via UI Sliders)
    ///   - Status strings (control mode, mission, AI)
    ///
    /// Subscriptions are added on top of ConnectionManager's baseline subscriptions,
    /// so each panel receives its own callbacks. UI refresh is rate-limited to 10 Hz.
    /// </summary>
    public class TelemetryPanel : MonoBehaviour
    {
        // ── Inspector — Odometry ──────────────────────────────────────────────
        [Header("Odometry")]
        [SerializeField] private TMP_Text odomPosX;
        [SerializeField] private TMP_Text odomPosY;
        [SerializeField] private TMP_Text odomHeading;
        [SerializeField] private TMP_Text odomLinVel;
        [SerializeField] private TMP_Text odomAngVel;

        // ── Inspector — IMU ───────────────────────────────────────────────────
        [Header("IMU")]
        [SerializeField] private TMP_Text imuRoll;
        [SerializeField] private TMP_Text imuPitch;
        [SerializeField] private TMP_Text imuYaw;
        [SerializeField] private TMP_Text imuAccelMag;

        // ── Inspector — Arm ───────────────────────────────────────────────────
        [Header("Arm Joints (6 Sliders)")]
        [SerializeField] private Slider[] armJointSliders; // length 6
        [SerializeField] private TMP_Text[] armJointLabels; // length 6
        [SerializeField] private TMP_Text armModeLabel;

        // ── Inspector — Status ────────────────────────────────────────────────
        [Header("Status")]
        [SerializeField] private TMP_Text controlModeLabel;
        [SerializeField] private TMP_Text missionStatusLabel;
        [SerializeField] private TMP_Text aiStatusLabel;
        [SerializeField] private TMP_Text aiResponseNeededLabel;
        [SerializeField] private GameObject aiResponseNeededPanel;

        // ── Cached data ───────────────────────────────────────────────────────
        private OdometryMsg  _latestOdom;
        private ImuMsg       _latestImu;
        private JointStateMsg _latestArmState;
        private string _controlMode  = "—";
        private string _armMode      = "—";
        private string _missionStatus = "—";
        private string _aiStatus     = "—";
        private string _aiResponseNeeded = "";

        // ── Display refresh ───────────────────────────────────────────────────
        private const float RefreshHz = 10f;
        private float _refreshInterval;
        private float _refreshTimer = 0f;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            _refreshInterval = 1f / RefreshHz;
        }

        private void Start()
        {
            // Subscribe to telemetry topics
            ROSBridgeClient.Instance.Subscribe(
                RobotConfig.TOPIC_ODOM, "nav_msgs/Odometry", OnOdom);
            ROSBridgeClient.Instance.Subscribe(
                RobotConfig.TOPIC_IMU, "sensor_msgs/Imu", OnImu);
            ROSBridgeClient.Instance.Subscribe(
                RobotConfig.TOPIC_ARM_STATES, "sensor_msgs/JointState", OnArmState);
            ROSBridgeClient.Instance.Subscribe(
                RobotConfig.TOPIC_CONTROL_MODE_ACTIVE, "std_msgs/String", OnControlMode);
            ROSBridgeClient.Instance.Subscribe(
                RobotConfig.TOPIC_ARM_MODE_ACTIVE, "std_msgs/String", OnArmMode);
            ROSBridgeClient.Instance.Subscribe(
                RobotConfig.TOPIC_MISSION_STATUS, "std_msgs/String", OnMissionStatus);
            ROSBridgeClient.Instance.Subscribe(
                RobotConfig.TOPIC_AI_STATUS, "std_msgs/String", OnAiStatus);
            ROSBridgeClient.Instance.Subscribe(
                RobotConfig.TOPIC_AI_RESPONSE_NEEDED, "std_msgs/String", OnAiResponseNeeded);

            // Configure arm sliders to joint ranges
            if (armJointSliders != null)
            {
                for (int i = 0; i < Mathf.Min(armJointSliders.Length, 6); i++)
                {
                    if (armJointSliders[i] == null) continue;
                    armJointSliders[i].minValue = RobotConfig.ARM_JOINT_MIN[i] * Mathf.Rad2Deg;
                    armJointSliders[i].maxValue = RobotConfig.ARM_JOINT_MAX[i] * Mathf.Rad2Deg;
                    armJointSliders[i].value    = 0f;
                    armJointSliders[i].interactable = false; // read-only
                }
            }

            // Hide AI response panel initially
            if (aiResponseNeededPanel != null)
                aiResponseNeededPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            // Unsubscribe to avoid lingering callbacks
            if (ROSBridgeClient.Instance != null)
            {
                ROSBridgeClient.Instance.Unsubscribe(RobotConfig.TOPIC_ODOM);
                ROSBridgeClient.Instance.Unsubscribe(RobotConfig.TOPIC_IMU);
                ROSBridgeClient.Instance.Unsubscribe(RobotConfig.TOPIC_ARM_STATES);
                ROSBridgeClient.Instance.Unsubscribe(RobotConfig.TOPIC_CONTROL_MODE_ACTIVE);
                ROSBridgeClient.Instance.Unsubscribe(RobotConfig.TOPIC_ARM_MODE_ACTIVE);
                ROSBridgeClient.Instance.Unsubscribe(RobotConfig.TOPIC_MISSION_STATUS);
                ROSBridgeClient.Instance.Unsubscribe(RobotConfig.TOPIC_AI_STATUS);
                ROSBridgeClient.Instance.Unsubscribe(RobotConfig.TOPIC_AI_RESPONSE_NEEDED);
            }
        }

        private void Update()
        {
            _refreshTimer += Time.deltaTime;
            if (_refreshTimer < _refreshInterval) return;
            _refreshTimer -= _refreshInterval;
            RefreshAllUI();
        }

        // ── ROSBridge callbacks (called on main thread via ROSBridgeClient) ──

        private void OnOdom(string json)
        {
            try { _latestOdom = JsonConvert.DeserializeObject<OdometryMsg>(json); }
            catch { /* ignore malformed */ }
        }

        private void OnImu(string json)
        {
            try { _latestImu = JsonConvert.DeserializeObject<ImuMsg>(json); }
            catch { }
        }

        private void OnArmState(string json)
        {
            try { _latestArmState = JsonConvert.DeserializeObject<JointStateMsg>(json); }
            catch { }
        }

        private void OnControlMode(string json)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<StringMsg>(json);
                if (msg != null) _controlMode = msg.data ?? "—";
            }
            catch { }
        }

        private void OnArmMode(string json)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<StringMsg>(json);
                if (msg != null) _armMode = msg.data ?? "—";
            }
            catch { }
        }

        private void OnMissionStatus(string json)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<StringMsg>(json);
                if (msg != null) _missionStatus = msg.data ?? "—";
            }
            catch { }
        }

        private void OnAiStatus(string json)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<StringMsg>(json);
                if (msg != null) _aiStatus = msg.data ?? "—";
            }
            catch { }
        }

        private void OnAiResponseNeeded(string json)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<StringMsg>(json);
                if (msg != null) _aiResponseNeeded = msg.data ?? "";
            }
            catch { }
        }

        // ── UI refresh ────────────────────────────────────────────────────────

        private void RefreshAllUI()
        {
            RefreshOdom();
            RefreshImu();
            RefreshArm();
            RefreshStatus();
        }

        private void RefreshOdom()
        {
            if (_latestOdom == null) return;

            float px = _latestOdom.pose?.pose?.position?.x ?? 0f;
            float py = _latestOdom.pose?.pose?.position?.y ?? 0f;

            // Heading from quaternion (yaw about Z in ROS frame)
            float qx = _latestOdom.pose?.pose?.orientation?.x ?? 0f;
            float qy = _latestOdom.pose?.pose?.orientation?.y ?? 0f;
            float qz = _latestOdom.pose?.pose?.orientation?.z ?? 0f;
            float qw = _latestOdom.pose?.pose?.orientation?.w ?? 1f;
            float yawRad = Mathf.Atan2(2f * (qw * qz + qx * qy), 1f - 2f * (qy * qy + qz * qz));
            float yawDeg = yawRad * Mathf.Rad2Deg;

            float linVel = _latestOdom.twist?.twist?.linear?.x ?? 0f;
            float angVel = _latestOdom.twist?.twist?.angular?.z ?? 0f;

            SetText(odomPosX,    $"X: {px:F2} m");
            SetText(odomPosY,    $"Y: {py:F2} m");
            SetText(odomHeading, $"Hdg: {yawDeg:F1}°");
            SetText(odomLinVel,  $"Lin: {linVel:F3} m/s");
            SetText(odomAngVel,  $"Ang: {angVel:F3} rad/s");
        }

        private void RefreshImu()
        {
            if (_latestImu == null) return;

            float qx = _latestImu.orientation?.x ?? 0f;
            float qy = _latestImu.orientation?.y ?? 0f;
            float qz = _latestImu.orientation?.z ?? 0f;
            float qw = _latestImu.orientation?.w ?? 1f;

            // ROS convention: roll=rotation about X, pitch=rotation about Y, yaw=rotation about Z
            float sinrCosp = 2f * (qw * qx + qy * qz);
            float cosrCosp = 1f - 2f * (qx * qx + qy * qy);
            float roll     = Mathf.Atan2(sinrCosp, cosrCosp) * Mathf.Rad2Deg;

            float sinp = 2f * (qw * qy - qz * qx);
            float pitch = (Mathf.Abs(sinp) >= 1f)
                ? Mathf.Sign(sinp) * 90f
                : Mathf.Asin(sinp) * Mathf.Rad2Deg;

            float sinyCosp = 2f * (qw * qz + qx * qy);
            float cosyCosp = 1f - 2f * (qy * qy + qz * qz);
            float yaw      = Mathf.Atan2(sinyCosp, cosyCosp) * Mathf.Rad2Deg;

            float ax = _latestImu.linear_acceleration?.x ?? 0f;
            float ay = _latestImu.linear_acceleration?.y ?? 0f;
            float az = _latestImu.linear_acceleration?.z ?? 0f;
            float accelMag = Mathf.Sqrt(ax * ax + ay * ay + az * az);

            SetText(imuRoll,      $"Roll:  {roll:F1}°");
            SetText(imuPitch,     $"Pitch: {pitch:F1}°");
            SetText(imuYaw,       $"Yaw:   {yaw:F1}°");
            SetText(imuAccelMag,  $"|a|:   {accelMag:F2} m/s²");
        }

        private void RefreshArm()
        {
            if (armModeLabel != null) armModeLabel.text = $"Arm Mode: {_armMode}";

            if (_latestArmState == null) return;

            int count = Mathf.Min(
                _latestArmState.position?.Length ?? 0,
                armJointSliders?.Length ?? 0,
                6);

            for (int i = 0; i < count; i++)
            {
                float angleDeg = (_latestArmState.position[i]) * Mathf.Rad2Deg;

                if (armJointSliders != null && i < armJointSliders.Length && armJointSliders[i] != null)
                    armJointSliders[i].value = angleDeg;

                if (armJointLabels != null && i < armJointLabels.Length && armJointLabels[i] != null)
                    armJointLabels[i].text = $"{RobotConfig.ARM_JOINT_NAMES[i]}: {angleDeg:F1}°";
            }
        }

        private void RefreshStatus()
        {
            SetText(controlModeLabel,  $"Mode: {_controlMode}");
            SetText(missionStatusLabel, $"Mission: {_missionStatus}");
            SetText(aiStatusLabel,      $"AI: {_aiStatus}");

            bool hasResponse = !string.IsNullOrEmpty(_aiResponseNeeded);
            if (aiResponseNeededPanel != null)
                aiResponseNeededPanel.SetActive(hasResponse);
            if (hasResponse)
                SetText(aiResponseNeededLabel, $"AI asks: {_aiResponseNeeded}");
        }

        // ── Utility ───────────────────────────────────────────────────────────

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null) label.text = value;
        }
    }
}
