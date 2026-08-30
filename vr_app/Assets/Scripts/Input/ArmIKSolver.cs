using UnityEngine;
using OmniBot.VR.Core;

namespace OmniBot.VR.Input
{
    /// <summary>
    /// CCD (Cyclic Coordinate Descent) inverse kinematics solver for the
    /// SO-101 6-DOF arm.
    ///
    /// Joint chain (indices 0-5):
    ///   0: arm_shoulder_pan   (rotation about world-Z / base vertical)
    ///   1: arm_shoulder_lift  (rotation about joint-Y / lateral)
    ///   2: arm_elbow_flex     (rotation about joint-Y)
    ///   3: arm_wrist_flex     (rotation about joint-Y)
    ///   4: arm_wrist_roll     (rotation about joint-X / roll)
    ///   5: arm_gripper        (open/close — not part of position chain)
    ///
    /// The CCD loop iterates over joints 0-4 to bring the end-effector
    /// toward targetPos, then joints 3-4 are fine-tuned from targetRot.
    /// Joint 5 (gripper) is never modified by IK; use PinchToGripper().
    /// </summary>
    public class ArmIKSolver
    {
        // ── Link geometry ────────────────────────────────────────────────────
        // Link offsets in local joint space (metres).
        // Joint 0 sits at ARM_BASE_HEIGHT above robot origin.
        private static readonly Vector3[] LinkOffsets =
        {
            new Vector3(0f,  RobotConfig.ARM_BASE_HEIGHT, 0f), // world → joint0 base
            new Vector3(0f,  RobotConfig.ARM_LINK1_LENGTH, 0f), // joint0 → joint1
            new Vector3(0f,  RobotConfig.ARM_LINK2_LENGTH, 0f), // joint1 → joint2
            new Vector3(0f,  RobotConfig.ARM_LINK3_LENGTH, 0f), // joint2 → joint3 (wrist)
            new Vector3(0f,  0f, 0f),                           // joint3 → joint4 (wrist roll, same pos)
            new Vector3(0f,  0f, 0f),                           // joint4 → EE
        };

        // Local rotation axes for each joint
        private static readonly Vector3[] JointAxes =
        {
            Vector3.up,     // shoulder_pan  — yaw
            Vector3.right,  // shoulder_lift — pitch
            Vector3.right,  // elbow_flex    — pitch
            Vector3.right,  // wrist_flex    — pitch
            Vector3.forward,// wrist_roll    — roll
            Vector3.right,  // gripper       — not used in CCD
        };

        // Number of joints driving position (exclude gripper)
        private const int PositionJoints = 5;

