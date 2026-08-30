using UnityEngine;
using Newtonsoft.Json;
using OmniBot.VR.Core;

namespace OmniBot.VR.Input
{
    /// <summary>
    /// Converts right-hand tracking into SO-101 arm joint commands published
    /// over ROSBridge at ARM_PUBLISH_HZ (20 Hz).
    ///
    /// Arm enable/disable is toggled by a left-hand thumbs-up gesture
    /// (thumb pointing upward while other fingers are curled).
    /// When the arm is disabled, no joint commands are published.
    /// </summary>
    public class HandTrackingArmController : MonoBehaviour
    {
        // ── Inspector fields ─────────────────────────────────────────────────
        [SerializeField] private GestureDetector gestureDetector;

        /// <summary>
        /// Transform placed at the robot arm's base position in the VR scene.
        /// Hand position is measured relative to this origin.
        /// </summary>
        [SerializeField] private Transform handWorkspaceOrigin;

        // ── State ────────────────────────────────────────────────────────────
        private bool    _armEnabled     = false;
        private float[] _currentAngles  = new float[6]; // last sent joint angles
        private ArmIKSolver _ikSolver   = new ArmIKSolver();

        // ── Publish timer ─────────────────────────────────────────────────────
        private float _publishInterval;
        private float _publishTimer = 0f;

        // ── Thumbs-up gesture debounce ────────────────────────────────────────
        private bool  _wasThumbsUp       = false;
        private float _thumbsUpHoldTimer = 0f;
        private const float ThumbsUpHoldRequired = 0.5f; // seconds hold to toggle

        // ── Velocity prediction ──────────────────────────────────────────────
        private Vector3 _lastWorldHandPos;
        private float   _lastHandPosTime;

        // ── Arm max reach (metres) ────────────────────────────────────────────
        private static readonly float ArmMaxReach =
            RobotConfig.ARM_LINK1_LENGTH +
            RobotConfig.ARM_LINK2_LENGTH +
            RobotConfig.ARM_LINK3_LENGTH; // ~0.33 m

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            _publishInterval = 1f / RobotConfig.ARM_PUBLISH_HZ;

            // Initialize joint angles to home (all zeros, gripper open)
            for (int i = 0; i < 6; i++)
                _currentAngles[i] = 0f;
                
            _lastHandPosTime = Time.time;
        }

        private void Start()
        {
            if (gestureDetector == null)
                gestureDetector = FindObjectOfType<GestureDetector>();

            if (handWorkspaceOrigin == null)
            {
                // Create a default workspace origin at arm base height
                GameObject origin = new GameObject("HandWorkspaceOrigin");
                origin.transform.position = new Vector3(0f, RobotConfig.ARM_BASE_HEIGHT, 0f);
                handWorkspaceOrigin = origin.transform;
                Debug.LogWarning("[HandTrackingArmController] handWorkspaceOrigin not set — created default at arm base height.");
            }
        }

        private void Update()
        {
            // ── Calibration ───────────────────────────────────────────────
            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                CalibrateHomePose();
            }

            // ── Left-hand thumbs-up gesture: toggle arm enable ────────────────
            if (gestureDetector != null &&
                gestureDetector.leftHand != null &&
                gestureDetector.IsHandTracked(gestureDetector.leftHand))
            {
                bool thumbsUp = IsThumbsUp(gestureDetector.leftHand);
                if (thumbsUp)
                {
                    _thumbsUpHoldTimer += Time.deltaTime;
                    if (!_wasThumbsUp && _thumbsUpHoldTimer >= ThumbsUpHoldRequired)
                    {
                        ToggleArmEnabled();
                        _wasThumbsUp = true;
                    }
                }
                else
                {
                    _thumbsUpHoldTimer = 0f;
                    _wasThumbsUp       = false;
                }
            }

            // ── Arm control (only when enabled and connected) ──────────────
            if (!_armEnabled || !ROSBridgeClient.Instance.IsConnected)
                return;

            if (gestureDetector == null ||
                gestureDetector.rightHand == null ||
                !gestureDetector.IsHandTracked(gestureDetector.rightHand))
                return;

