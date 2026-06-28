using UnityEngine;
using OmniBot.VR.Core;

namespace OmniBot.VR.Control.Drive
{
    /// <summary>
    /// Differential / skid-steer / tracked drive — the most common wheeled base
    /// (TurtleBot 4, skid-steer, tracked robots). Left stick Y → forward/back
    /// linear velocity, right stick X → yaw angular velocity. No strafe (the
    /// action vector is <c>[v, ω]</c>). Clamped to
    /// <c>profile.MaxLinVel/MaxAngVel</c>.
    /// </summary>
    public sealed class DifferentialDriveScheme : DriveSchemeBase
    {
        public override string HudHint => "Left stick Y: drive · Right stick X: turn · Right grip: turbo";

        protected override TwistMsg ComputeVelocity(
            float lx, float ly, float rx, float ry, float speedMult, out bool hasInput)
        {
            bool hasFwd = Mathf.Abs(ly) > 0f;
            bool hasRot = Mathf.Abs(rx) > 0f;
            hasInput = hasFwd || hasRot;

            float v     = Mathf.Clamp(ly * speedMult, -1f, 1f) * Profile.MaxLinVel;
            float omega = Mathf.Clamp(-rx * speedMult, -1f, 1f) * Profile.MaxAngVel;

            // No strafe — linear.y stays zero (non-holonomic constraint).
            return new TwistMsg(
                new Vector3Msg(v, 0f, 0f),
                new Vector3Msg(0f, 0f, omega));
        }
    }
}
