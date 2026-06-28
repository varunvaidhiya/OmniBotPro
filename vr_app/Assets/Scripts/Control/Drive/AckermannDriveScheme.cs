using UnityEngine;
using OmniBot.VR.Core;

namespace OmniBot.VR.Control.Drive
{
    /// <summary>
    /// Ackermann steering drive — for car-like wheeled robots (front steering,
    /// rear drive). Left stick Y → forward/back linear velocity, right stick X →
    /// steering angle δ. The steering angle is mapped to an angular velocity
    /// (<c>ω = v·tan(δ)/L</c>) for the Twist message since ROS Twist doesn't carry
    /// a steering angle directly. No strafe.
    /// </summary>
    public sealed class AckermannDriveScheme : DriveSchemeBase
    {
        public override string HudHint => "Left stick Y: throttle · Right stick X: steer · Right grip: turbo";

        // Mapping the steering stick to an angular velocity at full throttle.
        // A real Ackermann robot would convert ω → δ on its side using its
        // wheelbase; this keeps the VR control simple and consistent.
        protected override TwistMsg ComputeVelocity(
            float lx, float ly, float rx, float ry, float speedMult, out bool hasInput)
        {
            bool hasFwd = Mathf.Abs(ly) > 0f;
            bool hasSteer = Mathf.Abs(rx) > 0f;
            hasInput = hasFwd || hasSteer;

            float v = Mathf.Clamp(ly * speedMult, -1f, 1f) * Profile.MaxLinVel;
            // Steering maps to angular velocity, scaled by MaxAngVel.
            // When stationary, still allow a turn-in-place approximation so the
            // user can reposition; a real Ackermann robot will ignore ω when v≈0.
            float omega = Mathf.Clamp(-rx * speedMult, -1f, 1f) * Profile.MaxAngVel;

            return new TwistMsg(
                new Vector3Msg(v, 0f, 0f),
                new Vector3Msg(0f, 0f, omega));
        }
    }
}