            // ── Publish at 20 Hz ──────────────────────────────────────────
            _publishTimer += Time.deltaTime;
            if (_publishTimer < _publishInterval) return;
            _publishTimer -= _publishInterval;

            // 1. Get right hand position relative to workspace origin
            Vector3    worldHandPos = gestureDetector.GetHandPosition(gestureDetector.rightHand);
            Quaternion worldHandRot = gestureDetector.GetHandRotation(gestureDetector.rightHand);

            // Predict forward 30ms to handle latency
            float dt = Time.time - _lastHandPosTime;
            Vector3 velocity = dt > 0.001f ? (worldHandPos - _lastWorldHandPos) / dt : Vector3.zero;
            _lastWorldHandPos = worldHandPos;
            _lastHandPosTime = Time.time;
            
            Vector3 predictedWorldHandPos = worldHandPos + velocity * 0.03f; // 30ms latency prediction

            Vector3 localHandPos = handWorkspaceOrigin.InverseTransformPoint(predictedWorldHandPos);

            // 2. Scale: workspace radius → arm reach
            float scale = ArmMaxReach / RobotConfig.HAND_WORKSPACE_RADIUS;
            Vector3 targetPos = localHandPos * scale;

            // Convert local to world for IK (IK works in world/robot frame)
            Vector3    ikTargetPos = handWorkspaceOrigin.TransformPoint(targetPos / scale * ArmMaxReach / ArmMaxReach);
            Quaternion ikTargetRot = worldHandRot;

            // Re-express in a consistent frame: arm base world position
            Vector3 armBaseWorld = new Vector3(
                handWorkspaceOrigin.position.x,
                handWorkspaceOrigin.position.y,
                handWorkspaceOrigin.position.z);

            // Target relative to arm base
            Vector3 targetRelativeToBase = predictedWorldHandPos - armBaseWorld;
            targetRelativeToBase = targetRelativeToBase * scale;
            
            bool unreachable = targetRelativeToBase.magnitude > ArmMaxReach * 0.98f;
            targetRelativeToBase = Vector3.ClampMagnitude(targetRelativeToBase, ArmMaxReach * 0.98f);
            
            SetHandColor(unreachable ? Color.red : Color.white);

            Vector3 absoluteTarget = armBaseWorld + targetRelativeToBase;

            // 3. Solve IK
            float[] newAngles = _ikSolver.SolveIK(absoluteTarget, ikTargetRot, _currentAngles);

            // 4. Gripper from pinch strength
            float pinch = gestureDetector.GetPinchStrength(gestureDetector.rightHand);
            newAngles[5] = _ikSolver.PinchToGripper(pinch);

            _currentAngles = newAngles;

            // 5. Build and publish JointStateMsg
            var msg = new JointStateMsg(
                (string[])RobotConfig.ARM_JOINT_NAMES.Clone(),
                newAngles);

