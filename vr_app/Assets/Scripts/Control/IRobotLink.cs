using System;
using OmniBot.VR.Core;

namespace OmniBot.VR.Control
{
    /// <summary>
    /// The robot-side transport the teleop control layer talks to — a thin
    /// interface so schemes can be unit-tested against a fake link, and so a
    /// future non-ROS bridge (WebRTC, serial) can slot in without touching the
    /// schemes. Mirrors <c>website/lib/connect/types.ts</c>
    /// <c>RobotTransport</c>.
    ///
    /// Today's only implementation is <see cref="RosBridgeLink"/>, which wraps the
    /// existing <see cref="ROSBridgeClient"/> singleton.
    /// </summary>
    public interface IRobotLink
    {
        bool IsConnected { get; }

        /// <summary>Advertise a publisher topic so ROSBridge creates the ROS publisher.</summary>
        void Advertise(string topic, string type);

        /// <summary>Publish a typed message to a topic.</summary>
        void Publish<T>(string topic, T msg);

        /// <summary>Subscribe to a topic; the callback receives the raw msg JSON on the main thread.</summary>
        void Subscribe(string topic, string type, Action<string> callback);

        /// <summary>Unsubscribe all callbacks for a topic.</summary>
        void Unsubscribe(string topic);
    }

    /// <summary>
    /// <see cref="IRobotLink"/> backed by the existing
    /// <see cref="ROSBridgeClient"/> WebSocket singleton. The schemes never touch
    /// <c>ROSBridgeClient</c> directly — they go through this link so the robot
    /// transport is swappable and testable. Delegates every call straight through.
    /// </summary>
    public sealed class RosBridgeLink : IRobotLink
    {
        public static readonly RosBridgeLink Instance = new RosBridgeLink();

        public bool IsConnected => ROSBridgeClient.Instance != null && ROSBridgeClient.Instance.IsConnected;

        private RosBridgeLink() { }

        public void Advertise(string topic, string type)
            => ROSBridgeClient.Instance.Advertise(topic, type);

        public void Publish<T>(string topic, T msg)
            => ROSBridgeClient.Instance.Publish(topic, msg);

        public void Subscribe(string topic, string type, Action<string> callback)
            => ROSBridgeClient.Instance.Subscribe(topic, type, callback);

        public void Unsubscribe(string topic)
            => ROSBridgeClient.Instance.Unsubscribe(topic);
    }
}
