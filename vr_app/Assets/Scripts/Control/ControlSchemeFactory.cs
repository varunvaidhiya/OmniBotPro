using UnityEngine;
using OmniBot.VR.Control.Drive;
using OmniBot.VR.Control.Manip;

namespace OmniBot.VR.Control
{
    /// <summary>
    /// Selects the right <see cref="IDriveScheme"/> + <see cref="IManipulationScheme"/>
    /// for a <see cref="RobotProfile"/> — the C# counterpart of the website's
    /// strategy-per-<c>DriveKind</c> table. Today it ships the OmniBot reference
    /// pair (<c>MecanumDriveScheme</c> + <c>HandIK6DofScheme</c>); Phase 3 grows
    /// this into the full factory (differential, ackermann, quadrotor, quadruped,
    /// dual-arm, gripper-only, none).
    /// </summary>
    public static class ControlSchemeFactory
    {
        /// <summary>
        /// Build the drive scheme for the profile's <see cref="DriveKind"/>.
        /// Returns null for fixed-base / wearable robots (no base to drive).
        /// </summary>
        public static IDriveScheme CreateDriveScheme(RobotProfile profile)
        {
            if (profile == null) return null;
            switch (profile.Drive)
            {
                case DriveKind.Mecanum:
                case DriveKind.SkidSteer:
                case DriveKind.Tracked:
                case DriveKind.Differential:
                case DriveKind.Unknown:
                    // All wheeled holonomic/differential bases share the mecanum
                    // mapping for now (vx/vy/ω); differential just ignores vy.
                    // Phase 3 adds a dedicated DifferentialDriveScheme.
                    var scheme = new MecanumDriveScheme();
                    scheme.Configure(profile);
                    return scheme;

                // Aerial, legged, thruster, gantry → Phase 3.
                default:
                    if (profile.BaseDof > 0)
                        Debug.LogWarning($"[ControlSchemeFactory] No drive scheme yet for {profile.Drive} (Phase 3). Robot will not drive.");
                    return null;
            }
        }

        /// <summary>
        /// Build the manipulation scheme for the profile's arm. Returns null for
        /// robots with no arm (drones / AMRs) — the manipulation layer is simply
        /// absent in that case.
        /// </summary>
        public static IManipulationScheme CreateManipulationScheme(RobotProfile profile, Transform workspaceOrigin)
        {
            if (profile == null || !profile.HasArm || profile.ArmDof <= 0) return null;

            // HandIK6Dof covers 6-DOF arms (SO-101, UR5e). Other DOFs → Phase 3
            // (DualArm for 14-DOF humanoids, GripperOnly for gripper-only bots).
            if (profile.ArmDof == 6)
            {
                var scheme = new HandIK6DofScheme();
                scheme.Configure(profile, workspaceOrigin);
                return scheme;
            }

            Debug.LogWarning($"[ControlSchemeFactory] No manipulation scheme yet for {profile.ArmDof}-DOF arm (Phase 3). Arm will not be driven.");
            return null;
        }
    }
}