            ROSBridgeClient.Instance.Publish(RobotConfig.TOPIC_ARM_COMMANDS, msg);
        }

        // ── Public API ───────────────────────────────────────────────────────

        private void CalibrateHomePose()
        {
            if (gestureDetector == null || gestureDetector.rightHand == null) return;
            Vector3 handPos = gestureDetector.GetHandPosition(gestureDetector.rightHand);
            // The VR app resets the reference frame origin so that the current hand pose maps to the robot's default forward-facing arm pose
            handWorkspaceOrigin.position = handPos - new Vector3(0, 0, ArmMaxReach * 0.5f);
            handWorkspaceOrigin.rotation = Quaternion.identity;
            Debug.Log("[HandTrackingArmController] Calibrated home pose.");
        }
        
        private void SetHandColor(Color color)
        {
            if (gestureDetector == null || gestureDetector.rightHand == null) return;
            var smr = gestureDetector.rightHand.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null && smr.material != null)
            {
                smr.material.color = color;
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Toggles arm enable state and publishes /arm/enable.</summary>
        public void ToggleArmEnabled()
        {
            SetArmEnabled(!_armEnabled);
        }

        /// <summary>Explicitly sets arm enable state and publishes /arm/enable.</summary>
        public void SetArmEnabled(bool enabled)
        {
            _armEnabled = enabled;
            Debug.Log($"[HandTrackingArmController] Arm {(enabled ? "enabled" : "disabled")}");

            var boolMsg = new BoolMsg(enabled);
            ROSBridgeClient.Instance.Publish(RobotConfig.TOPIC_ARM_ENABLE, boolMsg);
        }

        /// <summary>Whether the arm controller is currently active.</summary>
        public bool ArmEnabled => _armEnabled;

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Detects a thumbs-up gesture: thumb pointing upward, all other fingers
        /// curled (pinch strength of non-index fingers > 0.5, thumb extended).
        ///
        /// Uses OVRSkeleton bone positions to infer finger extension.
        /// If bone data is unavailable, falls back to pinch-strength heuristic.
        /// </summary>
        private bool IsThumbsUp(OVRHand hand)
        {
            if (hand == null || !gestureDetector.IsHandTracked(hand)) return false;

            OVRSkeleton skeleton = hand.GetComponent<OVRSkeleton>();
            if (skeleton == null || !skeleton.IsInitialized)
            {
                // Fallback: index pinch low (thumb not pressing index) + hand pointing up
                float indexPinch  = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
                float middlePinch = hand.GetFingerPinchStrength(OVRHand.HandFinger.Middle);
                float ringPinch   = hand.GetFingerPinchStrength(OVRHand.HandFinger.Ring);
                float pinkyPinch  = hand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky);

                bool othersCurled = middlePinch > 0.6f && ringPinch > 0.6f && pinkyPinch > 0.6f;
                bool thumbFree    = indexPinch < 0.3f;

                // Check if hand's up direction is mostly world-up
                Vector3 handUp = hand.transform.up;
                bool    pointingUp = Vector3.Dot(handUp, Vector3.up) > 0.7f;

                return othersCurled && thumbFree && pointingUp;
            }

            // Use skeleton bones: compare thumb tip Y position to other finger tips
            var bones = skeleton.Bones;
            if (bones == null || bones.Count < 24) return false;

            // OVRSkeleton bone indices for finger tips (standard Meta hand skeleton):
            // Thumb tip: bone 5, Index tip: 10, Middle tip: 15, Ring tip: 20, Pinky tip: 24
            int thumbTipIdx  = (int)OVRSkeleton.BoneId.Hand_ThumbTip;
            int indexTipIdx  = (int)OVRSkeleton.BoneId.Hand_IndexTip;
            int middleTipIdx = (int)OVRSkeleton.BoneId.Hand_MiddleTip;
            int ringTipIdx   = (int)OVRSkeleton.BoneId.Hand_RingTip;
            int pinkyTipIdx  = (int)OVRSkeleton.BoneId.Hand_PinkyTip;

            // Wrist (bone 0) for reference
            Vector3 wristPos   = bones[0].Transform.position;
            Vector3 thumbTip   = bones[thumbTipIdx].Transform.position;
            Vector3 indexTip   = bones[indexTipIdx].Transform.position;
            Vector3 middleTip  = bones[middleTipIdx].Transform.position;
            Vector3 ringTip    = bones[ringTipIdx].Transform.position;
            Vector3 pinkyTip   = bones[pinkyTipIdx].Transform.position;

            // Thumb extended: thumb tip is higher (world Y) than wrist
            bool thumbExtended = thumbTip.y > wristPos.y + 0.04f;

            // Other fingers curled: their tips are near the wrist (distance < 0.06m)
            float curledThreshold = 0.07f;
            bool indexCurled  = Vector3.Distance(indexTip,  wristPos) < curledThreshold;
            bool middleCurled = Vector3.Distance(middleTip, wristPos) < curledThreshold;
            bool ringCurled   = Vector3.Distance(ringTip,   wristPos) < curledThreshold;
            bool pinkyCurled  = Vector3.Distance(pinkyTip,  wristPos) < curledThreshold;

            return thumbExtended && indexCurled && middleCurled && ringCurled && pinkyCurled;
        }
    }
}
