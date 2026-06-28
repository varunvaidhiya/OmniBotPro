using UnityEngine;
using OmniBot.VR.Core;

namespace OmniBot.VR.Control.Manip
{
    /// <summary>
    /// Dual-arm manipulation — for humanoid robots (Unitree G1, Figure 02) where
    /// two hands drive two independent arms. The right hand retargets to the
    /// right arm, the left hand to the left arm. Each arm is solved independently
    /// via <see cref="FabrikSolver"/> (generic chains, since humanoid arms rarely
    /// match the SO-101 analytic geometry). Pinch on either hand drives that
    /// hand's gripper.
    ///
    /// Publishes a single <see cref="JointStateMsg"/> with both arms' joint names
    /// and positions to <c>profile.Topics.ArmCommands</c> at 20 Hz. The arm-enable
    /// toggle (left thumbs-up) enables/disables both arms together.
    /// </summary>
    public sealed class DualArmManipulationScheme : IManipulationScheme
    {
        private const float PublishHz = 20f;
        private const float ThumbsUpHoldRequired = 0.5f;

        private RobotProfile _profile;
        private Transform _workspaceOrigin;
        private float _publishInterval;
        private float _publishTimer;
        private bool _armEnabled;

        // Two independent IK solvers (one per arm)
        private FabrikSolver _rightSolver;
        private FabrikSolver _leftSolver;
        private float[] _rightAngles;
        private float[] _leftAngles;

        // Joint name layout: [right arm joints..., left arm joints...]
        private string[] _allJointNames;
        private int _armJointCount; // joints per arm (including gripper)

        // Workspace origins for each arm (offset left/right from the center origin)
        private Vector3 _rightArmOffset;
        private Vector3 _leftArmOffset;

        public bool IsConfigured => _profile != null && _rightSolver != null;
        public bool ArmEnabled => _armEnabled;
        public string HudHint => "Right hand: right arm · Left hand: left arm · Pinch: gripper · Left thumbs-up (hold): toggle";

        public void Configure(RobotProfile profile, Transform workspaceOrigin)
        {
            _profile = profile;
            _workspaceOrigin = workspaceOrigin;
            _publishInterval = 1f / PublishHz;
            _publishTimer = 0f;
            _armEnabled = false;

            // For dual-arm, the profile's Joints array describes one arm.
            // We mirror it for both arms with left/right prefixed names.
            _armJointCount = profile.Joints != null ? profile.Joints.Length : 7;
            var rightJoints = new JointSpec[_armJointCount];
            var leftJoints = new JointSpec[_armJointCount];
            _allJointNames = new string[_armJointCount * 2];

            for (int i = 0; i < _armJointCount; i++)
            {
                var j = profile.Joints[i];
                rightJoints[i] = j;
                leftJoints[i] = j;
                _allJointNames[i] = "r_" + j.Name;                       // right arm
                _allJointNames[i + _armJointCount] = "l_" + j.Name;      // left arm
            }

            // Configure both FABRIK solvers with the same link lengths
            _rightSolver = new FabrikSolver();
            _leftSolver = new FabrikSolver();
            _rightSolver.Configure(rightJoints, profile.ArmLinkLengths);
            _leftSolver.Configure(leftJoints, profile.ArmLinkLengths);

            _rightAngles = new float[_armJointCount];
            _leftAngles = new float[_armJointCount];
            for (int i = 0; i < _armJointCount; i++)
            {
                _rightAngles[i] = profile.Joints[i].Home;
                _leftAngles[i] = profile.Joints[i].Home;
            }

            // Shoulder offsets: arms are spaced ~0.36 m apart on a humanoid torso.
            _rightArmOffset = new Vector3(+0.18f, 0f, 0f);
            _leftArmOffset = new Vector3(-0.18f, 0f, 0f);
        }

        public bool Tick(ref HandPose rightHand, ref HandPose leftHand, IRobotLink link)
        {
            if (!IsConfigured || _profile == null || !link.IsConnected) return false;
            if (!_armEnabled) return false;

            _publishTimer += Time.deltaTime;
            if (_publishTimer < _publishInterval) return false;
            _publishTimer -= _publishInterval;

            // Solve each arm independently if the corresponding hand is tracked.
            if (rightHand.Tracked)
                _rightAngles = SolveArm(_rightSolver, rightHand, _workspaceOrigin, _rightArmOffset, _rightAngles, _profile);

            if (leftHand.Tracked)
                _leftAngles = SolveArm(_leftSolver, leftHand, _workspaceOrigin, _leftArmOffset, _leftAngles, _profile);

            // Combine both arms into one JointState message
            var allAngles = new float[_armJointCount * 2];
            System.Array.Copy(_rightAngles, 0, allAngles, 0, _armJointCount);
            System.Array.Copy(_leftAngles, 0, allAngles, _armJointCount, _armJointCount);

            var msg = new JointStateMsg((string[])_allJointNames.Clone(), allAngles);
            link.Publish(_profile.Topics.ArmCommands, msg);
            return true;
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
            Debug.Log($"[DualArmManipulationScheme] Arms {(enabled ? "enabled" : "disabled")}");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private float[] SolveArm(
            FabrikSolver solver,
            HandPose hand,
            Transform origin,
            Vector3 armOffset,
            float[] currentAngles,
            RobotProfile profile)
        {
            // Hand position relative to the arm's shoulder
            Vector3 armBase = origin.position + armOffset;
            float scale = profile.ArmMaxReach / profile.HandWorkspaceRadius;

            Vector3 targetRel = (hand.Position - armBase) * scale;
            targetRel = Vector3.ClampMagnitude(targetRel, solver.TotalReach * 0.98f);

            // Solve in a frame relative to the arm base
            Vector3 localTarget = origin.InverseTransformPoint(armBase + targetRel);
            // Shift so the base is at origin for the solver
            Vector3 armBaseLocal = origin.InverseTransformPoint(armBase);
            localTarget -= armBaseLocal;

            float[] newAngles = solver.Solve(localTarget, currentAngles);

            // Gripper from pinch (last joint)
            if (newAngles.Length > 0 && profile.Joints.Length > 0)
            {
                int last = newAngles.Length - 1;
                var grip = profile.Joints[profile.Joints.Length - 1];
                newAngles[last] = Mathf.Lerp(grip.Max, grip.Min, Mathf.Clamp01(hand.PinchStrength));
            }

            return newAngles;
        }
    }
}
