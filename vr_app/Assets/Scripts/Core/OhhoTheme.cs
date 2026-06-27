using UnityEngine;

namespace OmniBot.VR.Core
{
    /// <summary>
    /// OhhO design tokens for the VR app — the headset UI mirrors the website's
    /// branding (app/globals.css). Defaults match the committed
    /// public/vr/manifest.json; <see cref="ApplyFrom"/> overrides them at runtime
    /// from the live manifest so the headset always tracks the site.
    /// </summary>
    public static class OhhoTheme
    {
        // ── Colours (mirror :root in website/app/globals.css) ─────────────────
        public static Color Bg         = Hex("#0A0E1A");
        public static Color Surface    = Hex("#0F1628");
        public static Color Cyan       = Hex("#00D4FF"); // primary accent
        public static Color Violet     = Hex("#7C3AED"); // secondary accent
        public static Color VioletLite = Hex("#A78BFA");
        public static Color Text       = Hex("#FFFFFF");
        public static Color Muted      = new Color(1f, 1f, 1f, 0.52f);
        public static Color Border     = new Color(1f, 1f, 1f, 0.07f);

        // ── Fonts (TMP font assets must be imported with these names) ─────────
        public static string DisplayFont = "Space Grotesk"; // headings
        public static string BodyFont    = "Inter";         // body
        public static string MonoFont    = "JetBrains Mono"; // micro-labels

        /// <summary>Accent colour for a product/category accent string.</summary>
        public static Color Accent(string accent) =>
            accent == "violet" ? Violet : Cyan;

        /// <summary>Override the tokens from a manifest theme block.</summary>
        public static void ApplyFrom(Platform.VrTheme theme)
        {
            if (theme == null) return;
            if (TryHex(theme.Bg, out var c)) Bg = c;
            if (TryHex(theme.Surface, out c)) Surface = c;
            if (TryHex(theme.Cyan, out c)) Cyan = c;
            if (TryHex(theme.Violet, out c)) Violet = c;
            if (TryHex(theme.VioletLite, out c)) VioletLite = c;
            if (TryHex(theme.Text, out c)) Text = c;
            if (theme.Fonts != null)
            {
                if (!string.IsNullOrEmpty(theme.Fonts.Display)) DisplayFont = theme.Fonts.Display;
                if (!string.IsNullOrEmpty(theme.Fonts.Body)) BodyFont = theme.Fonts.Body;
                if (!string.IsNullOrEmpty(theme.Fonts.Mono)) MonoFont = theme.Fonts.Mono;
            }
        }

        /// <summary>Parse a "#RRGGBB" / "#RRGGBBAA" hex string into a Color.</summary>
        public static Color Hex(string hex)
        {
            return TryHex(hex, out var c) ? c : Color.magenta;
        }

        private static bool TryHex(string hex, out Color color)
        {
            color = Color.magenta;
            if (string.IsNullOrEmpty(hex)) return false;
            return ColorUtility.TryParseHtmlString(hex, out color);
        }
    }
}
