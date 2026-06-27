using UnityEngine;
using OmniBot.VR.Core.Platform;

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

        [Header("Services")]
        [SerializeField] private OhhoPlatform platform;
        [SerializeField] private SupabaseAuthService auth;
        [SerializeField] private GarageClient garage;

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
        }

        private void OnDisable()
        {
            if (platform != null) platform.OnLoaded -= OnManifestLoaded;
            if (auth != null) auth.OnAuthChanged -= OnAuthChanged;
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
        public void ShowLogin() => Activate(login: true, console: false, garage: false);
        public void ShowConsole() => Activate(login: false, console: true, garage: false);
        public void ShowGarage() => Activate(login: false, console: false, garage: true);

        private void Activate(bool login, bool console, bool garage)
        {
            if (loginPanel != null) loginPanel.SetActive(login);
            if (consolePanel != null) consolePanel.SetActive(console);
            if (garagePanel != null) garagePanel.SetActive(garage);
        }
    }
}
