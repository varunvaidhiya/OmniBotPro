namespace OmniBot.VR.Control
{
    /// <summary>
    /// Every locomotion strategy the OhhO catalog distinguishes — a 1:1 mirror of
    /// <c>website/lib/garage/robot-config.ts</c> <c>DriveKind</c>. The
    /// <see cref="DriveSpec"/> table and <see cref="RobotProfileFactory"/> turn a
    /// catalog model's free-text <c>locomotion</c> into one of these, and
    /// <see cref="ControlSchemeFactory"/> picks a drive scheme from it.
    /// </summary>
    public enum DriveKind
    {
        Mecanum,
        Differential,
        Ackermann,
        SkidSteer,
        Tracked,
        RockerBogie,
        Gantry,
        Quadrotor,
        Hexacopter,
        Vtol,
        FixedWing,
        Quadruped,
        Hexapod,
        Bipedal,
        Thruster,
        FixedBase,
        Wearable,
        Unknown,
    }

    /// <summary>String identifiers used in JSON / the catalog, matching the TS union.</summary>
    public static class DriveKindIds
    {
        public const string Mecanum      = "mecanum";
        public const string Differential = "differential";
        public const string Ackermann    = "ackermann";
        public const string SkidSteer    = "skid-steer";
        public const string Tracked      = "tracked";
        public const string RockerBogie  = "rocker-bogie";
        public const string Gantry       = "gantry";
        public const string Quadrotor    = "quadrotor";
        public const string Hexacopter   = "hexacopter";
        public const string Vtol         = "vtol";
        public const string FixedWing    = "fixed-wing";
        public const string Quadruped    = "quadruped";
        public const string Hexapod      = "hexapod";
        public const string Bipedal      = "bipedal";
        public const string Thruster     = "thruster";
        public const string FixedBase    = "fixed-base";
        public const string Wearable     = "wearable";
        public const string Unknown      = "unknown";
    }
}