        // CCD solver parameters
        private const int MaxIterations = 50;
        private const float Tolerance   = 0.001f; // metres

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Runs CCD IK and returns a new 6-element joint angle array.
        /// currentAngles is used as the starting configuration; it is not mutated.
        /// All returned angles are clamped to RobotConfig limits.
        /// </summary>
        /// <param name="targetPos">Desired end-effector world position (metres).</param>
        /// <param name="targetRot">Desired end-effector world rotation.</param>
        /// <param name="currentAngles">Current joint angles in radians (length 6).</param>
        /// <returns>New joint angles in radians (length 6).</returns>
        public float[] SolveIK(Vector3 targetPos, Quaternion targetRot, float[] currentAngles)
        {
            float[] angles = new float[6];
            for (int i = 0; i < 6; i++)
                angles[i] = currentAngles != null && currentAngles.Length == 6
                    ? currentAngles[i] : 0f;

            // ── DLS position loop ────────────────────────────────────────────
            float lambdaSq = 0.01f; // Damping factor squared
            for (int iter = 0; iter < MaxIterations; iter++)
            {
                // Forward kinematics to get all joint world positions/rotations
                Vector3[]    jointPos = new Vector3[PositionJoints + 1];
                Quaternion[] jointRot = new Quaternion[PositionJoints + 1];
                ForwardKinematics(angles, jointPos, jointRot, PositionJoints);

                Vector3 eePos = jointPos[PositionJoints];
                Vector3 deltaPos = targetPos - eePos;

                // Check convergence
                if (deltaPos.sqrMagnitude < Tolerance * Tolerance)
                    break;

                // Build Jacobian (3x5)
                Vector3[] J = new Vector3[PositionJoints];
                for (int j = 0; j < PositionJoints; j++)
                {
                    Quaternion parentRot = j > 0 ? jointRot[j] : Quaternion.identity;
                    Vector3 worldAxis = parentRot * JointAxes[j];
                    // Cross product of rotation axis and vector from joint to EE
                    J[j] = Vector3.Cross(worldAxis, eePos - jointPos[j]);
                }

                // Compute JJ^T (3x3)
                // JJ^T = sum_{j} J[j] * J[j]^T
                float m00 = lambdaSq, m01 = 0, m02 = 0;
                float m10 = 0, m11 = lambdaSq, m12 = 0;
                float m20 = 0, m21 = 0, m22 = lambdaSq;

                for (int j = 0; j < PositionJoints; j++)
                {
                    Vector3 Jj = J[j];
                    m00 += Jj.x * Jj.x; m01 += Jj.x * Jj.y; m02 += Jj.x * Jj.z;
                    m10 += Jj.y * Jj.x; m11 += Jj.y * Jj.y; m12 += Jj.y * Jj.z;
                    m20 += Jj.z * Jj.x; m21 += Jj.z * Jj.y; m22 += Jj.z * Jj.z;
                }

                // Invert M = JJ^T + lambda^2 I
                float det = m00 * (m11 * m22 - m12 * m21) - m01 * (m10 * m22 - m12 * m20) + m02 * (m10 * m21 - m11 * m20);
                if (Mathf.Abs(det) < 1e-6f) break; // Singular

                float invDet = 1f / det;
                float i00 =  (m11 * m22 - m12 * m21) * invDet;
                float i01 = -(m01 * m22 - m02 * m21) * invDet;
                float i02 =  (m01 * m12 - m02 * m11) * invDet;
                float i10 = -(m10 * m22 - m12 * m20) * invDet;
                float i11 =  (m00 * m22 - m02 * m20) * invDet;
                float i12 = -(m00 * m12 - m02 * m10) * invDet;
                float i20 =  (m10 * m21 - m11 * m20) * invDet;
                float i21 = -(m00 * m21 - m01 * m20) * invDet;
                float i22 =  (m00 * m11 - m01 * m10) * invDet;

                // dy = M^-1 * dx
                Vector3 dy = new Vector3(
                    i00 * deltaPos.x + i01 * deltaPos.y + i02 * deltaPos.z,
                    i10 * deltaPos.x + i11 * deltaPos.y + i12 * deltaPos.z,
                    i20 * deltaPos.x + i21 * deltaPos.y + i22 * deltaPos.z
                );

                // dTheta = J^T * dy
                for (int j = 0; j < PositionJoints; j++)
                {
                    float dTheta = Vector3.Dot(J[j], dy);
                    // DLS doesn't clamp per iteration usually but it's safe to enforce joint limits
                    angles[j] = ClampJoint(j, angles[j] + dTheta);
                }
            }

            // ── Wrist orientation pass ───────────────────────────────────────
            // Decompose targetRot into RPY relative to the forearm frame and
            // apply to wrist_flex (3), wrist_roll (4). Gripper (5) is unchanged.
            {
                // Get forearm-to-world rotation (after joint 2)
                Vector3[]    fkPos = new Vector3[4];
                Quaternion[] fkRot = new Quaternion[4];
                ForwardKinematics(angles, fkPos, fkRot, 3);
                Quaternion forearmWorldRot = fkRot[3]; // rotation at elbow tip

                // Target rotation relative to forearm
                Quaternion relRot = Quaternion.Inverse(forearmWorldRot) * targetRot;
                relRot.ToAngleAxis(out float wristAngleDeg, out Vector3 wristAxis);
                float wristAngleRad = wristAngleDeg * Mathf.Deg2Rad;

                // Decompose onto wrist axes using dot products
                float flexComponent = Vector3.Dot(wristAxis.normalized, Vector3.right) * wristAngleRad;
                float rollComponent = Vector3.Dot(wristAxis.normalized, Vector3.forward) * wristAngleRad;

                angles[3] = ClampJoint(3, angles[3] + flexComponent);
                angles[4] = ClampJoint(4, angles[4] + rollComponent);
            }

            return angles;
        }

        /// <summary>
        /// Maps pinch strength [0..1] (from OVR hand tracking) to gripper angle.
        /// pinch=0  → fully open  (ARM_JOINT_MAX[5])
        /// pinch=1  → fully closed (ARM_JOINT_MIN[5])
        /// </summary>
        public float PinchToGripper(float pinchStrength)
        {
            float t = Mathf.Clamp01(pinchStrength);
            // Open = max, closed = min
            return Mathf.Lerp(
                RobotConfig.ARM_JOINT_MAX[5],
                RobotConfig.ARM_JOINT_MIN[5],
                t);
        }

        /// <summary>Clamps a joint angle to its configured min/max limits.</summary>
        public static float ClampJoint(int index, float angle)
        {
            return Mathf.Clamp(angle,
                RobotConfig.ARM_JOINT_MIN[index],
                RobotConfig.ARM_JOINT_MAX[index]);
        }

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Computes world-space positions and rotations for joints 0..numJoints
        /// given the current joint angles. Index numJoints holds the end-effector.
        /// </summary>
        private static void ForwardKinematics(
            float[]      angles,
            Vector3[]    outPos,
            Quaternion[] outRot,
            int          numJoints)
        {
            Quaternion rotation = Quaternion.identity;
            Vector3    position = Vector3.zero;

            // Apply base offset (height of arm above robot base)
            position += new Vector3(0f, RobotConfig.ARM_BASE_HEIGHT, 0f);

            for (int i = 0; i < numJoints; i++)
            {
                // Rotate joint
                Quaternion jointRot = Quaternion.AngleAxis(angles[i] * Mathf.Rad2Deg, JointAxes[i]);
                rotation = rotation * jointRot;

                outPos[i] = position;
                outRot[i] = rotation;

                // Advance along the link (next joint's offset)
                int nextLinkIdx = i + 1;
                if (nextLinkIdx < LinkOffsets.Length)
                {
                    // Skip base offset (already applied before the loop)
                    Vector3 linkLocal = LinkOffsets[nextLinkIdx];
                    if (linkLocal.sqrMagnitude > 1e-8f)
                        position += rotation * linkLocal;
                }
            }

            // End-effector position
            outPos[numJoints] = position;
            if (outRot.Length > numJoints)
                outRot[numJoints] = rotation;
        }
    }
}
