using UnityEngine;
using OmniBot.VR.Core;

namespace OmniBot.VR.Control.Drive
{
    /// <summary>
    /// Holonomic mecanum base drive — the OmniBot reference. Left thumbstick →
    /// vx/vy strafe, right thumbstick X → yaw. Turbo on right grip (2×, still
    /// clamped to the profile's <c>MaxLinVel/MaxAngVel</c>). Emergency stop on
    /// both grips. Publishes <c>Twist</c> to <c>profile.Topics.CmdVel</c> at 20 Hz
    /// and manages the <c>/control_mode</c> string (teleop while driving, nav2
    /// after 0.5 s idle).
    ///
    /// Logic extracted verbatim from the legacy <c>Input/BaseController.cs</c> so
    /// the behaviour is identical — the only change is it reads limits/topic from
    /// the <see cref="RobotProfile"/> instead of <c>RobotConfig</c> constants.
    /// </summary>
    public sealed class MecanumDriveScheme : IDriveScheme
    {
        private const float DeadZone = 0.1f;
        private const float TurboMultiplier = 2.0f;
        private const float ZeroStopDelay = 0.1f;   // s before we stop publishing zeros
        private const float NavModeRevertDelay = 0.5f; // s idle → revert to nav2
        private const float PublishHz = 20f;

        private RobotProfile _profile;
        private float _publishInterval;
        private float _publishTimer;

        private bool _emergencyStopActive;
        private bool _prevBothGrips;
        private bool _isPublishing;
        private float _noInputTimer;
        private float _noInputModeTimer;
        private bool _inTeleopMode;

        public bool IsConfigured => _profile != null;
        public string HudHint => "Left stick: strafe · Right stick X: yaw · Right grip: turbo";

        public void Configure(RobotProfile profile)
        {
            _profile = profile;
            _publishInterval = 1f / PublishHz;
            _publishTimer = 0f;
            _emergencyStopActive = false;
            _prevBothGrips = false;
            _isPublishing = false;
            _noInputTimer = 0f;
            _noInputModeTimer = 0f;
            _inTeleopMode = false;
        }

        public void Tick(ref TeleopInput input, IRobotLink link)
        {
            if (!IsConfigured || _profile == null || !link.IsConnected) return;

            // ── Emergency stop: both grips (rising/falling edge) ───────────────
            if (input.BothGripsPressed)
            {
                _emergencyStopActive = true;
                link.Publish(_profile.Topics.EmergencyStop, new BoolMsg(true));
                OVRInput.SetControllerVibration(1f, 1f, OVRInput.Controller.LTouch);
                OVRInput.SetControllerVibration(1f, 1f, OVRInput.Controller.RTouch);
                Debug.Log("[MecanumDriveScheme] Emergency stop ACTIVATED.");
            }
            else if (input.BothGripsReleased && _emergencyStopActive)
            {
                _emergencyStopActive = false;
                link.Publish(_profile.Topics.EmergencyStop, new BoolMsg(false));
                OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
                OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
                Debug.Log("[MecanumDriveScheme] Emergency stop RELEASED.");
            }
            _prevBothGrips = input.BothGrips;

            if (_emergencyStopActive) return;

            // ── Thumbsticks (dead-zoned) ───────────────────────────────────────
            float lx = ApplyDeadZone(input.LeftStick.x);
            float ly = ApplyDeadZone(input.LeftStick.y);
            float rx = ApplyDeadZone(input.RightStick.x);

            bool hasInput = Mathf.Abs(lx) > 0f || Mathf.Abs(ly) > 0f || Mathf.Abs(rx) > 0f;

            // Turbo: right grip held (but NOT both grips)
            bool turbo = input.RightGrip && !input.LeftGrip;
            float speedMult = turbo ? TurboMultiplier : 1.0f;

            float vx    = Mathf.Clamp(ly * speedMult, -1f, 1f) * _profile.MaxLinVel;
            float vy    = Mathf.Clamp(lx * speedMult, -1f, 1f) * _profile.MaxLinVel;
            float omega = Mathf.Clamp(-rx * speedMult, -1f, 1f) * _profile.MaxAngVel;

            // ── Control-mode management ────────────────────────────────────────
            if (hasInput)
            {
                _noInputTimer = 0f;
                _noInputModeTimer = 0f;
                if (!_inTeleopMode)
                {
                    _inTeleopMode = true;
                    PublishControlMode("teleop", link);
                }
            }
            else
            {
                _noInputTimer += input.DeltaTime;
                _noInputModeTimer += input.DeltaTime;
                if (_inTeleopMode && _noInputModeTimer >= NavModeRevertDelay)
                {
                    _inTeleopMode = false;
                    PublishControlMode("nav2", link);
                }
            }

            // ── Publish at 20 Hz ───────────────────────────────────────────────
            _publishTimer += input.DeltaTime;
            if (_publishTimer < _publishInterval) return;
            _publishTimer -= _publishInterval;

            bool shouldPublish = hasInput || _noInputTimer < ZeroStopDelay;
            if (shouldPublish)
            {
                var twist = new TwistMsg(
                    new Vector3Msg(vx, vy, 0f),
                    new Vector3Msg(0f, 0f, omega));
                link.Publish(_profile.Topics.CmdVel, twist);
                _isPublishing = true;
            }
            else if (_isPublishing)
            {
                link.Publish(_profile.Topics.CmdVel, new TwistMsg(
                    new Vector3Msg(0f, 0f, 0f), new Vector3Msg(0f, 0f, 0f)));
                _isPublishing = false;
            }
        }

        public void Stop(IRobotLink link)
        {
            if (!IsConfigured || _profile == null) return;
            // Zero velocity + revert to nav2
            if (link.IsConnected)
            {
                link.Publish(_profile.Topics.CmdVel, new TwistMsg(
                    new Vector3Msg(0f, 0f, 0f), new Vector3Msg(0f, 0f, 0f)));
                if (_inTeleopMode) PublishControlMode("nav2", link);
            }
            _inTeleopMode = false;
            _isPublishing = false;
            _noInputTimer = 0f;
            _noInputModeTimer = 0f;
        }

        private void PublishControlMode(string mode, IRobotLink link)
            => link.Publish(_profile.Topics.ControlMode, new StringMsg(mode));

        private static float ApplyDeadZone(float value)
        {
            if (Mathf.Abs(value) < DeadZone) return 0f;
            float sign = Mathf.Sign(value);
            float scaled = (Mathf.Abs(value) - DeadZone) / (1f - DeadZone);
            return sign * Mathf.Clamp01(scaled);
        }
    }
}
