using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OmniBot.VR.Control;

namespace OmniBot.VR.Video
{
    /// <summary>
    /// Profile-driven camera feed controller — the Phase 4 replacement for the
    /// legacy <c>CameraFeedViewer</c>. Instead of hardcoded topic names, it reads
    /// the camera list from <see cref="RobotProfile.Topics.Images"/> (set by
    /// <see cref="RobotProfileFactory"/> from the catalog), so a drone shows its
    /// FPV + down cameras, a mobile manipulator shows front + wrist + BEV, and an
    /// industrial arm shows just its wrist camera — all automatically.
    ///
    /// Video transport: tries <see cref="WebRtcVideoSource"/> first (sub-100 ms
    /// telepresence) when <c>preferWebRtc</c> is set, and falls back to
    /// <see cref="MjpegVideoSource"/> if WebRTC is unavailable or the signaling
    /// server isn't configured. The fallback is automatic per-camera — no UI
    /// change needed. Right thumbstick press cycles cameras.
    /// </summary>
    public class CameraFeedController : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private RawImage displayImage;
        [SerializeField] private TMP_Text cameraNameOverlay;
        [SerializeField] private Texture2D noSignalTexture;

        [Header("Transport")]
        [Tooltip("Prefer WebRTC (sub-100 ms) when com.unity.webrtc is available. Falls back to MJPEG automatically.")]
        [SerializeField] private bool preferWebRtc = true;

        // ── State ─────────────────────────────────────────────────────────────
        private RobotProfile _profile;
        private List<string> _cameras = new List<string>();
        private int _currentIndex;
        private IVideoSource _activeSource;
        private string _robotIp;

        // ── Unity lifecycle ────────────────────────────────────────────────────
        private void Start()
        {
            _robotIp = PlayerPrefs.GetString("robot_ip", "192.168.1.101");
            var conn = OmniBot.VR.Core.ConnectionManager.Instance;
            if (conn != null) conn.OnRobotIpChanged += OnRobotIpChanged;
        }

        private void OnDestroy()
        {
            var conn = OmniBot.VR.Core.ConnectionManager.Instance;
            if (conn != null) conn.OnRobotIpChanged -= OnRobotIpChanged;
            _activeSource?.Dispose();
        }

        private void Update()
        {
            if (OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick, OVRInput.Controller.RTouch))
                CycleCamera();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Configure the camera list from the selected robot's profile. Called by
        /// <see cref="OmniBot.VR.Control.TeleopController"/> (or OhhoVrApp) when
        /// teleop starts.
        /// </summary>
        public void Configure(RobotProfile profile)
        {
            _profile = profile;
            _cameras = profile?.Topics?.Images ?? new List<string>();
            _currentIndex = 0;
            StartCurrentCamera();
        }

        /// <summary>Advances to the next camera and restarts the stream.</summary>
        public void CycleCamera()
        {
            if (_cameras.Count == 0) return;
            _currentIndex = (_currentIndex + 1) % _cameras.Count;
            StartCurrentCamera();
        }

        /// <summary>Switches to a specific camera by index.</summary>
        public void SetCamera(int index)
        {
            if (_cameras.Count == 0) return;
            _currentIndex = Mathf.Clamp(index, 0, _cameras.Count - 1);
            StartCurrentCamera();
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void OnRobotIpChanged(string newIp)
        {
            _robotIp = newIp;
            StartCurrentCamera();
        }

        private void StartCurrentCamera()
        {
            _activeSource?.Dispose();
            _activeSource = null;

            if (_cameras.Count == 0)
            {
                ShowNoSignal();
                if (cameraNameOverlay != null) cameraNameOverlay.text = "No cameras";
                return;
            }

            int idx = Mathf.Clamp(_currentIndex, 0, _cameras.Count - 1);
            string topic = _cameras[idx];
            if (cameraNameOverlay != null) cameraNameOverlay.text = topic;

            // Try WebRTC first (if preferred + available)
            if (preferWebRtc && WebRtcVideoSource.IsAvailable)
            {
                var webrtc = new WebRtcVideoSource(this);
                webrtc.OnFrame += OnVideoFrame;
                webrtc.OnError += _ => FallbackToMjpeg(topic);
                webrtc.Start(_robotIp, topic);
                _activeSource = webrtc;
                Debug.Log($"[CameraFeedController] Starting WebRTC for {topic}");
                return;
            }

            // Otherwise go straight to MJPEG
            StartMjpeg(topic);
        }

        private void FallbackToMjpeg(string topic)
        {
            Debug.Log($"[CameraFeedController] Falling back to MJPEG for {topic}");
            _activeSource?.Dispose();
            _activeSource = null;
            StartMjpeg(topic);
        }

        private void StartMjpeg(string topic)
        {
            var mjpeg = new MjpegVideoSource(this);
            mjpeg.OnFrame += OnVideoFrame;
            mjpeg.OnError += err =>
            {
                Debug.LogWarning($"[CameraFeedController] MJPEG error: {err}");
                ShowNoSignal();
            };
            mjpeg.Start(_robotIp, topic);
            _activeSource = mjpeg;
        }

        private void OnVideoFrame(Texture2D frame)
        {
            if (displayImage != null)
            {
                displayImage.color = Color.white; // full brightness once streaming
                displayImage.texture = frame;
            }
        }

        private void ShowNoSignal()
        {
            if (displayImage == null) return;
            displayImage.color = new Color(0.02f, 0.03f, 0.06f, 1f); // dark idle state
            if (noSignalTexture != null)
                displayImage.texture = noSignalTexture;
            else
            {
                var grey = new Texture2D(64, 64);
                var pixels = new Color[64 * 64];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0.2f, 0.2f, 0.2f);
                grey.SetPixels(pixels);
                grey.Apply();
                displayImage.texture = grey;
            }
        }
    }
}
