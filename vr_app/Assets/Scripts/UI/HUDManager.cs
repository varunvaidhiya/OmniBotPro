using UnityEngine;
using System.Collections.Generic;

namespace OmniBot.VR.UI
{
    /// <summary>
    /// Manages the set of VR floating HUD panels on a world-space Canvas.
    ///
    /// The Canvas follows the user's head lazily: it smoothly lerps toward the
    /// camera's forward direction with a configurable lag time (default 0.3 s).
    ///
    /// Left controller B button   → toggle Main panel.
    /// Right controller A button  → cycle through all panels.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        // ── Panel type enumeration ───────────────────────────────────────────
        public enum PanelType
        {
            Main,
            Telemetry,
            Camera,
            Recording,
            Connection,
            AICommand
        }

        // ── Inspector fields ─────────────────────────────────────────────────
        [Header("Canvas Settings")]
        [SerializeField] private Canvas  hudCanvas;
        [SerializeField] private float   hudDistance      = 1.5f;   // metres ahead
        [SerializeField] private float   hudFollowLag     = 0.3f;   // seconds
        [SerializeField] private Vector3 hudOffset        = new Vector3(0f, -0.1f, 0f);

        [Header("Panel GameObjects (assigned in Inspector)")]
        [SerializeField] private GameObject panelMain;
        [SerializeField] private GameObject panelTelemetry;
        [SerializeField] private GameObject panelCamera;
        [SerializeField] private GameObject panelRecording;
        [SerializeField] private GameObject panelConnection;
        [SerializeField] private GameObject panelAICommand;

        // ── State ────────────────────────────────────────────────────────────
        private Dictionary<PanelType, GameObject> _panels;
        private PanelType _activeCyclePanel = PanelType.Main;

        // Head-follow state
        private Vector3    _targetPosition;
        private Quaternion _targetRotation;
        private float      _followVelocity = 0f;

        // Button debounce
        private bool _prevBButtonPressed = false;
        private bool _prevAButtonPressed = false;

        // Panel cycle order
        private static readonly PanelType[] CycleOrder =
        {
            PanelType.Main,
            PanelType.Telemetry,
            PanelType.Camera,
            PanelType.Recording,
            PanelType.Connection,
            PanelType.AICommand
        };
        private int _cycleIndex = 0;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            _panels = new Dictionary<PanelType, GameObject>
            {
                { PanelType.Main,        panelMain       },
                { PanelType.Telemetry,   panelTelemetry  },
                { PanelType.Camera,      panelCamera     },
                { PanelType.Recording,   panelRecording  },
                { PanelType.Connection,  panelConnection },
                { PanelType.AICommand,   panelAICommand  },
            };

            // Ensure canvas is world-space
            if (hudCanvas != null)
                hudCanvas.renderMode = RenderMode.WorldSpace;
        }

        private void Start()
        {
            // Start with only the main panel visible
            foreach (var kvp in _panels)
            {
                if (kvp.Value != null)
                    kvp.Value.SetActive(kvp.Key == PanelType.Main);
            }

            // Position HUD in front of the player initially
            SnapHudToCamera();
        }

        private void Update()
        {
            HandleButtonInput();
            UpdateHudPosition();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Makes the specified panel visible.</summary>
        public void ShowPanel(PanelType panel)
        {
            if (_panels.TryGetValue(panel, out var go) && go != null)
                go.SetActive(true);
        }

        /// <summary>Hides the specified panel.</summary>
        public void HidePanel(PanelType panel)
        {
            if (_panels.TryGetValue(panel, out var go) && go != null)
                go.SetActive(false);
        }

        /// <summary>Toggles the visibility of the specified panel.</summary>
        public void TogglePanel(PanelType panel)
        {
            if (_panels.TryGetValue(panel, out var go) && go != null)
                go.SetActive(!go.activeSelf);
        }

        /// <summary>Returns true if the specified panel is currently visible.</summary>
        public bool IsPanelVisible(PanelType panel)
        {
            if (_panels.TryGetValue(panel, out var go) && go != null)
                return go.activeSelf;
            return false;
        }

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>Reads OVR controller buttons and handles panel toggling / cycling.</summary>
        private void HandleButtonInput()
        {
            // Left controller B button → toggle Main panel
            bool bButton = OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.LTouch);
            if (bButton && !_prevBButtonPressed)
                TogglePanel(PanelType.Main);
            _prevBButtonPressed = bButton;

            // Right controller A button → cycle through all panels
            bool aButton = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch);
            if (aButton && !_prevAButtonPressed)
                CycleToNextPanel();
            _prevAButtonPressed = aButton;
        }

        /// <summary>Advances to the next panel in the cycle order, hiding others.</summary>
        private void CycleToNextPanel()
        {
            // Hide current cycle panel
            HidePanel(CycleOrder[_cycleIndex]);

            // Advance index
            _cycleIndex = (_cycleIndex + 1) % CycleOrder.Length;

            // Show next panel
            ShowPanel(CycleOrder[_cycleIndex]);
        }

        /// <summary>
        /// Lazily follows the camera's forward direction.
        /// The HUD smoothly lerps toward the point (cameraPos + cameraForward * hudDistance).
        /// </summary>
        private void UpdateHudPosition()
        {
            if (hudCanvas == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            // Compute desired position: in front of camera, at hudDistance
            Vector3 flatForward = new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z).normalized;
            if (flatForward.sqrMagnitude < 0.01f)
                flatForward = Vector3.forward;

            Vector3    desiredPos = cam.transform.position + flatForward * hudDistance + hudOffset;
            Quaternion desiredRot = Quaternion.LookRotation(flatForward);

            // Smooth lerp (lag = hudFollowLag seconds → approx speed = 1/lag per second)
            float lerpSpeed = hudFollowLag > 0f ? (1f / hudFollowLag) : 10f;
            float t = 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime);

            hudCanvas.transform.position = Vector3.Lerp(hudCanvas.transform.position, desiredPos, t);
            hudCanvas.transform.rotation = Quaternion.Slerp(hudCanvas.transform.rotation, desiredRot, t);
        }

        /// <summary>Immediately snaps the HUD to camera's forward direction.</summary>
        private void SnapHudToCamera()
        {
            if (hudCanvas == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 flatForward = new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z).normalized;
            if (flatForward.sqrMagnitude < 0.01f) flatForward = Vector3.forward;

            hudCanvas.transform.position = cam.transform.position + flatForward * hudDistance + hudOffset;
            hudCanvas.transform.rotation = Quaternion.LookRotation(flatForward);
        }
    }
}
