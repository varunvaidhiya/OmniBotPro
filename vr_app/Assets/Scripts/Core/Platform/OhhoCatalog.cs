using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace OmniBot.VR.Core.Platform
{
    /// <summary>
    /// Lazily loads the OhhO robot catalog (<c>{site}/vr/catalog.json</c>) — the
    /// same catalog the website's garage uses — and resolves a user's saved
    /// robots to displayable models. Fetched on demand (when the user opens the
    /// teleop garage), not at launch, because it is larger than the manifest.
    /// </summary>
    public class OhhoCatalog : MonoBehaviour
    {
        public static OhhoCatalog Instance { get; private set; }

        [Tooltip("Origin of the deployed OhhO site. Defaults to the platform's base URL when present.")]
        [SerializeField] private string platformBaseUrl = "https://ohho-robotics.com";

        [Tooltip("Optional bundled fallback in Resources (no extension), used if the fetch fails.")]
        [SerializeField] private string offlineResource = "vr_catalog";

        public VrCatalog Catalog { get; private set; }
        public bool IsLoaded => Catalog != null;
        public IReadOnlyList<VrCategory> Categories =>
            Catalog?.Categories ?? (IReadOnlyList<VrCategory>)Array.Empty<VrCategory>();

        public event Action<VrCatalog> OnLoaded;
        public event Action<string> OnError;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>Fetch the catalog if not already loaded.</summary>
        public void EnsureLoaded()
        {
            if (IsLoaded) { OnLoaded?.Invoke(Catalog); return; }
            StartCoroutine(Fetch());
        }

        private string BaseUrl =>
            (OhhoPlatform.Instance != null && !string.IsNullOrEmpty(OhhoPlatform.Instance.PlatformBaseUrl))
                ? OhhoPlatform.Instance.PlatformBaseUrl
                : platformBaseUrl;

        private IEnumerator Fetch()
        {
            var url = $"{BaseUrl.TrimEnd('/')}/vr/catalog.json";
            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success && TryParse(req.downloadHandler.text, out var c))
            {
                Apply(c);
                yield break;
            }

            Debug.LogWarning($"[OhhoCatalog] Fetch failed ({req.error}); trying bundled fallback.");
            if (!string.IsNullOrEmpty(offlineResource))
            {
                var asset = Resources.Load<TextAsset>(offlineResource);
                if (asset != null && TryParse(asset.text, out var offline)) { Apply(offline); yield break; }
            }
            OnError?.Invoke(req.error ?? "catalog unavailable");
        }

        private void Apply(VrCatalog c)
        {
            Catalog = c;
            OnLoaded?.Invoke(c);
        }

        // ── Resolution helpers (UserRobot ids → displayable models) ───────────

        public VrRobotType FindRobotType(string id) =>
            Catalog?.RobotTypes?.Find(t => t.Id == id);

        public VrCategory FindCategory(string id) =>
            Catalog?.Categories?.Find(cat => cat.Id == id);

        public VrHardwareModel FindHardwareModel(string robotTypeId, string hardwareModelId)
        {
            var type = FindRobotType(robotTypeId);
            return type?.HardwareModels?.Find(m => m.Id == hardwareModelId);
        }

        /// <summary>Join a saved robot with its catalog entries for display.
        /// Returns null if the catalog isn't loaded yet.</summary>
        public GarageRobot Resolve(UserRobot robot)
        {
            if (robot == null || Catalog == null) return null;
            var type = FindRobotType(robot.RobotTypeId);
            return new GarageRobot
            {
                UserRobot = robot,
                RobotType = type,
                HardwareModel = FindHardwareModel(robot.RobotTypeId, robot.HardwareModelId),
                Category = type != null ? FindCategory(type.Category) : null,
            };
        }

        private static bool TryParse(string json, out VrCatalog catalog)
        {
            catalog = null;
            try { catalog = JsonConvert.DeserializeObject<VrCatalog>(json); return catalog != null; }
            catch (Exception e) { Debug.LogWarning($"[OhhoCatalog] parse error: {e.Message}"); return false; }
        }
    }
}
