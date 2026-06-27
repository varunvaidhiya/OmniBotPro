using UnityEngine;
using UnityEngine.UI;
using OmniBot.VR.Core;

namespace OmniBot.VR.UI.Theme
{
    /// <summary>
    /// Turns a UI <see cref="Graphic"/> (Image/RawImage) or a panel quad into an
    /// OhhO glass card — the website's GlassCard look, recreated for VR via the
    /// <c>OhhO/Glass</c> shader. Fill + accent come from <see cref="OhhoTheme"/>,
    /// so panels restyle automatically when the manifest theme loads.
    ///
    /// Requires the shader to be reachable at runtime: add "OhhO/Glass" to
    /// Project Settings → Graphics → Always Included Shaders (see SCENE_SETUP.md).
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class GlassPanel : MonoBehaviour
    {
        public enum Accent { Cyan, Violet }

        [SerializeField] private Accent accent = Accent.Cyan;
        [Tooltip("0 = use the theme surface fill; otherwise override here.")]
        [SerializeField] private Color fillOverride = new Color(0, 0, 0, 0);

        private Graphic _graphic;
        private Material _material;

        private static readonly int FillId = Shader.PropertyToID("_FillColor");
        private static readonly int AccentId = Shader.PropertyToID("_AccentColor");

        private void Awake() => _graphic = GetComponent<Graphic>();

        private void OnEnable()
        {
            EnsureMaterial();
            Apply();
        }

        /// <summary>Re-read the tokens and restyle (call after the theme changes).</summary>
        public void Apply()
        {
            if (_material == null) return;
            Color fill = fillOverride.a > 0f
                ? fillOverride
                : new Color(OhhoTheme.Surface.r, OhhoTheme.Surface.g, OhhoTheme.Surface.b, 0.62f);
            _material.SetColor(FillId, fill);
            _material.SetColor(AccentId, accent == Accent.Violet ? OhhoTheme.Violet : OhhoTheme.Cyan);
        }

        public void SetAccent(Accent a) { accent = a; Apply(); }

        private void EnsureMaterial()
        {
            if (_material != null) return;
            var shader = Shader.Find("OhhO/Glass");
            if (shader == null)
            {
                Debug.LogWarning("[GlassPanel] Shader 'OhhO/Glass' not found — add it to Always Included Shaders.");
                return;
            }
            _material = new Material(shader) { name = "OhhoGlass (instance)" };
            if (_graphic != null) _graphic.material = _material;
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
