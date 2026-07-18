using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniBot.VR.Core;
using OmniBot.VR.Recording;

namespace OmniBot.VR.UI
{
    /// <summary>
    /// The teleop page of the unified OhhO screen — everything the operator needs
    /// while driving a robot, on one surface:
    ///
    ///   - Connection row (robot IP / port / Connect / Disconnect + status dot)
    ///   - Live telemetry summary (odom pose, velocity, arm joints, control mode)
    ///   - Camera feed (RawImage driven by <see cref="Video.CameraFeedController"/>)
    ///   - Dataset recording controls (Start / Stop&amp;Save / Discard, live timer)
    ///   - Control hints for the active robot profile
    ///   - Back-to-garage navigation
    ///
    /// All subscriptions ride on top of <see cref="ConnectionManager"/>'s baseline
    /// topic set; UI refresh is rate-limited to keep the Quest GPU free for
    /// rendering.
    /// </summary>
    public class TeleopHudController : MonoBehaviour
    {
        [Header("Connection")]
        [SerializeField] private TMP_InputField ipField;
        [SerializeField] private TMP_InputField portField;
        [SerializeField] private Button connectButton;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private Image connectionDot;
        [SerializeField] private TMP_Text connectionStatusText;

        [Header("Telemetry")]
        [SerializeField] private TMP_Text poseText;
        [SerializeField] private TMP_Text velocityText;
        [SerializeField] private TMP_Text armText;
        [SerializeField] private TMP_Text modeText;

        [Header("Camera")]
        [SerializeField] private Button cycleCameraButton;
        [SerializeField] private Video.CameraFeedController cameraFeed;

        [Header("Recording")]
        [SerializeField] private Button startRecordingButton;
        [SerializeField] private Button stopSaveButton;
        [SerializeField] private Button discardButton;
        [SerializeField] private TMP_Text recordingStatusText;
        [SerializeField] private Image recordingDot;

        [Header("Export (upload episodes to the robot's VR bridge)")]
        [SerializeField] private Button exportButton;
        [SerializeField] private TMP_Text exportStatusText;
        [SerializeField] private int bridgeHttpPort = 8765;

        [Header("Robot / navigation")]
        [SerializeField] private TMP_Text robotNameText;
        [SerializeField] private TMP_Text hintsText;
        [SerializeField] private Button backButton;

        // ── State ─────────────────────────────────────────────────────────────
        private App.OhhoVrApp _app;
        private Control.TeleopController _teleop;
        private OdometryMsg _latestOdom;
        private JointStateMsg _latestArm;
        private string _controlMode = "—";
        private float _refreshTimer;
        private const float UiRefreshHz = 10f;

        private static readonly Color ConnectedColor = new Color(0.18f, 0.85f, 0.45f);
        private static readonly Color DisconnectedColor = new Color(0.85f, 0.25f, 0.25f);
        private static readonly Color RecordingColor = new Color(1f, 0.2f, 0.2f);
        private static readonly Color IdleDotColor = new Color(0.45f, 0.45f, 0.5f);

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _app = FindObjectOfType<App.OhhoVrApp>();
            _teleop = FindObjectOfType<Control.TeleopController>();
        }

        private void OnEnable()
        {
            if (connectButton != null) connectButton.onClick.AddListener(OnConnectClicked);
            if (disconnectButton != null) disconnectButton.onClick.AddListener(OnDisconnectClicked);
            if (cycleCameraButton != null) cycleCameraButton.onClick.AddListener(OnCycleCamera);
            if (startRecordingButton != null) startRecordingButton.onClick.AddListener(OnStartRecording);
            if (stopSaveButton != null) stopSaveButton.onClick.AddListener(OnStopSave);
            if (discardButton != null) discardButton.onClick.AddListener(OnDiscard);
            if (exportButton != null) exportButton.onClick.AddListener(OnExportAll);
            if (backButton != null) backButton.onClick.AddListener(OnBack);

            var conn = ConnectionManager.Instance;
            if (conn != null) conn.OnConnectionStateChanged += OnConnectionState;

            SubscribeTelemetry();

            // Pre-fill the connection fields from the saved preferences.
            if (conn != null)
            {
                if (ipField != null) ipField.text = conn.CurrentIp;
                if (portField != null) portField.text = conn.CurrentPort.ToString();
            }

            RefreshRobotLabel();
            OnConnectionState(conn != null && conn.IsConnected);
            RefreshRecordingUi();
        }

