using UnityEngine;
using NUnit.Framework;
using OmniBot.VR.Control;

namespace OmniBot.VR.Tests
{
    /// <summary>
    /// Unit tests for <see cref="CalibrationManager"/> — per-robot calibration
    /// persistence and profile application. Uses a known robot id and verifies
    /// that overrides take effect on the profile.
    /// </summary>
    public class CalibrationManagerTests
    {
        private const string TestRobotId = "test-calibration-robot";

        [SetUp]
        public void SetUp()
        {
            CalibrationManager.Reset(TestRobotId);
        }

        [TearDown]
        public void TearDown()
        {
            CalibrationManager.Reset(TestRobotId);
        }

        [Test]
        public void Load_NoSavedCalibration_ReturnsDefaults()
        {
            var cal = CalibrationManager.Load(TestRobotId);
            Assert.AreEqual(0f, cal.armBaseHeightOffset);
            Assert.AreEqual(1.0f, cal.workspaceRadiusScale);
            Assert.AreEqual(0f, cal.maxLinVelOverride);
            Assert.AreEqual(-1, cal.ikLocationOverride);
        }

        [Test]
        public void Save_ThenLoad_RoundTripsValues()
        {
            var cal = new RobotCalibration
            {
                armBaseHeightOffset = 0.05f,
                workspaceRadiusScale = 1.2f,
                maxLinVelOverride = 0.3f,
                maxAngVelOverride = 1.5f,
                ikLocationOverride = 1,
            };
            CalibrationManager.Save(TestRobotId, cal);

            var loaded = CalibrationManager.Load(TestRobotId);
            Assert.AreEqual(0.05f, loaded.armBaseHeightOffset, 0.001f);
            Assert.AreEqual(1.2f, loaded.workspaceRadiusScale, 0.001f);
            Assert.AreEqual(0.3f, loaded.maxLinVelOverride, 0.001f);
            Assert.AreEqual(1.5f, loaded.maxAngVelOverride, 0.001f);
            Assert.AreEqual(1, loaded.ikLocationOverride);
        }

        [Test]
        public void ApplyTo_ArmBaseHeight_AddsOffset()
        {
            var profile = new RobotProfile { ArmBaseHeight = 0.35f };
            var cal = new RobotCalibration { armBaseHeightOffset = 0.10f };
            CalibrationManager.ApplyTo(profile, cal);
            Assert.AreEqual(0.45f, profile.ArmBaseHeight, 0.001f);
        }

        [Test]
        public void ApplyTo_WorkspaceRadius_Scales()
        {
            var profile = new RobotProfile { HandWorkspaceRadius = 0.40f };
            var cal = new RobotCalibration { workspaceRadiusScale = 1.5f };
            CalibrationManager.ApplyTo(profile, cal);
            Assert.AreEqual(0.60f, profile.HandWorkspaceRadius, 0.001f);
        }

        [Test]
        public void ApplyTo_VelocityOverride_WhenPositive_Overrides()
        {
            var profile = new RobotProfile { MaxLinVel = 0.2f, MaxAngVel = 1.0f };
            var cal = new RobotCalibration { maxLinVelOverride = 0.5f, maxAngVelOverride = 2.0f };
            CalibrationManager.ApplyTo(profile, cal);
            Assert.AreEqual(0.5f, profile.MaxLinVel);
            Assert.AreEqual(2.0f, profile.MaxAngVel);
        }

        [Test]
        public void ApplyTo_VelocityOverride_WhenZero_KeepsDefault()
        {
            var profile = new RobotProfile { MaxLinVel = 0.2f, MaxAngVel = 1.0f };
            var cal = new RobotCalibration { maxLinVelOverride = 0f, maxAngVelOverride = 0f };
            CalibrationManager.ApplyTo(profile, cal);
            Assert.AreEqual(0.2f, profile.MaxLinVel);
            Assert.AreEqual(1.0f, profile.MaxAngVel);
        }

        [Test]
        public void ApplyTo_IkLocation_OverridesToRobot()
        {
            var profile = new RobotProfile { IkLocation = IkLocation.Headset };
            var cal = new RobotCalibration { ikLocationOverride = 1 };
            CalibrationManager.ApplyTo(profile, cal);
            Assert.AreEqual(IkLocation.Robot, profile.IkLocation);
        }

        [Test]
        public void ApplyTo_NullProfile_DoesNothing()
        {
            // Should not throw
            Assert.DoesNotThrow(() => CalibrationManager.ApplyTo(null, new RobotCalibration()));
        }

        [Test]
        public void Reset_ClearsCalibration()
        {
            CalibrationManager.Save(TestRobotId, new RobotCalibration { armBaseHeightOffset = 0.5f });
            CalibrationManager.Reset(TestRobotId);
            var loaded = CalibrationManager.Load(TestRobotId);
            Assert.AreEqual(0f, loaded.armBaseHeightOffset);
        }
    }
}
