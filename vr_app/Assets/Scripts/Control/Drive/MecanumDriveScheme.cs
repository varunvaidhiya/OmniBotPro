using UnityEngine;
using OmniBot.VR.Core;

namespace OmniBot.VR.Control.Drive
{
    /// <summary>
    /// Holonomic mecanum base drive — the OmniBot reference. Left thumbstick →
    /// vx/vy strafe, right thumbstick X → yaw. All clamped to
    /// <c>profile.MaxLinVel/MaxAngVel</c>. E-stop, turbo, publish timing and
    /// control-mode management are inherited from <see cref="DriveSchemeBase"/>.
    /// </summary>
    public sealed class MecanumDriveScheme : DriveSchemeBase
    {
        public override string HudHint => "Left stick: strafe · Right stick X: yaw · Right grip: turbo";

        protected override TwistMsg ComputeVelocity(
            float lx, float ly, float rx, float ry, float speedMult, out bool hasInput)
        {
            bool hasX = Mathf.Abs(lx) > 0f;
            bool hasY = Mathf.Abs(ly) > 0f;
            bool hasRot = Mathf.Abs(rx) > 0f;
            hasInput = hasX || hasY || hasRot;

            float vx    = Mathf.Clamp(ly * speedMult, -1f, 1f) * Profile.MaxLinVel;
            float vy    = Mathf.Clamp(lx * speedMult, -1f, 1f) * Profile.MaxLinVel;
            float omega = Mathf.Clamp(-rx * speedMult, -1f, 1f) * Profile.MaxAngVel;

            return new TwistMsg(
                new Vector3Msg(vx, vy, 0f),
                new Vector3Msg(0f, 0f, omega));
        }
    }
}
