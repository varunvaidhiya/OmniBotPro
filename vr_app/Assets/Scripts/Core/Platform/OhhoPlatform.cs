using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace OmniBot.VR.Core.Platform
{
    /// <summary>
    /// Loads the OhhO VR manifest from the deployed website and exposes it to the
    /// rest of the headset app. One static file — <c>{PlatformBaseUrl}/vr/manifest.json</c>
    /// — is the single source of truth for branding, auth config, and which
    /// products require a VR headset (today: Pilot / teleoperation).
    ///
    /// Mirrors the singleton style of <see cref="ConnectionManager"/>. A bundled
    /// fallback copy can be dropped in Resources/vr_manifest.json for offline use.
    /// </summary>
    public class OhhoPlatform : MonoBehaviour
    {
        public static OhhoPlatform Instance { get; private set; }

        [Tooltip("Origin of the deployed OhhO site. The manifest is fetched from {base}/vr/manifest.json.")]
        [SerializeField] private string platformBaseUrl = "https://ohho-robotics.com";

        [Tooltip("Fetch the manifest automatically on Start.")]
        [SerializeField] private bool fetchOnStart = true;

        [Tooltip("Optional bundled fallback in Resources (no extension), used if the network fetch fails.")]
        [SerializeField] private string offlineResource = "vr_manifest";

        /// <summary>The loaded manifest, or null until <see cref="OnLoaded"/> fires.</summary>
        public VrManifest Manifest { get; private set; }

        /// <summary>Products that require a VR headset, in catalog order.</summary>
        public IReadOnlyList<VrProduct> Products =>
            Manifest?.Products ?? (IReadOnlyList<VrProduct>)Array.Empty<VrProduct>();

        public event Action<VrManifest> OnLoaded;
        public event Action<string> OnError;

        public string PlatformBaseUrl => platformBaseUrl;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (fetchOnStart) Load();
        }

        /// <summary>Begin fetching the manifest (idempotent enough for a button).</summary>
        public void Load() => StartCoroutine(FetchManifest());

        private IEnumerator FetchManifest()
        {
            var url = $"{platformBaseUrl.TrimEnd('/')}/vr/manifest.json";
            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                if (TryParse(req.downloadHandler.text, out var manifest, out var parseError))
                {
                    Apply(manifest);
                    yield break;
                }
                Debug.LogWarning($"[OhhoPlatform] Manifest parse failed: {parseError}");
            }
            else
            {
                Debug.LogWarning($"[OhhoPlatform] Manifest fetch failed: {req.error} ({url})");
            }

            // Network/parse failure → fall back to a bundled copy if present.
            if (TryLoadOffline(out var offline))
            {
                Debug.Log("[OhhoPlatform] Using bundled offline manifest.");
                Apply(offline);
            }
            else
            {
                OnError?.Invoke(req.error ?? "manifest unavailable");
            }
        }

        private void Apply(VrManifest manifest)
        {
            Manifest = manifest;
            OhhoTheme.ApplyFrom(manifest.Theme); // headset branding tracks the site
            OnLoaded?.Invoke(manifest);
        }

        private bool TryLoadOffline(out VrManifest manifest)
        {
            manifest = null;
            if (string.IsNullOrEmpty(offlineResource)) return false;
            var asset = Resources.Load<TextAsset>(offlineResource);
            return asset != null && TryParse(asset.text, out manifest, out _);
        }

        private static bool TryParse(string json, out VrManifest manifest, out string error)
        {
            manifest = null;
            error = null;
            try
            {
                manifest = JsonConvert.DeserializeObject<VrManifest>(json);
                if (manifest == null) { error = "null manifest"; return false; }
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }
    }
}
