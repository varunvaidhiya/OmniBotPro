using UnityEngine;
using OmniBot.VR.Core; // Assuming ROSBridgeClient is here

namespace OmniBot.VR.Core
{
    /// <summary>
    /// Abstract interface allowing the UI to connect, disconnect, and send commands
    /// to the robot either via ROSBridge (ROS 2) or a direct API (Non-ROS).
    /// </summary>
    public abstract class RobotConnectionInterface : MonoBehaviour
    {
        public abstract bool IsConnected { get; }
        public abstract void Connect(string ip, string port);
        public abstract void Disconnect();
        
        // Robot controls
        public abstract void SendTwist(float linearX, float linearY, float angularZ);
        public abstract void SetArmEnabled(bool enabled);
        
        // Event actions
        public System.Action OnConnected;
        public System.Action OnDisconnected;
        public System.Action<string> OnError;
    }
}
