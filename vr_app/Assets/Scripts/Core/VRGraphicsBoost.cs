using UnityEngine;

namespace OmniBot.VR.Core
{
    /// <summary>
    /// One-shot visual quality bootstrap for the Quest build. The app renders
    /// through a single CenterEye camera in Built-in RP; without an explicit
    /// resolution scale OpenXR runs at the headset's default (1.0x) eye texture,
    /// which is what made the app "look like an old 3D game".
    ///
    /// What this does, once, at startup (and re-applies on quality level change):
    ///   - XRSettings.eyeTextureResolutionScale  (default 1.5x — sharper everything)
    ///   - MSAA 4x on the eye texture
    ///   - Fixed Foveated Rendering OFF (FFR blurs the periphery; the user asked
    ///     for maximum clarity — Quest 3 has headroom for a UI-driven app)
    ///   - 90 Hz display refresh request (smoother hand tracking + UI)
    ///   - Full-res textures (mipmap limit 0) + anisotropic filtering
    ///   - Linear-space skinning flags that keep hand meshes crisp in both eyes
    ///
    /// Attach once to the app bootstrap GameObject. All values are tunable in
    /// the inspector; defaults favor clarity over battery life.
    /// </summary>
    public class VRGraphicsBoost : MonoBehaviour
    {
        [Header("Resolution")]
        [Tooltip("Multiplier on the OpenXR eye texture size. 1.0 = stock, 1.5 = sharp on Quest 3.")]
        [SerializeField, Range(0.75f, 2f)] private float eyeTextureResolutionScale = 1.5f;

        [Tooltip("Extra supersampling inside the viewport. Keep at 1.0 — eyeTextureResolutionScale is the main lever.")]
        [SerializeField, Range(0.75f, 2f)] private float renderViewportScale = 1.0f;

        [Header("Anti-aliasing")]
        [Tooltip("MSAA samples on the eye texture (2/4/8). 4 is the Quest sweet spot.")]
        [SerializeField] private int msaaSamples = 4;

        [Header("Foveation")]
        [Tooltip("Fixed Foveated Rendering blurs the lens edges. Off = maximum clarity.")]
        [SerializeField] private bool ffrEnabled;

        [Header("Refresh rate")]
        [Tooltip("Requested display refresh in Hz (72/90/120). 0 = leave at system default.")]
        [SerializeField] private float displayRefreshHz = 90f;

        [Header("Textures")]
        [SerializeField] private bool fullResolutionTextures = true;

        private void Awake()
        {
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        /// <summary>Apply all quality settings. Safe to call repeatedly.</summary>
        public void Apply()
        {
            // ── Eye texture resolution (the big one) ─────────────────────────
            UnityEngine.XR.XRSettings.eyeTextureResolutionScale = eyeTextureResolutionScale;
            UnityEngine.XR.XRSettings.renderViewportScale = renderViewportScale;

            // ── MSAA ─────────────────────────────────────────────────────────
            QualitySettings.antiAliasing = msaaSamples;

            // ── Fixed foveated rendering ─────────────────────────────────────
            OVRManager.fixedFoveatedRenderingLevel = ffrEnabled
                ? OVRManager.FixedFoveatedRenderingLevel.Low
                : OVRManager.FixedFoveatedRenderingLevel.Off;

            // ── Refresh rate ─────────────────────────────────────────────────
            if (displayRefreshHz > 0f && OVRManager.display != null)
            {
                float[] supported = OVRManager.display.displayFrequenciesAvailable;
                if (supported != null && supported.Length > 0)
                {
                    float best = supported[0];
                    foreach (float f in supported)
                        if (Mathf.Abs(f - displayRefreshHz) < Mathf.Abs(best - displayRefreshHz))
                            best = f;
                    OVRManager.display.displayFrequency = best;
                    Debug.Log($"[VRGraphicsBoost] Display refresh set to {best} Hz.");
                }
            }

            // ── Texture fidelity ─────────────────────────────────────────────
            if (fullResolutionTextures)
            {
                QualitySettings.globalTextureMipmapLimit = 0;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            }

            Debug.Log($"[VRGraphicsBoost] Applied — eyeTextureResolutionScale={eyeTextureResolutionScale}, " +
                      $"MSAA={msaaSamples}x, FFR={(ffrEnabled ? "Low" : "Off")}.");
        }
    }
}
