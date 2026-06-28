using UnityEngine;
using OmniBot.VR.Core;

namespace OmniBot.VR.Control.Drive
{
    /// <summary>
    /// Thruster-vectoring drive — for underwater ROVs (BlueROV2, etc.). Left
    /// stick X/Y → horizontal plane (surge + sway), right stick Y → depth (heave),
    /// right stick X → yaw. This maps the standard ROV control convention to a
    /// <see cref="TwistMsg"/> with surge→linear.x, sway→linear.y, heave→linear.z,
    /// yaw→angular.z. Clamped to <c>profile.MaxLinVel/MaxAngVel</c>.
    /// </summary>
    public sealed class ThrusterDriveScheme : DriveSchemeBase
    {
        public override string HudHint => "Left stick: surge/sway · Right stick Y: heave · Right stick X: yaw · Right grip: turbo";

        protected override string IdleMode => "offboard";

        protected override TwistMsg ComputeVelocity(
            float lx, float ly, float rx, float ry, float speedMult, out bool hasInput)
        {
            bool hasSurge = Mathf.Abs(ly) > 0f;
            bool hasSway  = Mathf.Abs(lx) > 0f;
            bool hasHeave = Mathf.Abs(ry) > 0f;
            bool hasYaw   = Mathf.Abs(rx) > 0f;
            hasInput = hasSurge || hasSway || hasHeave || hasYaw;

            // ROV body-frame: surge (forward) → x, sway (right) → y, heave (down/up) → z
            // Convention: right stick Y up → heave up (positive z = up in ROS ENU).
            float surge = Mathf.Clamp(ly * speedMult, -1f, 1f) * Profile.MaxLinVel;
            float sway  = Mathf.Clamp(lx * speedMult, -1f, 1f) * Profile.MaxLinVel;
            float heave = Mathf.Clamp(ry * speedMult, -1f, 1f) * Profile.MaxLinVel;
            float yaw   = Mathf.Clamp(-rx * speedMult, -1f, 1f) * Profile.MaxAngVel;

            return new TwistMsg(
                new Vector3Msg(surge, sway, heave),
                new Vector3Msg(0f, 0f, yaw));
        }
    }
}
