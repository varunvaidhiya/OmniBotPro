using UnityEngine;

namespace OmniBot.VR.Control
{
    /// <summary>
    /// A snapshot of the VR controller state fed to <see cref="Drive.IDriveScheme"/>
    /// each frame. Filled by <see cref="TeleopController"/> from OVRInput so the
    /// drive schemes stay testable (no OVR dependency inside the scheme itself).
    /// </summary>
    public struct TeleopInput
    {
        /// <summary>Left thumbstick, range [-1, 1] on each axis after dead-zone.</summary>
        public Vector2 LeftStick;

        /// <summary>Right thumbstick, range [-1, 1] on each axis after dead-zone.</summary>
        public Vector2 RightStick;

        /// <summary>Left grip held this frame.</summary>
        public bool LeftGrip;

        /// <summary>Right grip held this frame.</summary>
        public bool RightGrip;

        /// <summary>Both grips held — used for the emergency-stop deadman.</summary>
        public bool BothGrips;

        /// <summary>Rising edge of both-grips this frame (activates e-stop).</summary>
        public bool BothGripsPressed;

        /// <summary>Falling edge of both-grips this frame (releases e-stop).</summary>
        public bool BothGripsReleased;

        /// <summary>Seconds since the previous frame.</summary>
        public float DeltaTime;
    }

    /// <summary>
    /// A snapshot of one tracked hand, fed to
    /// <see cref="Manip.IManipulationScheme"/>. Position/rotation are in world
    /// space; <see cref="TeleopController"/> resolves them relative to the arm
    /// workspace origin inside the scheme.
    /// </summary>
    public struct HandPose
    {
        public bool Tracked;
        public Vector3 Position;
        public Quaternion Rotation;
        public float PinchStrength; // [0..1]
    }
}
