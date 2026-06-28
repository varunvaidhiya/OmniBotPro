using UnityEngine;
using OmniBot.VR.Core;

namespace OmniBot.VR.Control.Manip
{
    /// <summary>
    /// Gripper-only manipulation — for robots that have a single-DOF gripper but
    /// no arm joints to IK (e.g. a simple pick-and-place AMR, or a drone with a
    /// servo gripper). The right-hand pinch strength directly drives the gripper
    /// joint; no IK is solved. Publishes a <see cref="JointStateMsg"/> with just
    /// the gripper position at 20 Hz.
    /// </summary>
    public sealed class GripperOnlyScheme : IManipulationScheme
    {
        private const float PublishHz = 20f;

        private RobotProfile _profile;
        private Transform _workspaceOrigin;
        private float _publishInterval;
        private float _publishTimer;
        private bool _armEnabled;
        private string[] _jointNames;
        private float _gripperOpen;
        private float _gripperClosed;

        public bool IsConfigured => _profile != null;
        public bool ArmEnabled => _armEnabled;
        public string HudHint => "Pinch: gripper open/close · Left thumbs-up (hold): toggle";

        public void Configure(RobotProfile profile, Transform workspaceOrigin)
        {
            _profile = profile;
            _workspaceOrigin = workspaceOrigin;
            _publishInterval = 1f / PublishHz;
            _publishTimer = 0f;
            _armEnabled = false;

            // The single joint is the gripper (last joint in the profile).
            if (profile.Joints != null && profile.Joints.Length > 0)
            {
                var grip = profile.Joints[profile.Joints.Length - 1];
                _jointNames = new[] { grip.Name };
                _gripperOpen = grip.Max;
                _gripperClosed = grip.Min;
            }
            else
            {
                _jointNames = new[] { "gripper" };
                _gripperOpen = 0.8f;
                _gripperClosed = 0f;
            }
        }

        public bool Tick(ref HandPose rightHand, ref HandPose leftHand, IRobotLink link)
        {
            if (!IsConfigured || _profile == null || !link.IsConnected) return false;
            if (!_armEnabled || !rightHand.Tracked) return false;

            _publishTimer += Time.deltaTime;
            if (_publishTimer < _publishInterval) return false;
            _publishTimer -= _publishInterval;

            // Pinch → gripper: 0 = open, 1 = closed
            float gripper = Mathf.Lerp(_gripperOpen, _gripperClosed, Mathf.Clamp01(rightHand.PinchStrength));

            var msg = new JointStateMsg((string[])_jointNames.Clone(), new[] { gripper });
            link.Publish(_profile.Topics.ArmCommands, msg);
            return true;
        }

        public void Stop(IRobotLink link)
        {
            _armEnabled = false;
            _publishTimer = 0f;
        }

        public void SetArmEnabled(bool enabled, IRobotLink link)
        {
            _armEnabled = enabled;
            if (_profile != null && link != null && link.IsConnected && !string.IsNullOrEmpty(_profile.Topics.ArmEnable))
                link.Publish(_profile.Topics.ArmEnable, new BoolMsg(enabled));
            Debug.Log($"[GripperOnlyScheme] Gripper {(enabled ? "enabled" : "disabled")}");
        }
    }
}
