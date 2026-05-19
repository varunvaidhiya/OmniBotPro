using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OmniBot.VR.Core;

namespace OmniBot.VR.UI
{
    /// <summary>
    /// VR UI panel for managing the ROSBridge connection.
    ///
    /// Displays IP / port input fields, a connect/disconnect button, a colored
    /// status indicator sphere, an auto-connect toggle, and a transient error
    /// message label (auto-clears after 3 seconds).
    /// </summary>
    public class ConnectionPanel : MonoBehaviour
    {
        // ── Inspector fields ─────────────────────────────────────────────────
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField ipInputField;
        [SerializeField] private TMP_InputField portInputField;

        [Header("Buttons")]
        [SerializeField] private Button connectButton;
        [SerializeField] private Button disconnectButton;

        [Header("Status Indicator")]
        [SerializeField] private Renderer statusSphere;   // green/yellow/red material
        [SerializeField] private TMP_Text  statusLabel;

        [Header("Auto-connect")]
        [SerializeField] private Toggle autoConnectToggle;

        [Header("Error message")]
        [SerializeField] private TMP_Text errorLabel;

        // ── Colors ────────────────────────────────────────────────────────────
        private static readonly Color ColorConnected    = new Color(0.1f, 0.8f, 0.1f);
        private static readonly Color ColorConnecting   = new Color(0.9f, 0.8f, 0.1f);
        private static readonly Color ColorDisconnected = new Color(0.8f, 0.1f, 0.1f);

        // IP validation regex
        private static readonly Regex IpRegex =
            new Regex(@"^(\d{1,3}\.){3}\d{1,3}$", RegexOptions.Compiled);

        // ── State ────────────────────────────────────────────────────────────
        private bool   _connecting       = false;
        private Coroutine _errorCoroutine = null;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Start()
        {
            // Pre-fill from stored preferences
            if (ipInputField   != null) ipInputField.text   = PlayerPrefs.GetString("robot_ip",   "192.168.1.101");
            if (portInputField != null) portInputField.text = PlayerPrefs.GetInt("robot_port", 9090).ToString();
            if (autoConnectToggle != null)
                autoConnectToggle.isOn = ConnectionManager.Instance.GetAutoConnect();

            // Wire buttons
            if (connectButton    != null) connectButton.onClick.AddListener(OnConnectClicked);
            if (disconnectButton != null) disconnectButton.onClick.AddListener(OnDisconnectClicked);
            if (autoConnectToggle != null)
                autoConnectToggle.onValueChanged.AddListener(OnAutoConnectChanged);

            // Wire connection events
            ConnectionManager.Instance.OnConnectionStateChanged += OnConnectionStateChanged;
            ROSBridgeClient.Instance.OnError += OnConnectionError;

            // Set initial UI state
            RefreshUI(ROSBridgeClient.Instance.IsConnected);

            // Hide error label initially
            if (errorLabel != null) errorLabel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (ConnectionManager.Instance != null)
                ConnectionManager.Instance.OnConnectionStateChanged -= OnConnectionStateChanged;
            if (ROSBridgeClient.Instance != null)
                ROSBridgeClient.Instance.OnError -= OnConnectionError;
        }

        // ── Button handlers ──────────────────────────────────────────────────

        private void OnConnectClicked()
        {
            string ip   = ipInputField   != null ? ipInputField.text.Trim()   : "";
            string portStr = portInputField != null ? portInputField.text.Trim() : "9090";

            // Validate IP
            if (!IpRegex.IsMatch(ip))
            {
                ShowError("Invalid IP address format.");
                return;
            }

            // Validate individual octets
            string[] octets = ip.Split('.');
            foreach (var octet in octets)
            {
                if (!int.TryParse(octet, out int val) || val < 0 || val > 255)
                {
                    ShowError("IP address octets must be 0–255.");
                    return;
                }
            }

            if (!int.TryParse(portStr, out int port) || port < 1 || port > 65535)
            {
                ShowError("Port must be a number between 1 and 65535.");
                return;
            }

            _connecting = true;
            SetStatusConnecting();
            ConnectionManager.Instance.Connect(ip, port);
        }

        private void OnDisconnectClicked()
        {
            ConnectionManager.Instance.Disconnect();
        }

        private void OnAutoConnectChanged(bool value)
        {
            ConnectionManager.Instance.SetAutoConnect(value);
        }

        // ── Event handlers ───────────────────────────────────────────────────

        private void OnConnectionStateChanged(bool connected)
        {
            _connecting = false;
            RefreshUI(connected);
        }

        private void OnConnectionError(string error)
        {
            _connecting = false;
            RefreshUI(false);
            ShowError($"Connection error: {error}");
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        private void RefreshUI(bool connected)
        {
            if (connected)
            {
                SetStatusColor(ColorConnected);
                if (statusLabel   != null) statusLabel.text = "Connected";
                if (connectButton != null) connectButton.interactable    = false;
                if (disconnectButton != null) disconnectButton.interactable = true;
            }
            else
            {
                SetStatusColor(ColorDisconnected);
                if (statusLabel   != null) statusLabel.text = "Disconnected";
                if (connectButton != null) connectButton.interactable    = true;
                if (disconnectButton != null) disconnectButton.interactable = false;
            }
        }

        private void SetStatusConnecting()
        {
            SetStatusColor(ColorConnecting);
            if (statusLabel != null) statusLabel.text = "Connecting...";
            if (connectButton != null) connectButton.interactable = false;
        }

        private void SetStatusColor(Color color)
        {
            if (statusSphere != null)
            {
                statusSphere.material.color = color;
            }
        }

        private void ShowError(string message)
        {
            Debug.LogWarning($"[ConnectionPanel] {message}");
            if (errorLabel == null) return;

            errorLabel.text = message;
            errorLabel.gameObject.SetActive(true);

            if (_errorCoroutine != null) StopCoroutine(_errorCoroutine);
            _errorCoroutine = StartCoroutine(HideErrorAfterDelay(3f));
        }

        private IEnumerator HideErrorAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (errorLabel != null)
                errorLabel.gameObject.SetActive(false);
        }
    }
}
