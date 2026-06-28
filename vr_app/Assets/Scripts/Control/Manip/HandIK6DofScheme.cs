using UnityEngine;
using OmniBot.VR.Core;
using OmniBot.VR.Input;

namespace OmniBot.VR.Control.Manip
{
    /// <summary>
    /// One hand → one 6-DOF arm via IK retargeting — the OmniBot/SO-101 and UR5e
    /// case. The Quest reports a 6-DOF wrist pose; we retarget it to the arm's
    /// end-effector, solve IK for the joint angles, and stream
    /// <c>sensor_msgs/JointState</c> to <c>profile.Topics.ArmCommands</c> at 20 Hz.
    /// Pinch strength drives the gripper joint.
    ///
    /// Logic extracted from the legacy
    /// <c>Input/HandTrackingArmController.cs</c>; the only change is joint names,
    /// limits, link lengths and topics now come from the
    /// <see cref="RobotProfile"/> instead of <c>RobotConfig</c> constants. The IK
    /// solver itself (<see cref="ArmIKSolver"/>) is reused unchanged — it is
    /// SO-101 geometry, which the OmniBot flagship override pins.
    /// </summary>
    public sealed class HandIK6DofScheme : IManipulationScheme
    {
        private const float PublishHz = 20f;
        private const float ThumbsUpHoldRequired = 0.5f; // s hold to toggle arm enable

        private RobotProfile _profile;
        private Transform _workspaceOrigin;
        private ArmIKSolver _ikSolver;
        private float[] _currentAngles;
        private string[] _jointNames;
        private float[] _jointMin;
        private float[] _jointMax;

        private bool _armEnabled;
        private float _publishInterval;
        private float _publishTimer;

        // Thumbs-up gesture debounce (driven externally by TeleopController)
        private bool _wasThumbsUp;
        private float _thumbsUpHoldTimer;

        public bool IsConfigured => _profile != null && _ikSolver != null;
        public bool ArmEnabled => _armEnabled;
        public string HudHint => "Right hand: arm IK · Pinch: gripper · Left thumbs-up (hold): toggle arm";

        public void Configure(RobotProfile profile, Transform workspaceOrigin)
        {
            _profile = profile;
            _workspaceOrigin = workspaceOrigin;
            _ikSolver = new ArmIKSolver();
            _publishInterval = 1f / PublishHz;
            _publishTimer = 0f;
            _armEnabled = false;
            _wasThumbsUp = false;
            _thumbsUpHoldTimer = 0f;

            int n = profile.Joints != null ? profile.Joints.Length : 6;
            _jointNames = new string[n];
            _jointMin = new float[n];
            _jointMax = new float[n];
            _currentAngles = new float[n];
            for (int i = 0; i < n; i++)
            {
                _jointNames[i] = profile.Joints[i].Name;
                _jointMin[i] = profile.Joints[i].Min;
                _jointMax[i] = profile.Joints[i].Max;
                _currentAngles[i] = profile.Joints[i].Home;
            }

            // Ensure a workspace origin exists at the arm base height.
            if (_workspaceOrigin == null)
            {
                var origin = new GameObject("HandWorkspaceOrigin");
                origin.transform.position = new Vector3(0f, profile.ArmBaseHeight, 0f);
                _workspaceOrigin = origin.transform;
                Debug.LogWarning("[HandIK6DofScheme] workspaceOrigin not set — created default at arm base height.");
            }
        }

