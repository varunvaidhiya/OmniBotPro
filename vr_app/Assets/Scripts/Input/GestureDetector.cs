using UnityEngine;

namespace OmniBot.VR.Input
{
    /// <summary>
    /// Uses the Meta XR SDK OVRHand API to detect hand gestures, returning
    /// smoothed positions, rotations, pinch strengths, and estimated velocities.
    ///
    /// Exponential moving average (alpha = 0.7) is applied to raw position and
    /// rotation. Changes below the dead-zone thresholds are suppressed.
    /// </summary>
    public class GestureDetector : MonoBehaviour
    {
        // ── Inspector fields ─────────────────────────────────────────────────
        [SerializeField] public OVRHand rightHand;
        [SerializeField] public OVRHand leftHand;

        // ── Smoothing / dead-zone ────────────────────────────────────────────
        private const float SmoothAlpha          = 0.7f;   // EMA weight for new sample
        private const float PositionDeadZone     = 0.005f; // metres
        private const float RotationDeadZoneDeg  = 0.5f;   // degrees

        // ── Smoothed state ───────────────────────────────────────────────────
        private Vector3    _rightPosSmoothened    = Vector3.zero;
        private Quaternion _rightRotSmoothened    = Quaternion.identity;
        private Vector3    _leftPosSmoothened     = Vector3.zero;
        private Quaternion _leftRotSmoothened     = Quaternion.identity;

        // For velocity estimation
        private Vector3 _rightPosPrev  = Vector3.zero;
        private Vector3 _leftPosPrev   = Vector3.zero;
        private float   _lastUpdateTime = 0f;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            // Attempt to auto-assign hands if not set in inspector
            if (rightHand == null)
            {
                OVRHand[] hands = FindObjectsOfType<OVRHand>();
                foreach (var h in hands)
                {
                    if (h.GetHand() == OVRPlugin.Hand.HandRight) rightHand = h;
                    if (h.GetHand() == OVRPlugin.Hand.HandLeft)  leftHand  = h;
                }
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt < 1e-6f) dt = 1e-6f;

            // Update smoothed state for right hand
            if (rightHand != null && IsHandTracked(rightHand))
            {
                Vector3    rawPos = GetRawHandPosition(rightHand);
                Quaternion rawRot = GetRawHandRotation(rightHand);

                // Position smoothing with dead zone
                Vector3 deltaPos = rawPos - _rightPosSmoothened;
                if (deltaPos.magnitude > PositionDeadZone)
                    _rightPosSmoothened = Vector3.Lerp(_rightPosSmoothened, rawPos, SmoothAlpha);

                // Rotation smoothing with dead zone
                float angleDelta = Quaternion.Angle(_rightRotSmoothened, rawRot);
                if (angleDelta > RotationDeadZoneDeg)
                    _rightRotSmoothened = Quaternion.Slerp(_rightRotSmoothened, rawRot, SmoothAlpha);
            }

            // Update smoothed state for left hand
            if (leftHand != null && IsHandTracked(leftHand))
            {
                Vector3    rawPos = GetRawHandPosition(leftHand);
                Quaternion rawRot = GetRawHandRotation(leftHand);

                Vector3 deltaPos = rawPos - _leftPosSmoothened;
                if (deltaPos.magnitude > PositionDeadZone)
                    _leftPosSmoothened = Vector3.Lerp(_leftPosSmoothened, rawPos, SmoothAlpha);

                float angleDelta = Quaternion.Angle(_leftRotSmoothened, rawRot);
                if (angleDelta > RotationDeadZoneDeg)
                    _leftRotSmoothened = Quaternion.Slerp(_leftRotSmoothened, rawRot, SmoothAlpha);
            }

            _lastUpdateTime = Time.time;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns the smoothed world-space wrist position of the given hand.
        /// Falls back to the OVRHand's transform position if the skeleton is unavailable.
        /// </summary>
        public Vector3 GetHandPosition(OVRHand hand)
        {
            if (hand == null) return Vector3.zero;
            bool isRight = hand.GetHand() == OVRPlugin.Hand.HandRight;
            return isRight ? _rightPosSmoothened : _leftPosSmoothened;
        }

        /// <summary>
        /// Returns the smoothed world-space rotation of the given hand's wrist.
        /// </summary>
        public Quaternion GetHandRotation(OVRHand hand)
        {
            if (hand == null) return Quaternion.identity;
            bool isRight = hand.GetHand() == OVRPlugin.Hand.HandRight;
            return isRight ? _rightRotSmoothened : _leftRotSmoothened;
        }

        /// <summary>
        /// Returns the index-thumb pinch strength [0..1] for the given hand.
        /// 0 = fully open, 1 = fully pinched.
        /// </summary>
        public float GetPinchStrength(OVRHand hand)
        {
            if (hand == null || !IsHandTracked(hand)) return 0f;
            return hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        }

        /// <summary>Returns true if the hand is being tracked with at least low confidence.</summary>
        public bool IsHandTracked(OVRHand hand)
        {
            if (hand == null) return false;
            return hand.IsTracked &&
                   hand.HandConfidence >= OVRHand.TrackingConfidence.Low;
        }

        /// <summary>
        /// Estimates hand velocity in m/s from the delta between this frame's
        /// raw position and the previously smoothed position.
        /// </summary>
        public Vector3 GetHandVelocity(OVRHand hand)
        {
            if (hand == null) return Vector3.zero;

            float dt = Time.deltaTime;
            if (dt < 1e-6f) return Vector3.zero;

            bool isRight = hand.GetHand() == OVRPlugin.Hand.HandRight;
            if (isRight)
            {
                Vector3 vel = (_rightPosSmoothened - _rightPosPrev) / dt;
                _rightPosPrev = _rightPosSmoothened;
                return vel;
            }
            else
            {
                Vector3 vel = (_leftPosSmoothened - _leftPosPrev) / dt;
                _leftPosPrev = _leftPosSmoothened;
                return vel;
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>Gets the raw (unsmoothed) wrist bone world position.</summary>
        private Vector3 GetRawHandPosition(OVRHand hand)
        {
            OVRSkeleton skeleton = hand.GetComponent<OVRSkeleton>();
            if (skeleton != null && skeleton.IsInitialized)
            {
                var bones = skeleton.Bones;
                // Bone index 0 is the wrist root in OVRSkeleton
                if (bones != null && bones.Count > 0 && bones[0] != null)
                    return bones[0].Transform.position;
            }
            return hand.transform.position;
        }

        /// <summary>Gets the raw (unsmoothed) wrist bone world rotation.</summary>
        private Quaternion GetRawHandRotation(OVRHand hand)
        {
            OVRSkeleton skeleton = hand.GetComponent<OVRSkeleton>();
            if (skeleton != null && skeleton.IsInitialized)
            {
                var bones = skeleton.Bones;
                if (bones != null && bones.Count > 0 && bones[0] != null)
                    return bones[0].Transform.rotation;
            }
            return hand.transform.rotation;
        }
    }
}
