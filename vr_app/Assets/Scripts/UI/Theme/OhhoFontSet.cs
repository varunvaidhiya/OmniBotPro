using TMPro;
using UnityEngine;

namespace OmniBot.VR.UI.Theme
{
    /// <summary>
    /// The three OhhO type families as TMP font assets — Space Grotesk (display),
    /// Inter (body), JetBrains Mono (mono), matching app/globals.css. Create one
    /// asset (Assets → Create → OhhO → Font Set), import the TMP fonts, and assign
    /// it on the ThemeApplier; <see cref="ThemedText"/> resolves fonts through it.
    /// </summary>
    [CreateAssetMenu(menuName = "OhhO/Font Set", fileName = "OhhoFontSet")]
    public class OhhoFontSet : ScriptableObject
    {
        public TMP_FontAsset display; // Space Grotesk
        public TMP_FontAsset body;    // Inter
        public TMP_FontAsset mono;    // JetBrains Mono

        /// <summary>Set by ThemeApplier so ThemedText can find fonts without a
        /// per-label reference.</summary>
        public static OhhoFontSet Active { get; set; }
    }
}
