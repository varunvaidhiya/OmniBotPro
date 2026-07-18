using UnityEngine;

namespace OmniBot.VR.Control.Manip
{
    /// <summary>
    /// FABRIK (Forward And Backward Reaching Inverse Kinematics) — a generic,
    /// free IK solver for arbitrary serial chains. Unlike the analytic
    /// <see cref="OmniBot.VR.Input.ArmIKSolver"/> which is pinned to SO-101
    /// geometry, FABRIK works with any set of link lengths and returns joint
    /// positions, which are then converted to angles for revolute joints.
    ///
    /// The algorithm (per Aristidou &amp; Lasenby 2011):
    /// 1. Forward pass — set the end-effector to the target, then work backward
    ///    from tip to root, placing each joint on the line to its child at the
    ///    correct link distance.
    /// 2. Backward pass — pin the root to its fixed position, then work forward
    ///    from root to tip, placing each joint on the line to its parent at the
    ///    correct link distance.
    /// 3. Repeat until the end-effector is within tolerance of the target or the
    ///    iteration budget is spent.
    ///
    /// Angle recovery: for each revolute joint, the angle is the signed angle
    /// between the previous link direction and the next link direction, projected
    /// onto the joint's rotation axis. This is an approximation (FABRIK doesn't
    /// model full 3D orientation constraints), but it is stable and sufficient
    /// for teleoperation where the human hand provides the target.
    /// </summary>
    public sealed class FabrikSolver
    {
        private const int MaxIterations = 30;
        private const float Tolerance = 0.002f; // 2 mm

        private Vector3[] _linkLengthsVec; // not used; kept for clarity
        private float[] _linkLengths;
        private Vector3[] _jointAxes;
        private float[] _jointMin;
        private float[] _jointMax;
        private int _numJoints;       // total joints including gripper
        private int _positionJoints;  // joints driving position (exclude gripper)

        /// <summary>
        /// Configure the solver for a chain. <paramref name="linkLengths"/> has
        /// <c>joints.Length - 1</c> entries (distance between consecutive joints).
        /// <paramref name="axes"/> is the local rotation axis per joint
        /// (up = yaw, right = pitch, forward = roll). Limits in radians.
        /// </summary>
        public void Configure(JointSpec[] joints, float[] linkLengths)
        {
            _numJoints = joints.Length;
            _positionJoints = Mathf.Max(0, _numJoints - 1); // last joint = gripper (not in position chain)

            // Link lengths: use provided, or fall back to equal segments.
            int numLinks = _positionJoints;
            _linkLengths = new float[numLinks];
            float totalReach = 0f;
            for (int i = 0; i < numLinks; i++)
            {
                if (linkLengths != null && i < linkLengths.Length)
                    _linkLengths[i] = linkLengths[i];
                else
                    _linkLengths[i] = 0.10f; // generic default
                totalReach += _linkLengths[i];
            }

            // Default joint axes: [yaw, pitch, pitch, pitch, roll, gripper]
            // (matches most 6-DOF arms). For other DOFs, cycle through the pattern.
            _jointAxes = new Vector3[_numJoints];
            Vector3[] pattern = { Vector3.up, Vector3.right, Vector3.right, Vector3.right, Vector3.forward, Vector3.right };
            for (int i = 0; i < _numJoints; i++)
                _jointAxes[i] = pattern[i % pattern.Length];

            _jointMin = new float[_numJoints];
            _jointMax = new float[_numJoints];
            for (int i = 0; i < _numJoints; i++)
            {
                _jointMin[i] = joints[i].Min;
                _jointMax[i] = joints[i].Max;
            }
        }

        /// <summary>Total reach (sum of link lengths), metres.</summary>
        public float TotalReach
        {
            get
            {
                float r = 0f;
                for (int i = 0; i < _linkLengths.Length; i++) r += _linkLengths[i];
                return r;
            }
        }

        /// <summary>
        /// Solve IK for the given target position (world space, relative to the
        /// arm base). Returns joint angles in radians, clamped to limits. The
        /// last angle (gripper) is copied from <paramref name="currentAngles"/>.
        /// </summary>
        public float[] Solve(Vector3 targetPos, float[] currentAngles)
        {
            if (_numJoints == 0) return new float[0];

            // ── Initialize joint positions along the chain (straight up from base)
            Vector3[] positions = new Vector3[_positionJoints + 1];
            Vector3 basePos = Vector3.zero;
            positions[0] = basePos;
            for (int i = 1; i <= _positionJoints; i++)
                positions[i] = positions[i - 1] + Vector3.up * _linkLengths[i - 1];

            // Clamp target to reachable space
            float dist = Vector3.Distance(basePos, targetPos);
            float maxReach = TotalReach * 0.98f;
            if (dist > maxReach)
                targetPos = basePos + (targetPos - basePos).normalized * maxReach;

            // ── FABRIK iterations
            for (int iter = 0; iter < MaxIterations; iter++)
            {
                // Forward pass: set tip to target, work backward
                positions[_positionJoints] = targetPos;
                for (int i = _positionJoints - 1; i >= 0; i--)
                {
                    Vector3 dir = (positions[i + 1] - positions[i]).normalized;
                    if (dir.sqrMagnitude < 1e-8f) dir = Vector3.up;
                    positions[i] = positions[i + 1] - dir * _linkLengths[i];
                }

                // Backward pass: pin root, work forward
                positions[0] = basePos;
                for (int i = 1; i <= _positionJoints; i++)
                {
                    Vector3 dir = (positions[i] - positions[i - 1]).normalized;
                    if (dir.sqrMagnitude < 1e-8f) dir = Vector3.up;
                    positions[i] = positions[i - 1] + dir * _linkLengths[i - 1];
                }

                // Convergence check
                if ((positions[_positionJoints] - targetPos).sqrMagnitude < Tolerance * Tolerance)
                    break;
            }

            // ── Convert positions to joint angles
            float[] angles = new float[_numJoints];
            for (int i = 0; i < _numJoints; i++)
                angles[i] = currentAngles != null && i < currentAngles.Length ? currentAngles[i] : 0f;

            for (int i = 0; i < _positionJoints; i++)
            {
                if (i == 0)
                {
                    // Root joint: angle from world up to first link direction
                    Vector3 linkDir = positions[1] - positions[0];
                    angles[i] = AngleAboutAxis(Vector3.up, linkDir, _jointAxes[i]);
                }
                else
                {
                    // Interior joint: angle between previous link and next link
                    Vector3 prevDir = positions[i] - positions[i - 1];
                    Vector3 nextDir = positions[i + 1] - positions[i];
                    angles[i] = AngleAboutAxis(prevDir, nextDir, _jointAxes[i]);
                }
                angles[i] = Mathf.Clamp(angles[i], _jointMin[i], _jointMax[i]);
            }

            // Gripper angle is preserved from current (set externally from pinch)
            return angles;
        }

        /// <summary>
        /// Computes the signed angle from <paramref name="from"/> to
        /// <paramref name="to"/> projected onto <paramref name="axis"/>, in radians.
        /// </summary>
        private static float AngleAboutAxis(Vector3 from, Vector3 to, Vector3 axis)
        {
            Vector3 projFrom = Vector3.ProjectOnPlane(from, axis).normalized;
            Vector3 projTo   = Vector3.ProjectOnPlane(to,   axis).normalized;
            if (projFrom.sqrMagnitude < 1e-6f || projTo.sqrMagnitude < 1e-6f) return 0f;
            return Vector3.SignedAngle(projFrom, projTo, axis) * Mathf.Deg2Rad;
        }
    }
}