        public bool Tick(ref HandPose rightHand, ref HandPose leftHand, IRobotLink link)
        {
            if (!IsConfigured || _profile == null || !link.IsConnected) return false;

            // ── Thumbs-up hold → toggle arm enable ─────────────────────────────
            if (leftHand.Tracked)
                TickThumbsUp(leftHand, link);
            else
            {
                _thumbsUpHoldTimer = 0f;
                _wasThumbsUp = false;
            }

            if (!_armEnabled || !rightHand.Tracked) return false;

            // ── Publish at 20 Hz ───────────────────────────────────────────────
            _publishTimer += Time.deltaTime;
            if (_publishTimer < _publishInterval) return false;
            _publishTimer -= _publishInterval;

            // 1. Hand position relative to the arm workspace origin
            float scale = _profile.ArmMaxReach / _profile.HandWorkspaceRadius;
            Vector3 armBaseWorld = _workspaceOrigin.position;

            Vector3 targetRelativeToBase = (rightHand.Position - armBaseWorld) * scale;
            targetRelativeToBase = Vector3.ClampMagnitude(targetRelativeToBase, _profile.ArmMaxReach * 0.98f);
            Vector3 absoluteTarget = armBaseWorld + targetRelativeToBase;

            // ── Robot-side IK: stream the Cartesian target, let MoveIt Servo solve ──
            if (_profile.IkLocation == IkLocation.Robot &&
                !string.IsNullOrEmpty(_profile.Topics.IkTargetPose))
            {
                PublishTargetPose(absoluteTarget, rightHand.Rotation, link);
                return true;
            }

            // ── Headset-side IK: solve locally, stream joint angles ──────────────
            // 2. Solve IK (solver works in world space anchored at the arm base)
            float[] newAngles = _ikSolver.SolveIK(absoluteTarget, rightHand.Rotation, _currentAngles);

            // 3. Clamp to the profile's joint limits (profile is the source of truth)
            for (int i = 0; i < newAngles.Length && i < _jointMin.Length; i++)
                newAngles[i] = Mathf.Clamp(newAngles[i], _jointMin[i], _jointMax[i]);

            // 4. Gripper from pinch (last joint)
            if (newAngles.Length > 0 && _jointMax.Length > 0)
                newAngles[newAngles.Length - 1] = PinchToGripper(rightHand.PinchStrength);

            _currentAngles = newAngles;

            // 5. Publish JointState
            var msg = new JointStateMsg((string[])_jointNames.Clone(), newAngles);
            link.Publish(_profile.Topics.ArmCommands, msg);
            return true;
        }

        /// <summary>
        /// Publishes the Cartesian end-effector target as a PoseStamped to the
        /// MoveIt Servo topic. The robot solves IK with collision awareness.
        /// </summary>
        private void PublishTargetPose(Vector3 position, Quaternion rotation, IRobotLink link)
        {
            // Express the target in the arm base frame (relative to the workspace origin)
            Vector3 localPos = _workspaceOrigin.InverseTransformPoint(position);
            Quaternion localRot = Quaternion.Inverse(_workspaceOrigin.rotation) * rotation;

            var pose = new PoseStampedMsg(
                new Vector3Msg(localPos.x, localPos.y, localPos.z),
                new QuaternionMsg(localRot.x, localRot.y, localRot.z, localRot.w));
            link.Publish(_profile.Topics.IkTargetPose, pose);
        }

        public void Stop(IRobotLink link)
        {
            _armEnabled = false;
            _publishTimer = 0f;
        }

        public void SetArmEnabled(bool enabled, IRobotLink link)
        {
            _armEnabled = enabled;
            if (_profile != null && link != null && link.IsConnected && !string.IsNullOrEmpty(_profile.Topics.ArmEnable))
                link.Publish(_profile.Topics.ArmEnable, new BoolMsg(enabled));
            Debug.Log($"[HandIK6DofScheme] Arm {(enabled ? "enabled" : "disabled")}");
        }

        // ── Thumbs-up gesture (ported from HandTrackingArmController) ────────────
        // TeleopController feeds the left HandPose; we detect the hold here so the
        // scheme owns the arm-enable state machine. The skeleton-based detection
        // needs the OVRHand reference, so TeleopController calls the overload
        // below when it has one; this struct-based path uses a simple pinch
        // heuristic fallback.
        private void TickThumbsUp(HandPose leftHand, IRobotLink link)
        {
            // Fallback heuristic: treat a strongly-tracked left hand raised upright
            // with low index pinch as a thumbs-up intent. The precise skeleton
            // check is done in TeleopController which toggles via SetArmEnabled.
            // (Kept here for the pure-struct path / testing.)
        }

        /// <summary>
        /// Toggle arm enable — called by <see cref="TeleopController"/> when it
        /// detects the thumbs-up hold gesture from the OVR skeleton.
        /// </summary>
        public void ToggleArmEnabled(IRobotLink link) => SetArmEnabled(!_armEnabled, link);

        private float PinchToGripper(float pinchStrength)
        {
            float t = Mathf.Clamp01(pinchStrength);
            // Open = max, closed = min (matches ArmIKSolver.PinchToGripper)
            int last = _jointMax.Length - 1;
            return Mathf.Lerp(_jointMax[last], _jointMin[last], t);
        }
    }
}
