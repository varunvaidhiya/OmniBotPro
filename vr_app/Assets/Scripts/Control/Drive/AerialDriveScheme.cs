using UnityEngine;
using OmniBot.VR.Core;

namespace OmniBot.VR.Control.Drive
{
    /// <summary>
    /// Aerial multirotor drive — for quadrotors, hexacopters and VTOLs. Left
    /// stick Y → altitude (vz), left stick X → yaw (ω); right stick X/Y →
    /// horizontal translation (vx, vy). This is the standard RC/drone mapping
    /// (mode 2): left stick = throttle + yaw, right stick = pitch + roll. Clamped
    /// to <c>profile.MaxLinVel/MaxAngVel</c>.
    ///
    /// The idle control mode is <c>"offboard"</c> (PX4/ROS 2 convention) instead
    /// of <c>"nav2"</c>, since drones don't use Nav2.
    /// </summary>
    public sealed class AerialDriveScheme : DriveSchemeBase
    {
        public override string HudHint => "Left stick Y: altitude · Left stick X: yaw · Right stick: translate · Right grip: turbo";

        protected override string IdleMode => "offboard";

        protected override TwistMsg ComputeVelocity(
            float lx, float ly, float rx, float ry, float speedMult, out bool hasInput)
        {
            bool hasAlt = Mathf.Abs(ly) > 0f;
            bool hasYaw = Mathf.Abs(lx) > 0f;
            bool hasHoriz = Mathf.Abs(rx) > 0f || Mathf.Abs(ry) > 0f;
            hasInput = hasAlt || hasYaw || hasHoriz;

            // Mode-2 RC mapping:
            //   Left stick Y  → vz (up = positive)
            //   Left stick X  → yaw rate (ω)
            //   Right stick Y → vx (forward)
            //   Right stick X → vy (right strafe)
            float vz    = Mathf.Clamp(ly * speedMult, -1f, 1f) * Profile.MaxLinVel;
            float omega = Mathf.Clamp(lx * speedMult, -1f, 1f) * Profile.MaxAngVel;
            float vx    = Mathf.Clamp(ry * speedMult, -1f, 1f) * Profile.MaxLinVel;
            float vy    = Mathf.Clamp(rx * speedMult, -1f, 1f) * Profile.MaxLinVel;

            return new TwistMsg(
                new Vector3Msg(vx, vy, vz),
                new Vector3Msg(0f, 0f, omega));
        }
    }
}
