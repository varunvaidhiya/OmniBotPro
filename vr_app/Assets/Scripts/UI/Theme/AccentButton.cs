using UnityEngine;
using UnityEngine.UI;
using OmniBot.VR.Core;

namespace OmniBot.VR.UI.Theme
{
    /// <summary>
    /// Styles a <see cref="Button"/> with an OhhO accent — a filled primary action
    /// (cyan, like the website's "Send magic link") or a subtle secondary. Sets
    /// the button's colour states from <see cref="OhhoTheme"/>.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class AccentButton : MonoBehaviour
    {
        public enum Style { PrimaryCyan, PrimaryViolet, Subtle }

        [SerializeField] private Style style = Style.PrimaryCyan;

        private Button _button;

        private void Awake() => _button = GetComponent<Button>();
        private void OnEnable() => Apply();

        public void SetStyle(Style s) { style = s; Apply(); }

        public void Apply()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_button == null) return;

            Color baseColor = style == Style.PrimaryViolet ? OhhoTheme.Violet
                             : style == Style.Subtle ? OhhoTheme.Surface
                             : OhhoTheme.Cyan;

            if (_button.targetGraphic != null) _button.targetGraphic.color = baseColor;

            var colors = _button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Lighten(baseColor, 0.12f);
            colors.pressedColor = Lighten(baseColor, -0.12f);
            colors.selectedColor = baseColor;
            colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.4f);
            _button.colors = colors;
        }

        private static Color Lighten(Color c, float amount)
        {
            return new Color(
                Mathf.Clamp01(c.r + amount),
                Mathf.Clamp01(c.g + amount),
                Mathf.Clamp01(c.b + amount),
                c.a);
        }
    }
}
