using UnityEngine;
using OmniBot.VR.Core.Platform;
using OmniBot.VR.Control;
using OmniBot.VR.UI;
using OmniBot.VR.UI.Garage;

namespace OmniBot.VR.App
{
    /// <summary>
    /// Top-level app flow for the OhhO VR headset — mirrors the website's own
    /// journey: <b>sign in → console (VR products) → open Pilot → garage → teleop</b>.
    ///
    /// It wires the platform services together once the manifest loads (so the
    /// Supabase auth + garage clients get the same project the website uses), then
    /// routes between panels based on auth state. A returning user with a saved
    /// session skips straight past login — the connected-experience goal: sign in
    /// once, anywhere, and your robots are already there.
    ///
    /// Drop this on a bootstrap GameObject alongside OhhoPlatform, OhhoCatalog,
    /// SupabaseAuthService and GarageClient, and assign the three panels.
    /// </summary>
    public class OhhoVrApp : MonoBehaviour
    {
        [Header("Panels (assign in the scene)")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject consolePanel;
        [SerializeField] private GameObject garagePanel;
        [Tooltip("The teleop page shown after a robot is picked (camera feed, telemetry, recording).")]
        [SerializeField] private GameObject teleopPanel;
        [SerializeField] private GameObject cameraPanel;

        [Header("Services")]
        [SerializeField] private OhhoPlatform platform;
        [SerializeField] private SupabaseAuthService auth;
        [SerializeField] private GarageClient garage;

        [Header("Teleop (Phase 2)")]
        [Tooltip("The garage panel controller — listened to for robot selection.")]
        [SerializeField] private GaragePanelController garagePanelController;
        [Tooltip("The fleet panel controller — enhanced garage with quick-resume. Optional.")]
        [SerializeField] private FleetPanelController fleetPanelController;
        [Tooltip("The teleop control layer. Receives the selected robot's profile.")]
        [SerializeField] private TeleopController teleopController;

        [Header("Phase 5 — polish")]
        [Tooltip("First-run onboarding overlay. Shows control hints on the first teleop session.")]
        [SerializeField] private OnboardingController onboarding;

        private void Awake()
        {
            if (platform == null) platform = OhhoPlatform.Instance;
            if (auth == null) auth = SupabaseAuthService.Instance;
            if (garage == null) garage = GarageClient.Instance;
        }

        private void OnEnable()
        {
            if (platform != null) platform.OnLoaded += OnManifestLoaded;
            if (auth != null) auth.OnAuthChanged += OnAuthChanged;
            if (garagePanelController != null) garagePanelController.RobotPicked += OnRobotPicked;
            if (fleetPanelController != null)
            {
                fleetPanelController.RobotPicked += OnRobotPicked;
                fleetPanelController.QuickResume += OnQuickResume;
            }
        }

        private void OnDisable()
        {
            if (platform != null) platform.OnLoaded -= OnManifestLoaded;
            if (auth != null) auth.OnAuthChanged -= OnAuthChanged;
            if (garagePanelController != null) garagePanelController.RobotPicked -= OnRobotPicked;
            if (fleetPanelController != null)
            {
                fleetPanelController.RobotPicked -= OnRobotPicked;
                fleetPanelController.QuickResume -= OnQuickResume;
            }
        }

        private void Start()
        {
            // Show login immediately; the manifest load decides where we land.
            ShowLogin();
            if (platform != null && platform.Manifest != null) OnManifestLoaded(platform.Manifest);
        }

        private void OnManifestLoaded(VrManifest manifest)
        {
            // Configure auth + garage with the same Supabase project the site uses.
            auth?.Configure(manifest.Auth);
            garage?.Configure(manifest.Auth);

            // Returning user? Restore the session and skip login.
            if (auth != null)
                auth.RestoreSession(restored => { if (restored) ShowConsole(); else ShowLogin(); });
        }

        private void OnAuthChanged(bool signedIn)
        {
            if (signedIn) ShowConsole();
            else ShowLogin();
        }

        // ── Panel routing ─────────────────────────────────────────────────────
        public void ShowLogin() => Activate(login: true, console: false, garage: false, teleop: false);
        public void ShowConsole() => Activate(login: false, console: true, garage: false, teleop: false);
        public void ShowGarage() => Activate(login: false, console: false, garage: true, teleop: false);
        public void ShowTeleop() => Activate(login: false, console: false, garage: false, teleop: true, camera: true);

        private void Activate(bool login, bool console, bool garage, bool teleop, bool camera = false)
        {
            if (loginPanel != null) loginPanel.SetActive(login);
            if (consolePanel != null) consolePanel.SetActive(console);
            if (garagePanel != null) garagePanel.SetActive(garage);
            if (teleopPanel != null) teleopPanel.SetActive(teleop);
            if (cameraPanel != null) cameraPanel.SetActive(camera);
        }

        /// <summary>Open ohho-robotics.com in the headset's system browser.</summary>
        public void OpenWebsite()
        {
            string url = platform != null ? platform.PlatformBaseUrl : "https://ohho-robotics.com";
            Application.OpenURL(url);
        }

        // ── Phase 2: robot selection → teleop ──────────────────────────────────

        /// <summary>
        /// Called when the user picks a robot in the garage. Builds the
        /// <see cref="RobotProfile"/> from the catalog entry (drive kind + arm DOF
        /// + topics + limits), hands it to the <see cref="TeleopController"/>, and
        /// routes to the teleop view. This is the connected-experience payoff:
        /// pick a robot, start driving it.
        /// </summary>
        private void OnRobotPicked(Core.Platform.GarageRobot robot)
        {
            if (robot == null) return;
            var profile = RobotProfileFactory.FromGarageRobot(robot);
            if (profile == null)
            {
                Debug.LogWarning($"[OhhoVrApp] Could not build a profile for {robot.DisplayName}.");
                return;
            }
            if (teleopController == null)
            {
                Debug.LogWarning("[OhhoVrApp] TeleopController not assigned — cannot start teleop.");
                return;
            }

            // Phase 5: apply per-robot calibration (workspace offsets, IK location, velocity caps)
            var cal = CalibrationManager.Load(profile.RobotId);
            CalibrationManager.ApplyTo(profile, cal);

            // Save as the last robot for quick-resume
            var conn = OmniBot.VR.Core.ConnectionManager.Instance;
            LastRobotStore.Save(profile.RobotId, robot.DisplayName,
                conn != null ? conn.CurrentIp : "192.168.1.101",
                conn != null ? conn.CurrentPort : 9090);

            teleopController.StartTeleop(profile);
            ShowTeleop();

            // Phase 5: show onboarding hints on the first session
            if (onboarding != null)
                onboarding.ShowIfFirstTime(teleopController.DriveHint, teleopController.ManipHint);
        }

        /// <summary>
        /// Quick-resume: jump straight into teleop with the last-driven robot.
        /// Resolves the saved robot id from the catalog, builds + calibrates the
        /// profile, and starts teleop. Falls back to the garage if the robot is
        /// no longer in the user's account.
        /// </summary>
        private void OnQuickResume()
        {
            if (!LastRobotStore.HasLastRobot)
            {
                ShowGarage();
                return;
            }

            string robotId = LastRobotStore.LastRobotId;
            var catalog = OhhoCatalog.Instance;
            if (catalog == null || !catalog.IsLoaded) { ShowGarage(); return; }

            // Find the robot in the user's garage by hardware model id
            var garageClient = GarageClient.Instance;
            if (garageClient == null) { ShowGarage(); return; }

            garageClient.GetUserRobots((robots, err) =>
            {
                if (err != null || robots == null) { ShowGarage(); return; }
                Core.Platform.UserRobot match = null;
                foreach (var r in robots)
                {
                    if (r.HardwareModelId == robotId) { match = r; break; }
                }
                if (match == null) { ShowGarage(); return; }
                var resolved = catalog.Resolve(match);
                if (resolved == null) { ShowGarage(); return; }
                OnRobotPicked(resolved);
            });
        }

        /// <summary>Stop teleoperation and return to the garage.</summary>
        public void StopTeleopAndReturnToGarage()
        {
            if (teleopController != null) teleopController.StopTeleop();
            ShowGarage();
        }
    }
}
