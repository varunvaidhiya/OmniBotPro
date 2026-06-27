using System.Collections.Generic;
using UnityEngine;

namespace OmniBot.VR.Control
{
    /// <summary>
    /// Per-<see cref="DriveKind"/> capability table — a 1:1 mirror of
    /// <c>website/lib/garage/robot-config.ts</c> <c>DRIVE_SPECS</c>. Tells the
    /// drive scheme how many base DOF the robot has, what its action vector means,
    /// whether it can strafe, and a safe default top speed. The
    /// <see cref="RobotProfile"/> carries the resolved values to the schemes.
    /// </summary>
    public struct DriveSpec
    {
        public string Label;
        public int BaseDof;
        public string[] ActionLabels;
        public bool Holonomic;
        public float MaxLinVel; // m/s
        public float MaxAngVel; // rad/s

        public DriveSpec(string label, int baseDof, string[] actionLabels,
                         bool holonomic, float maxLinVel, float maxAngVel)
        {
            Label = label;
            BaseDof = baseDof;
            ActionLabels = actionLabels;
            Holonomic = holonomic;
            MaxLinVel = maxLinVel;
            MaxAngVel = maxAngVel;
        }
    }

    /// <summary>
    /// Static lookup + <c>locomotion</c> string parser. Mirrors
    /// <c>parseDrive()</c> + <c>DRIVE_SPECS</c> from the website so the headset
    /// derives the same <see cref="DriveKind"/> the website would, offline, from
    /// the catalog JSON it already fetched.
    /// </summary>
    public static class DriveSpecs
    {
        private static readonly Dictionary<DriveKind, DriveSpec> Table =
            new Dictionary<DriveKind, DriveSpec>
            {
                { DriveKind.Mecanum,      new DriveSpec("Mecanum (holonomic)", 3, new[] {"vx","vy","ω"}, true,  0.5f, 1.5f) },
                { DriveKind.Differential, new DriveSpec("Differential drive",  2, new[] {"v","ω"},      false, 0.6f, 2.0f) },
                { DriveKind.Ackermann,    new DriveSpec("Ackermann steering",  2, new[] {"v","δ"},      false, 2.0f, 1.0f) },
                { DriveKind.SkidSteer,    new DriveSpec("Skid-steer",          2, new[] {"v","ω"},      false, 1.5f, 1.5f) },
                { DriveKind.Tracked,      new DriveSpec("Tracked",             2, new[] {"v","ω"},      false, 1.5f, 1.0f) },
                { DriveKind.RockerBogie,  new DriveSpec("Rocker-bogie (6-wheel)", 2, new[] {"v","ω"},  false, 0.3f, 0.5f) },
                { DriveKind.Gantry,       new DriveSpec("Cartesian gantry",    3, new[] {"x","y","z"},  true,  0.2f, 0f) },
                { DriveKind.Quadrotor,    new DriveSpec("Quadrotor",           4, new[] {"vx","vy","vz","ω"}, true, 12f, 3.0f) },
                { DriveKind.Hexacopter,   new DriveSpec("Hexacopter",          4, new[] {"vx","vy","vz","ω"}, true, 10f, 2.5f) },
                { DriveKind.Vtol,         new DriveSpec("VTOL fixed-wing",     4, new[] {"vx","vy","vz","ω"}, true, 20f, 1.5f) },
                { DriveKind.FixedWing,    new DriveSpec("Fixed-wing",          4, new[] {"thr","roll","pitch","yaw"}, false, 25f, 1.0f) },
                { DriveKind.Quadruped,    new DriveSpec("Quadruped gait",      3, new[] {"vx","vy","ω"}, true,  3.0f, 2.0f) },
                { DriveKind.Hexapod,      new DriveSpec("Hexapod gait",        3, new[] {"vx","vy","ω"}, true,  0.4f, 0.8f) },
                { DriveKind.Bipedal,      new DriveSpec("Bipedal gait",        3, new[] {"vx","vy","ω"}, true,  1.4f, 1.0f) },
                { DriveKind.Thruster,     new DriveSpec("Thruster vectoring",  4, new[] {"surge","sway","heave","yaw"}, true, 1.5f, 1.0f) },
                { DriveKind.FixedBase,    new DriveSpec("Fixed base",          0, new string[0],        false, 0f,   0f) },
                { DriveKind.Wearable,     new DriveSpec("Wearable",            0, new string[0],        false, 0f,   0f) },
                { DriveKind.Unknown,      new DriveSpec("Mobile base",         2, new[] {"v","ω"},      false, 0.5f, 1.0f) },
            };

        /// <summary>Resolve the spec for a drive kind (always succeeds — Unknown is the fallback).</summary>
        public static DriveSpec Get(DriveKind kind) => Table[kind];

        /// <summary>
        /// Map a catalog model's free-text <c>locomotion</c> string (+ category
        /// fallback) to a <see cref="DriveKind"/>. Mirrors <c>parseDrive()</c>
        /// from the website.
        /// </summary>
        public static DriveKind ParseDrive(string locomotion, string categoryId)
        {
            string l = (locomotion ?? "").ToLowerInvariant();

            if (l.Contains("mecanum"))     return DriveKind.Mecanum;
            if (l.Contains("ackermann"))   return DriveKind.Ackermann;
            if (l.Contains("skid"))        return DriveKind.SkidSteer;
            if (l.Contains("rocker"))      return DriveKind.RockerBogie;
            if (l.Contains("gantry") || l.Contains("cartesian")) return DriveKind.Gantry;
            if (l.Contains("hexacopter"))  return DriveKind.Hexacopter;
            if (l.Contains("vtol"))        return DriveKind.Vtol;
            if (l.Contains("fixed-wing") || l.Contains("fixed_wing")) return DriveKind.FixedWing;
            if (l.Contains("quadrotor") || l.Contains("quadcopter")) return DriveKind.Quadrotor;
            if (l.Contains("quadruped"))   return DriveKind.Quadruped;
            if (l.Contains("hexapod"))     return DriveKind.Hexapod;
            if (l.Contains("biped"))       return DriveKind.Bipedal;
            if (l.Contains("thruster") || l.Contains("marine") || l.Contains("underwater")) return DriveKind.Thruster;
            if (l.Contains("tracked") || l.Contains("track")) return DriveKind.Tracked;
            if (l.Contains("wearable"))    return DriveKind.Wearable;
            if (l.Contains("fixed-base") || l.Contains("fixed_base")) return DriveKind.FixedBase;
            if (l.Contains("differential") || l.Contains("diff")) return DriveKind.Differential;

            // Category fallbacks for sparse/odd locomotion strings
            switch (categoryId)
            {
                case "drones":          return DriveKind.Quadrotor;
                case "industrial-arm":  return DriveKind.FixedBase;
                case "humanoid":        return DriveKind.Bipedal;
                case "legged":          return DriveKind.Quadruped;
                case "marine":
                case "underwater-rov":  return DriveKind.Thruster;
                case "space":           return DriveKind.RockerBogie;
                case "wheeled":
                case "delivery":        return DriveKind.Differential;
                default:                return DriveKind.Unknown;
            }
        }
    }
}
