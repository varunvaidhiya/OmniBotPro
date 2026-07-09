using UnityEngine;

namespace OmniBot.VR.Core
{
    public class DirectConnectionWrapper : RobotConnectionInterface
    {
        private bool _connected = false;

        public override bool IsConnected => _connected;

        public override void Connect(string ip, string port)
        {
            Debug.Log($"[DirectConnection] Connecting to Direct API at {ip}:{port}");
            _connected = true;
            OnConnected?.Invoke();
        }

        public override void Disconnect()
        {
            Debug.Log("[DirectConnection] Disconnecting.");
            _connected = false;
            OnDisconnected?.Invoke();
        }

        public override void SendTwist(float linearX, float linearY, float angularZ)
        {
            if (!_connected) return;
            Debug.Log($"[DirectConnection] Direct Move: X={linearX}, Y={linearY}, Z={angularZ}");
        }

        public override void SetArmEnabled(bool enabled)
        {
            if (!_connected) return;
            Debug.Log($"[DirectConnection] Set Arm Enabled: {enabled}");
        }
    }
}
