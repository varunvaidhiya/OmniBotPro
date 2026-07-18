using UnityEngine;

namespace OmniBot.VR.MR
{
    /// <summary>
    /// B button (left controller) shows/hides the unified OhhO screen — the
    /// readme's "toggle main HUD panel" control. Handy when the operator wants
    /// an unobstructed passthrough view while driving.
    ///
    /// Toggles the Canvas + raycaster components rather than the GameObject, so
    /// page routing state is preserved and the app keeps running underneath
    /// (connection, teleop, recording all continue while hidden).
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class ScreenToggle : MonoBehaviour
    {
        private bool _prevB;
        private Canvas _canvas;
        private Behaviour _raycaster;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _raycaster = GetComponent<OVRRaycaster>();
        }

        private void Update()
        {
            bool b = OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.LTouch);
            if (b && !_prevB) Toggle();
            _prevB = b;
        }

        /// <summary>Show or hide the screen.</summary>
        public void Toggle()
        {
            if (_canvas != null) _canvas.enabled = !_canvas.enabled;
            if (_raycaster != null) _raycaster.enabled = _canvas == null || _canvas.enabled;
        }
    }
}