        private void OnDisable()
        {
            if (connectButton != null) connectButton.onClick.RemoveListener(OnConnectClicked);
            if (disconnectButton != null) disconnectButton.onClick.RemoveListener(OnDisconnectClicked);
            if (cycleCameraButton != null) cycleCameraButton.onClick.RemoveListener(OnCycleCamera);
            if (startRecordingButton != null) startRecordingButton.onClick.RemoveListener(OnStartRecording);
            if (stopSaveButton != null) stopSaveButton.onClick.RemoveListener(OnStopSave);
            if (discardButton != null) discardButton.onClick.RemoveListener(OnDiscard);
            if (exportButton != null) exportButton.onClick.RemoveListener(OnExportAll);
            if (backButton != null) backButton.onClick.RemoveListener(OnBack);

            var conn = ConnectionManager.Instance;
            if (conn != null) conn.OnConnectionStateChanged -= OnConnectionState;
        }

        private void Update()
        {
            _refreshTimer += Time.deltaTime;
            if (_refreshTimer >= 1f / UiRefreshHz)
            {
                _refreshTimer = 0f;
                RefreshTelemetryUi();
                RefreshRecordingUi();
            }
        }

        // ── Connection ────────────────────────────────────────────────────────

        private void OnConnectClicked()
        {
            string ip = ipField != null ? ipField.text.Trim() : "192.168.1.101";
            int port = 9090;
            if (portField != null) int.TryParse(portField.text.Trim(), out port);
            if (string.IsNullOrEmpty(ip)) ip = "192.168.1.101";

            SetConnectionLabel("Connecting…");
            ConnectionManager.Instance.Connect(ip, port);
        }

        private void OnDisconnectClicked()
        {
            ConnectionManager.Instance.Disconnect();
        }

        private void OnConnectionState(bool connected)
        {
            if (connectionDot != null)
                connectionDot.color = connected ? ConnectedColor : DisconnectedColor;
            SetConnectionLabel(connected ? "Connected" : "Disconnected");
            if (connectButton != null) connectButton.interactable = !connected;
            if (disconnectButton != null) disconnectButton.interactable = connected;
        }

        private void SetConnectionLabel(string s)
        {
            if (connectionStatusText != null) connectionStatusText.text = s;
        }

        // ── Telemetry ─────────────────────────────────────────────────────────

        private void SubscribeTelemetry()
        {
            var ros = ROSBridgeClient.Instance;
            if (ros == null) return;

            ros.Subscribe(RobotConfig.TOPIC_ODOM, "nav_msgs/Odometry", json =>
            {
                try { _latestOdom = JsonConvert.DeserializeObject<OdometryMsg>(JObject.Parse(json)["msg"]?.ToString() ?? json); }
                catch { /* malformed frame — keep last good */ }
            });

            ros.Subscribe(RobotConfig.TOPIC_ARM_STATES, "sensor_msgs/JointState", json =>
            {
                try { _latestArm = JsonConvert.DeserializeObject<JointStateMsg>(JObject.Parse(json)["msg"]?.ToString() ?? json); }
                catch { }
            });

            ros.Subscribe(RobotConfig.TOPIC_CONTROL_MODE_ACTIVE, "std_msgs/String", json =>
            {
                try
                {
                    var token = JObject.Parse(json)["msg"]?["data"];
                    if (token != null) _controlMode = token.ToString();
                }
                catch { }
            });
        }

        private void RefreshTelemetryUi()
        {
            if (_latestOdom != null)
            {
                var p = _latestOdom.pose.pose.position;
                var v = _latestOdom.twist.twist;
                if (poseText != null)
                    poseText.text = $"x {p.x:F2} m   y {p.y:F2} m";
                if (velocityText != null)
                    velocityText.text = $"vx {v.linear.x:F2}   vy {v.linear.y:F2}   yaw {v.angular.z:F2}";
            }

            if (armText != null)
            {
                if (_latestArm?.position != null && _latestArm.position.Length > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    int n = Mathf.Min(_latestArm.position.Length, 6);
                    for (int i = 0; i < n; i++)
                        sb.Append(_latestArm.position[i].ToString("F2")).Append(i < n - 1 ? "  " : "");
                    armText.text = sb.ToString();
                }
                else armText.text = "no arm data";
            }

            if (modeText != null) modeText.text = _controlMode;
        }

