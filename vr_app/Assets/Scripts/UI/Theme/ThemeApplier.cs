using UnityEngine;
using OmniBot.VR.Core.Platform;

namespace OmniBot.VR.UI.Theme
{
    /// <summary>
    /// One-stop OhhO theming for a UI subtree. Publishes the font set and, when
    /// the manifest's branding tokens arrive (<see cref="OhhoPlatform"/> →
    /// <c>OhhoTheme</c>), restyles every themed widget underneath it — so the
    /// headset's look tracks whatever the website ships, with no rebuild.
    ///
    /// Put this on the UI root and assign the font set.
    /// </summary>
    public class ThemeApplier : MonoBehaviour
    {
        [SerializeField] private OhhoFontSet fontSet;
        [Tooltip("Re-style again whenever the platform manifest (theme) loads.")]
        [SerializeField] private bool reapplyOnManifest = true;

        private void OnEnable()
        {
            if (fontSet != null) OhhoFontSet.Active = fontSet;
            ApplyAll();
            if (reapplyOnManifest && OhhoPlatform.Instance != null)
                OhhoPlatform.Instance.OnLoaded += OnManifest;
        }

        private void OnDisable()
        {
            if (OhhoPlatform.Instance != null) OhhoPlatform.Instance.OnLoaded -= OnManifest;
        }

        private void OnManifest(VrManifest _) => ApplyAll(); // OhhoTheme is already updated by OhhoPlatform

        /// <summary>Restyle every themed widget in this subtree.</summary>
        public void ApplyAll()
        {
            foreach (var p in GetComponentsInChildren<GlassPanel>(true)) p.Apply();
            foreach (var t in GetComponentsInChildren<ThemedText>(true)) t.Apply();
            foreach (var b in GetComponentsInChildren<AccentButton>(true)) b.Apply();
        }
    }
}
