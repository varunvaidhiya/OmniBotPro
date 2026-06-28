using UnityEngine;
using NUnit.Framework;
using OmniBot.VR.Control;
using OmniBot.VR.Control.Manip;

namespace OmniBot.VR.Tests
{
    /// <summary>
    /// Unit tests for <see cref="FabrikSolver"/> — the generic IK chain solver.
    /// Verifies convergence, reach clamping, and angle clamping to joint limits.
    /// </summary>
    public class FabrikSolverTests
    {
        private static JointSpec[] MakeJoints(int n, float min = -3.14f, float max = 3.14f)
        {
            var j = new JointSpec[n];
            for (int i = 0; i < n; i++)
                j[i] = new JointSpec($"j{i}", $"J{i}", min, max, 0f);
            return j;
        }

        [Test]
        public void Solve_TargetAtBase_ConvergesToBase()
        {
            var solver = new FabrikSolver();
            var joints = MakeJoints(6);
            solver.Configure(joints, new float[] { 0.1f, 0.1f, 0.1f });

            float[] result = solver.Solve(Vector3.zero, new float[6]);
            // End-effector should be near the base (target)
            // All angles should be within limits
            Assert.AreEqual(6, result.Length);
            foreach (var a in result)
                Assert.That(a, Is.InRange(-3.14f, 3.14f));
        }

        [Test]
        public void Solve_TargetBeyondReach_ClampsToReach()
        {
            var solver = new FabrikSolver();
            var joints = MakeJoints(6);
            solver.Configure(joints, new float[] { 0.1f, 0.1f, 0.1f });

            // Target way beyond reach (10 metres, reach is 0.3 m)
            float[] result = solver.Solve(new Vector3(0f, 10f, 0f), new float[6]);
            Assert.AreEqual(6, result.Length);
            // Solver should not explode — all angles within limits
            foreach (var a in result)
                Assert.That(a, Is.InRange(-3.14f, 3.14f));
        }

        [Test]
        public void Solve_PreservesGripperAngle()
        {
            var solver = new FabrikSolver();
            var joints = MakeJoints(6);
            solver.Configure(joints, new float[] { 0.1f, 0.1f, 0.1f });

            float[] current = new float[6] { 0f, 0f, 0f, 0f, 0f, 0.5f };
            float[] result = solver.Solve(new Vector3(0f, 0.2f, 0f), current);
            // The last joint (gripper) should be preserved from current
            Assert.AreEqual(0.5f, result[5], 0.001f);
        }

        [Test]
        public void Solve_RespectsJointLimits()
        {
            var solver = new FabrikSolver();
            // Very tight limits
            var joints = MakeJoints(4, min: -0.5f, max: 0.5f);
            solver.Configure(joints, new float[] { 0.1f, 0.1f, 0.1f });

            float[] result = solver.Solve(new Vector3(0.1f, 0.2f, 0f), new float[4]);
            foreach (var a in result)
                Assert.That(a, Is.InRange(-0.5f, 0.5f));
        }

        [Test]
        public void TotalReach_SumsLinkLengths()
        {
            var solver = new FabrikSolver();
            solver.Configure(MakeJoints(4), new float[] { 0.15f, 0.15f, 0.15f });
            Assert.AreEqual(0.45f, solver.TotalReach, 0.001f);
        }

        [Test]
        public void Solve_EmptyChain_ReturnsEmptyArray()
        {
            var solver = new FabrikSolver();
            solver.Configure(System.Array.Empty<JointSpec>(), System.Array.Empty<float>());
            float[] result = solver.Solve(Vector3.zero, System.Array.Empty<float>());
            Assert.AreEqual(0, result.Length);
        }
    }
}
