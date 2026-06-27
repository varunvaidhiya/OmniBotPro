using TMPro;
using UnityEngine;
using OmniBot.VR.Core;

namespace OmniBot.VR.UI.Theme
{
    /// <summary>
    /// Applies an OhhO type role to a TMP label — font family + colour in one
    /// place, so headings, body and the cyan mono micro-labels look like the
    /// website everywhere.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class ThemedText : MonoBehaviour
    {
        public enum Role
        {
            Heading,  // Space Grotesk, full white
            Body,     // Inter, white
            Muted,    // Inter, 52% white
            Label,    // JetBrains Mono, cyan (the uppercase micro-labels)
            Accent,   // Inter, cyan
        }

        [SerializeField] private Role role = Role.Body;
        [Tooltip("Optional explicit font set; falls back to OhhoFontSet.Active.")]
        [SerializeField] private OhhoFontSet fontSet;

        private TMP_Text _text;

        private void Awake() => _text = GetComponent<TMP_Text>();
        private void OnEnable() => Apply();

        public void SetRole(Role r) { role = r; Apply(); }

        public void Apply()
        {
            if (_text == null) _text = GetComponent<TMP_Text>();
            if (_text == null) return;

            _text.color = ColorFor(role);

            var set = fontSet != null ? fontSet : OhhoFontSet.Active;
            var font = FontFor(role, set);
            if (font != null) _text.font = font;
        }

        private static Color ColorFor(Role r)
        {
            switch (r)
            {
                case Role.Heading: return OhhoTheme.Text;
                case Role.Body:    return OhhoTheme.Text;
                case Role.Muted:   return OhhoTheme.Muted;
                case Role.Label:   return OhhoTheme.Cyan;
                case Role.Accent:  return OhhoTheme.Cyan;
                default:           return OhhoTheme.Text;
            }
        }

        private static TMP_FontAsset FontFor(Role r, OhhoFontSet set)
        {
            if (set == null) return null;
            switch (r)
            {
                case Role.Heading: return set.display;
                case Role.Label:   return set.mono;
                default:           return set.body;
            }
        }
    }
}
