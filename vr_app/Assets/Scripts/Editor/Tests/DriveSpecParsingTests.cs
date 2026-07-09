using UnityEngine;
using NUnit.Framework;
using OmniBot.VR.Control;
using OmniBot.VR.Control.Drive;

namespace OmniBot.VR.Tests
{
    /// <summary>
    /// Unit tests for <see cref="DriveSpecs"/> — the locomotion-string →
    /// <see cref="DriveKind"/> parser. Mirrors the website's
    /// <c>parseDrive()</c> behavior so the headset derives the same drive kind
    /// the website would from the same catalog data.
    /// </summary>
    public class DriveSpecParsingTests
    {
        [Test]
        public void ParseDrive_MecanumString_ReturnsMecanum()
        {
            Assert.AreEqual(DriveKind.Mecanum,
                DriveSpecs.ParseDrive("Mecanum holonomic", "mobile-manipulator"));
        }

        [Test]
        public void ParseDrive_AckermannString_ReturnsAckermann()
        {
            Assert.AreEqual(DriveKind.Ackermann,
                DriveSpecs.ParseDrive("Ackermann steering", "wheeled"));
        }

        [Test]
        public void ParseDrive_DifferentialString_ReturnsDifferential()
        {
            Assert.AreEqual(DriveKind.Differential,
                DriveSpecs.ParseDrive("differential drive", "wheeled"));
        }

        [Test]
        public void ParseDrive_QuadrotorString_ReturnsQuadrotor()
        {
            Assert.AreEqual(DriveKind.Quadrotor,
                DriveSpecs.ParseDrive("quadrotor", "drones"));
        }

        [Test]
        public void ParseDrive_ThrusterString_ReturnsThruster()
        {
            Assert.AreEqual(DriveKind.Thruster,
                DriveSpecs.ParseDrive("thruster vectoring", "underwater-rov"));
        }

        [Test]
        public void ParseDrive_EmptyString_DronesCategory_FallsBackToQuadrotor()
        {
            Assert.AreEqual(DriveKind.Quadrotor,
                DriveSpecs.ParseDrive("", "drones"));
        }

        [Test]
        public void ParseDrive_EmptyString_IndustrialArmCategory_FallsBackToFixedBase()
        {
            Assert.AreEqual(DriveKind.FixedBase,
                DriveSpecs.ParseDrive("", "industrial-arm"));
        }

        [Test]
        public void ParseDrive_EmptyString_HumanoidCategory_FallsBackToBipedal()
        {
            Assert.AreEqual(DriveKind.Bipedal,
                DriveSpecs.ParseDrive("", "humanoid"));
        }

        [Test]
        public void ParseDrive_EmptyString_UnknownCategory_FallsBackToUnknown()
        {
            Assert.AreEqual(DriveKind.Unknown,
                DriveSpecs.ParseDrive("", "some-random-category"));
        }

        [Test]
        public void Get_Mecanum_HasCorrectBaseDof()
        {
            var spec = DriveSpecs.Get(DriveKind.Mecanum);
            Assert.AreEqual(3, spec.BaseDof);
            Assert.IsTrue(spec.Holonomic);
        }

        [Test]
        public void Get_Differential_HasCorrectBaseDof()
        {
            var spec = DriveSpecs.Get(DriveKind.Differential);
            Assert.AreEqual(2, spec.BaseDof);
            Assert.IsFalse(spec.Holonomic);
        }

        [Test]
        public void Get_FixedBase_HasZeroBaseDof()
        {
            var spec = DriveSpecs.Get(DriveKind.FixedBase);
            Assert.AreEqual(0, spec.BaseDof);
            Assert.AreEqual(0f, spec.MaxLinVel);
        }

        [Test]
        public void Get_Quadrotor_HasFourBaseDof()
        {
            var spec = DriveSpecs.Get(DriveKind.Quadrotor);
            Assert.AreEqual(4, spec.BaseDof);
            Assert.IsTrue(spec.Holonomic);
        }
    }
}