        // ── Camera ────────────────────────────────────────────────────────────

        private void OnCycleCamera()
        {
            if (cameraFeed != null) cameraFeed.CycleCamera();
        }

        // ── Recording ─────────────────────────────────────────────────────────

        private void OnStartRecording()
        {
            EpisodeManager.Instance.StartEpisode();
            RefreshRecordingUi();
        }

        private void OnStopSave()
        {
            EpisodeManager.Instance.StopEpisode();
            RefreshRecordingUi();
        }

        private void OnDiscard()
        {
            EpisodeManager.Instance.DiscardEpisode();
            RefreshRecordingUi();
        }

        private void RefreshRecordingUi()
        {
            var em = EpisodeManager.Instance;
            bool recording = em.State == EpisodeManager.EpisodeState.Recording || em.State == EpisodeManager.EpisodeState.Paused;

            if (recordingDot != null)
                recordingDot.color = recording ? RecordingColor : IdleDotColor;
            if (recordingStatusText != null)
                recordingStatusText.text = recording
                    ? $"REC {em.CurrentEpisodeName}  {em.EpisodeDuration:F0}s"
                    : "Idle";
            if (startRecordingButton != null) startRecordingButton.interactable = !recording;
            if (stopSaveButton != null) stopSaveButton.interactable = recording;
            if (discardButton != null) discardButton.interactable = recording;
        }

        // ── Export (readme §7: upload episodes to the robot's VR bridge) ──────

        private void OnExportAll()
        {
            StartCoroutine(ExportAllEpisodes());
        }

        private System.Collections.IEnumerator ExportAllEpisodes()
        {
            string dir = System.IO.Path.Combine(Application.persistentDataPath, "recordings");
            if (!System.IO.Directory.Exists(dir))
            {
                ShowExportStatus("No recordings yet.");
                yield break;
            }

            string[] files = System.IO.Directory.GetFiles(dir, "*.jsonl");
            if (files.Length == 0)
            {
                ShowExportStatus("No recordings yet.");
                yield break;
            }

            ShowExportStatus($"Exporting {files.Length} episode(s)…");
            int succeeded = 0;
            foreach (string file in files)
            {
                yield return ExportSingleEpisode(file);
                succeeded++;
            }
            ShowExportStatus($"Exported {succeeded}/{files.Length} episodes.");
        }

        private System.Collections.IEnumerator ExportSingleEpisode(string filePath)
        {
            string robotIp = PlayerPrefs.GetString("robot_ip", "192.168.1.101");
            string endpoint = $"http://{robotIp}:{bridgeHttpPort}/upload_episode";

            byte[] fileData;
            try { fileData = System.IO.File.ReadAllBytes(filePath); }
            catch (System.Exception ex)
            {
                ShowExportStatus($"Read failed: {ex.Message}");
                yield break;
            }

            string filename = System.IO.Path.GetFileName(filePath);
            var form = new WWWForm();
            form.AddBinaryData("file", fileData, filename, "application/jsonl");

            using (var req = UnityEngine.Networking.UnityWebRequest.Post(endpoint, form))
            {
                req.timeout = 30;
                yield return req.SendWebRequest();
                if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                    ShowExportStatus($"Export failed ({filename}): {req.error}");
            }
        }

        private Coroutine _exportStatusRoutine;
        private void ShowExportStatus(string message)
        {
            if (exportStatusText == null) return;
            exportStatusText.text = message;
            exportStatusText.gameObject.SetActive(true);
            if (_exportStatusRoutine != null) StopCoroutine(_exportStatusRoutine);
            _exportStatusRoutine = StartCoroutine(HideExportStatus(4f));
        }

        private System.Collections.IEnumerator HideExportStatus(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (exportStatusText != null) exportStatusText.gameObject.SetActive(false);
        }

        // ── Robot label / hints ───────────────────────────────────────────────

        private void RefreshRobotLabel()
        {
            var profile = _teleop != null ? _teleop.Profile : null;
            if (robotNameText != null)
                robotNameText.text = profile != null ? profile.Name : "No robot selected";
            if (hintsText != null && _teleop != null)
                hintsText.text = $"{_teleop.DriveHint}\n{_teleop.ManipHint}";
        }

        // ── Navigation ────────────────────────────────────────────────────────

        private void OnBack()
        {
            if (_app != null) _app.StopTeleopAndReturnToGarage();
        }
    }
}
