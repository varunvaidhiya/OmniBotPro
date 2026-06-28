using System;
using Newtonsoft.Json;
using UnityEngine;

namespace OmniBot.VR.Control
{
    /// <summary>
    /// Per-robot calibration data — adjustments the user makes in-headset to
    /// align the virtual arm workspace with the real robot's mounting position.
    /// Each robot in the garage gets its own calibration (the OmniBot on the
    /// desk has a different arm-base height than the UR5e on its stand).
    ///
    /// Persisted in <c>PlayerPrefs</c> as JSON keyed by <c>robotId</c>. Applied
    /// by <see cref="CalibrationManager.ApplyTo"/> over the
    /// <see cref="RobotProfile"/> before the schemes are built.
    /// </summary>
    [Serializable]
    public class RobotCalibration
    {
        /// <summary>Offset added to the profile's arm base height, metres.</summary>
        public float armBaseHeightOffset;

        /// <summary>Multiplier on the hand workspace radius (1.0 = default).</summary>
        public float workspaceRadiusScale = 1.0f;

        /// <summary>Offset added to the workspace origin position (world space).</summary>
        public Vector3Ser workspaceOriginOffset;

        /// <summary>Max linear velocity override (0 = use profile default).</summary>
        public float maxLinVelOverride;

        /// <summary>Max angular velocity override (0 = use profile default).</summary>
        public float maxAngVelOverride;

        /// <summary>IK location override (-1 = use profile default, 0 = headset, 1 = robot).</summary>
        public int ikLocationOverride = -1;
    }

    /// <summary>Serializable Vector3 for JSON persistence (Unity Vector3 isn't JSON-serializable by default).</summary>
    [Serializable]
    public struct Vector3Ser
    {
        public float x, y, z;
        public Vector3Ser(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static implicit operator Vector3(Vector3Ser v) => new Vector3(v.x, v.y, v.z);
        public static implicit operator Vector3Ser(Vector3 v) => new Vector3Ser(v.x, v.y, v.z);
    }

    /// <summary>
    /// Load/save/apply per-robot calibration. The calibration is pure data
    /// (no MonoBehaviour) so it can be unit-tested; the manager is a thin
    /// PlayerPrefs wrapper.
    /// </summary>
    public static class CalibrationManager
    {
        private const string KeyPrefix = "ohho.calibration.";

        /// <summary>Load the calibration for a robot id (or return defaults).</summary>
        public static RobotCalibration Load(string robotId)
        {
            if (string.IsNullOrEmpty(robotId)) return new RobotCalibration();
            string key = KeyPrefix + robotId;
            if (!PlayerPrefs.HasKey(key)) return new RobotCalibration();
            try
            {
                var json = PlayerPrefs.GetString(key, null);
                return JsonConvert.DeserializeObject<RobotCalibration>(json) ?? new RobotCalibration();
            }
            catch { return new RobotCalibration(); }
        }

        /// <summary>Save the calibration for a robot id.</summary>
        public static void Save(string robotId, RobotCalibration cal)
        {
            if (string.IsNullOrEmpty(robotId)) return;
            PlayerPrefs.SetString(KeyPrefix + robotId, JsonConvert.SerializeObject(cal));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Apply the calibration overrides to a profile (mutates the profile in
        /// place). Called after <see cref="RobotProfileFactory.FromGarageRobot"/>
        /// and before <see cref="ControlSchemeFactory"/> builds the schemes.
        /// </summary>
        public static void ApplyTo(RobotProfile profile, RobotCalibration cal)
        {
            if (profile == null || cal == null) return;

            profile.ArmBaseHeight += cal.armBaseHeightOffset;
            profile.HandWorkspaceRadius *= cal.workspaceRadiusScale;
            if (cal.maxLinVelOverride > 0f) profile.MaxLinVel = cal.maxLinVelOverride;
            if (cal.maxAngVelOverride > 0f) profile.MaxAngVel = cal.maxAngVelOverride;
            if (cal.ikLocationOverride == 0) profile.IkLocation = IkLocation.Headset;
            else if (cal.ikLocationOverride == 1) profile.IkLocation = IkLocation.Robot;
        }

        /// <summary>Reset calibration for a robot id to defaults.</summary>
        public static void Reset(string robotId)
        {
            if (string.IsNullOrEmpty(robotId)) return;
            PlayerPrefs.DeleteKey(KeyPrefix + robotId);
            PlayerPrefs.Save();
        }
    }
}
