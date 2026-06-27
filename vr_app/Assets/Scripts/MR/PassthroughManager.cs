using UnityEngine;

namespace OmniBot.VR.MR
{
    /// <summary>
    /// Brings up mixed reality: makes the headset camera transparent so Quest
    /// passthrough shows the real room behind the OhhO UI, and enables the
    /// passthrough layer.
    ///
    /// Kept SDK-agnostic on purpose — assign the Meta <c>OVRPassthroughLayer</c>
    /// (a Behaviour) to <see cref="passthroughLayer"/> in the inspector and this
    /// just toggles it, so the script compiles without a hard Meta SDK type
    /// reference. Project Settings → enable the Passthrough OpenXR feature (see
    /// SCENE_SETUP.md).
    /// </summary>
    public class PassthroughManager : MonoBehaviour
    {
        [Tooltip("The headset camera. Defaults to Camera.main.")]
        [SerializeField] private Camera hmdCamera;

        [Tooltip("Assign the OVRPassthroughLayer component here (it's a Behaviour).")]
        [SerializeField] private Behaviour passthroughLayer;

        [SerializeField] private bool enableOnStart = true;

        private CameraClearFlags _prevClear;
        private Color _prevBg;

        private void Start()
        {
            if (hmdCamera == null) hmdCamera = Camera.main;
            if (enableOnStart) EnablePassthrough(true);
        }

        /// <summary>Turn mixed-reality passthrough on or off.</summary>
        public void EnablePassthrough(bool on)
        {
            if (hmdCamera != null)
            {
                if (on)
                {
                    _prevClear = hmdCamera.clearFlags;
                    _prevBg = hmdCamera.backgroundColor;
                    hmdCamera.clearFlags = CameraClearFlags.SolidColor;
                    hmdCamera.backgroundColor = new Color(0f, 0f, 0f, 0f); // transparent → passthrough shows through
                }
                else
                {
                    hmdCamera.clearFlags = _prevClear;
                    hmdCamera.backgroundColor = _prevBg;
                }
            }

            if (passthroughLayer != null) passthroughLayer.enabled = on;
            else if (on) Debug.LogWarning("[PassthroughManager] No passthrough layer assigned — UI will render on a black background.");
        }
    }
}
