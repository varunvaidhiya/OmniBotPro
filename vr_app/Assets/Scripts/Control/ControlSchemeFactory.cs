using UnityEngine;
using OmniBot.VR.Control.Drive;
using OmniBot.VR.Control.Manip;

namespace OmniBot.VR.Control
{
    /// <summary>
    /// Selects the right <see cref="IDriveScheme"/> + <see cref="IManipulationScheme"/>
    /// for a <see cref="RobotProfile"/> — the C# counterpart of the website's
    /// strategy-per-<c>DriveKind</c> table. Dispatches every drive kind to its
    /// scheme and every arm topology to its manipulation scheme.
    ///
    /// Drive dispatch (mirrors <c>DRIVE_SPECS</c> on the website):
    /// <list type="bullet">
    /// <item><c>mecanum</c> → <see cref="MecanumDriveScheme"/> (holonomic strafe)</item>
    /// <item><c>differential</c> / <c>skid-steer</c> / <c>tracked</c> / <c>rocker-bogie</c> / <c>unknown</c> → <see cref="DifferentialDriveScheme"/></item>
    /// <item><c>ackermann</c> → <see cref="AckermannDriveScheme"/></item>
    /// <item><c>quadrotor</c> / <c>hexacopter</c> / <c>vtol</c> → <see cref="AerialDriveScheme"/></item>
    /// <item><c>quadruped</c> / <c>hexapod</c> / <c>bipedal</c> → <see cref="MecanumDriveScheme"/> (same vx/vy/ω; gait gestures are Phase 5)</item>
    /// <item><c>thruster</c> → <see cref="ThrusterDriveScheme"/></item>
    /// <item><c>fixed-base</c> / <c>fixed-wing</c> / <c>gantry</c> / <c>wearable</c> → null (no VR base drive)</item>
    /// </list>
    ///
    /// Manipulation dispatch:
    /// <list type="bullet">
    /// <item>6-DOF arm (SO-101, UR5e) → <see cref="HandIK6DofScheme"/></item>
    /// <item>Dual-arm humanoids (armDof ≥ 7, two hands) → <see cref="DualArmManipulationScheme"/></item>
    /// <item>1-DOF gripper-only → <see cref="GripperOnlyScheme"/></item>
    /// <item>No arm → null (manipulation layer absent)</item>
    /// </list>
    /// </summary>
    public static class ControlSchemeFactory
    {
        /// <summary>
        /// Build the drive scheme for the profile's <see cref="DriveKind"/>.
        /// Returns null for fixed-base / wearable / fixed-wing robots (no VR
        /// base-drive control).
        /// </summary>
        public static IDriveScheme CreateDriveScheme(RobotProfile profile)
        {
            if (profile == null) return null;
            IDriveScheme scheme;
            switch (profile.Drive)
            {
                case DriveKind.Mecanum:
                    scheme = new MecanumDriveScheme();
                    break;

                case DriveKind.Differential:
                case DriveKind.SkidSteer:
                case DriveKind.Tracked:
                case DriveKind.RockerBogie:
                case DriveKind.Unknown:
                    scheme = new DifferentialDriveScheme();
                    break;

                case DriveKind.Ackermann:
                    scheme = new AckermannDriveScheme();
                    break;

                case DriveKind.Quadrotor:
                case DriveKind.Hexacopter:
                case DriveKind.Vtol:
                    scheme = new AerialDriveScheme();
                    break;

                case DriveKind.Quadruped:
                case DriveKind.Hexapod:
                case DriveKind.Bipedal:
                    // Legged robots share the mecanum vx/vy/ω mapping; gait
                    // selection gestures (tap to trot, etc.) are Phase 5.
                    scheme = new MecanumDriveScheme();
                    break;

                case DriveKind.Thruster:
                    scheme = new ThrusterDriveScheme();
                    break;

                // No VR base drive for these:
                case DriveKind.FixedBase:
                case DriveKind.FixedWing:
                case DriveKind.Gantry:
                case DriveKind.Wearable:
                default:
                    if (profile.BaseDof > 0)
                        Debug.LogWarning($"[ControlSchemeFactory] No drive scheme for {profile.Drive}. Robot will not drive.");
                    return null;
            }
            scheme.Configure(profile);
            return scheme;
        }

        /// <summary>
        /// Build the manipulation scheme for the profile's arm. Returns null for
        /// robots with no arm (drones / AMRs) — the manipulation layer is simply
        /// absent in that case.
        /// </summary>
        public static IManipulationScheme CreateManipulationScheme(RobotProfile profile, Transform workspaceOrigin)
        {
            if (profile == null || !profile.HasArm || profile.ArmDof <= 0) return null;

            // 1-DOF: gripper-only (e.g. a simple pick-and-place AMR)
            if (profile.ArmDof == 1)
            {
                var scheme = new GripperOnlyScheme();
                scheme.Configure(profile, workspaceOrigin);
                return scheme;
            }

            // 6-DOF: hand-IK (SO-101, UR5e) — the common case
            if (profile.ArmDof == 6)
            {
                var scheme = new HandIK6DofScheme();
                scheme.Configure(profile, workspaceOrigin);
                return scheme;
            }

            // 7+ DOF: dual-arm humanoids (Unitree G1, Figure 02) — two hands → two arms
            if (profile.ArmDof >= 7)
            {
                var scheme = new DualArmManipulationScheme();
                scheme.Configure(profile, workspaceOrigin);
                return scheme;
            }

            // 2–5 DOF: generic FABRIK (simple arms, grippers with a few joints)
            if (profile.ArmDof >= 2 && profile.ArmDof <= 5)
            {
                var scheme = new HandIK6DofScheme();
                scheme.Configure(profile, workspaceOrigin);
                return scheme;
            }

            Debug.LogWarning($"[ControlSchemeFactory] No manipulation scheme for {profile.ArmDof}-DOF arm. Arm will not be driven.");
            return null;
        }
    }
}
