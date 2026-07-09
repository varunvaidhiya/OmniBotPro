using UnityEngine;
using NUnit.Framework;
using OmniBot.VR.Control;
using OmniBot.VR.Control.Drive;
using OmniBot.VR.Control.Manip;

namespace OmniBot.VR.Tests
{
    /// <summary>
    /// Unit tests for <see cref="ControlSchemeFactory"/> — verifies the factory
    /// picks the correct drive + manipulation scheme for each
    /// <see cref="DriveKind"/> + arm DOF combination.
    /// </summary>
    public class ControlSchemeFactoryTests
    {
        private RobotProfile MakeProfile(DriveKind drive, int armDof = 6, bool hasArm = true)
        {
            return new RobotProfile
            {
                RobotId = "test",
                Name = "Test Robot",
                Drive = drive,
                BaseDof = DriveSpecs.Get(drive).BaseDof,
                MaxLinVel = 0.5f,
                MaxAngVel = 1.0f,
                HasArm = hasArm,
                ArmDof = armDof,
                Joints = hasArm ? new JointSpec[armDof] : System.Array.Empty<JointSpec>(),
                Topics = new RobotTopics { CmdVel = "/cmd_vel/teleop", ControlMode = "/control_mode" },
                HandWorkspaceRadius = 0.40f,
                ArmBaseHeight = 0.35f,
                ArmMaxReach = 0.33f,
            };
        }

        // ── Drive scheme dispatch ────────────────────────────────────────────────

        [Test]
        public void CreateDrive_Mecanum_ReturnsMecanumDriveScheme()
        {
            var scheme = ControlSchemeFactory.CreateDriveScheme(MakeProfile(DriveKind.Mecanum));
            Assert.IsNotNull(scheme);
            Assert.IsInstanceOf<MecanumDriveScheme>(scheme);
            Assert.IsTrue(scheme.IsConfigured);
        }

        [Test]
        public void CreateDrive_Differential_ReturnsDifferentialDriveScheme()
        {
            var scheme = ControlSchemeFactory.CreateDriveScheme(MakeProfile(DriveKind.Differential));
            Assert.IsNotNull(scheme);
            Assert.IsInstanceOf<DifferentialDriveScheme>(scheme);
        }

        [Test]
        public void CreateDrive_Ackermann_ReturnsAckermannDriveScheme()
        {
            var scheme = ControlSchemeFactory.CreateDriveScheme(MakeProfile(DriveKind.Ackermann));
            Assert.IsNotNull(scheme);
            Assert.IsInstanceOf<AckermannDriveScheme>(scheme);
        }

        [Test]
        public void CreateDrive_Quadrotor_ReturnsAerialDriveScheme()
        {
            var scheme = ControlSchemeFactory.CreateDriveScheme(MakeProfile(DriveKind.Quadrotor));
            Assert.IsNotNull(scheme);
            Assert.IsInstanceOf<AerialDriveScheme>(scheme);
        }

        [Test]
        public void CreateDrive_Thruster_ReturnsThrusterDriveScheme()
        {
            var scheme = ControlSchemeFactory.CreateDriveScheme(MakeProfile(DriveKind.Thruster));
            Assert.IsNotNull(scheme);
            Assert.IsInstanceOf<ThrusterDriveScheme>(scheme);
        }

        [Test]
        public void CreateDrive_FixedBase_ReturnsNull()
        {
            var scheme = ControlSchemeFactory.CreateDriveScheme(MakeProfile(DriveKind.FixedBase));
            Assert.IsNull(scheme);
        }

        [Test]
        public void CreateDrive_Quadruped_ReturnsMecanumDriveScheme()
        {
            // Legged robots reuse the mecanum vx/vy/ω mapping (gait gestures = Phase 5+)
            var scheme = ControlSchemeFactory.CreateDriveScheme(MakeProfile(DriveKind.Quadruped));
            Assert.IsNotNull(scheme);
            Assert.IsInstanceOf<MecanumDriveScheme>(scheme);
        }

        // ── Manipulation scheme dispatch ─────────────────────────────────────────

        [Test]
        public void CreateManip_6Dof_ReturnsHandIK6DofScheme()
        {
            var scheme = ControlSchemeFactory.CreateManipulationScheme(MakeProfile(DriveKind.Mecanum, armDof: 6), null);
            Assert.IsNotNull(scheme);
            Assert.IsInstanceOf<HandIK6DofScheme>(scheme);
        }

        [Test]
        public void CreateManip_1Dof_ReturnsGripperOnlyScheme()
        {
            var scheme = ControlSchemeFactory.CreateManipulationScheme(MakeProfile(DriveKind.Mecanum, armDof: 1), null);
            Assert.IsNotNull(scheme);
            Assert.IsInstanceOf<GripperOnlyScheme>(scheme);
        }

        [Test]
        public void CreateManip_14Dof_ReturnsDualArmScheme()
        {
            var scheme = ControlSchemeFactory.CreateManipulationScheme(MakeProfile(DriveKind.Bipedal, armDof: 14), null);
            Assert.IsNotNull(scheme);
            Assert.IsInstanceOf<DualArmManipulationScheme>(scheme);
        }

        [Test]
        public void CreateManip_NoArm_ReturnsNull()
        {
            var scheme = ControlSchemeFactory.CreateManipulationScheme(MakeProfile(DriveKind.Mecanum, armDof: 0, hasArm: false), null);
            Assert.IsNull(scheme);
        }
    }
}
