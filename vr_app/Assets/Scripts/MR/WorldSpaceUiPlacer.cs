using UnityEngine;

namespace OmniBot.VR.MR
{
    /// <summary>
    /// Parks a world-space UI panel in front of the user in mixed reality and,
    /// optionally, keeps it gently facing them (billboard). Used for the login /
    /// console / garage panels so they float comfortably in the room over
    /// passthrough rather than being locked to the head.
    ///
    /// A later phase anchors the teleop workspace to a real surface via MRUK; this
    /// is the lightweight "place a floating panel" helper for menus.
    /// </summary>
    public class WorldSpaceUiPlacer : MonoBehaviour
    {
        [SerializeField] private Camera hmdCamera;
        [Tooltip("Metres in front of the user.")]
        [SerializeField] private float distance = 1.2f;
        [Tooltip("Metres below eye level (panels read better slightly low).")]
        [SerializeField] private float verticalOffset = -0.15f;
        [SerializeField] private bool placeOnEnable = true;
        [SerializeField] private bool billboard = true;
        [Tooltip("How quickly the panel re-faces the user (0 = instant).")]
        [SerializeField] private float billboardLerp = 6f;

        private void OnEnable()
        {
            if (hmdCamera == null) hmdCamera = Camera.main;
            if (placeOnEnable) PlaceInFront();
        }

        /// <summary>Snap the panel to a comfortable spot in front of the user.</summary>
        public void PlaceInFront()
        {
            if (hmdCamera == null) hmdCamera = Camera.main;
            if (hmdCamera == null) return;

            // Use a flattened forward so the panel sits upright regardless of head pitch.
            Vector3 fwd = hmdCamera.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            fwd.Normalize();

            transform.position = hmdCamera.transform.position + fwd * distance + Vector3.up * verticalOffset;
            FaceUser(instant: true);
        }

        private void LateUpdate()
        {
            if (billboard && hmdCamera != null) FaceUser(instant: false);
        }

        private void FaceUser(bool instant)
        {
            if (hmdCamera == null) return;
            Vector3 toCam = transform.position - hmdCamera.transform.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude < 1e-4f) return;

            Quaternion target = Quaternion.LookRotation(toCam.normalized, Vector3.up);
            transform.rotation = instant || billboardLerp <= 0f
                ? target
                : Quaternion.Slerp(transform.rotation, target, Time.deltaTime * billboardLerp);
        }
    }
}
