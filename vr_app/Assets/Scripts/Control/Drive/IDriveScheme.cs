namespace OmniBot.VR.Control.Drive
{
    /// <summary>
    /// Base / locomotion strategy — one implementation per
    /// <see cref="DriveKind"/>. Maps VR controller input → the robot's native
    /// velocity command and publishes it to
    /// <c>RobotProfile.Topics.CmdVel</c>. Selected by
    /// <see cref="ControlSchemeFactory"/> from the profile's drive kind.
    ///
    /// This is the C# counterpart of the website's
    /// <c>DRIVE_SPECS</c> strategy: a drone, a differential base, a quadruped and
    /// a mecanum base each get a correct, distinct mapping from the same
    /// thumbsticks. The scheme owns its own publish-rate timing and control-mode
    /// management so the <see cref="TeleopController"/> stays a thin input pump.
    /// </summary>
    public interface IDriveScheme
    {
        /// <summary>Configure the scheme from the selected robot's profile.</summary>
        void Configure(RobotProfile profile);

        /// <summary>
        /// Advance the scheme one frame: read controller input, manage publish
        /// timing, and publish the velocity command + control-mode string to the
        /// link. Called every Unity frame by <see cref="TeleopController"/>.
        /// </summary>
        void Tick(ref TeleopInput input, IRobotLink link);

        /// <summary>
        /// Immediately zero the robot's velocity and revert control mode. Called
        /// on e-stop, disconnect, or robot switch.
        /// </summary>
        void Stop(IRobotLink link);

        /// <summary>One-line HUD hint, e.g. "Left stick: strafe · Right stick: yaw".</summary>
        string HudHint { get; }

        /// <summary>True once <see cref="Configure"/> has run with a valid profile.</summary>
        bool IsConfigured { get; }
    }
}
