using UnityEngine;

namespace OmniBot.VR.Core
{
    public class ROSConnectionWrapper : RobotConnectionInterface
    {
        private ROSBridgeClient _ros;

        private void Awake()
        {
            _ros = GetComponent<ROSBridgeClient>();
            if (_ros == null) _ros = gameObject.AddComponent<ROSBridgeClient>();
        }

        public override bool IsConnected => _ros != null && _ros.IsConnected;

        public override void Connect(string ip, string port)
        {
            // Assuming ROSBridgeClient has Connect(string wsUrl)
            string url = $"ws://{ip}:{port}";
            Debug.Log($"[ROSConnection] Connecting to {url}");
            
            // NOTE: We wrap the standard implementation here. 
            // In a full codebase, we'd hook up actual ROSBridgeClient methods.
            OnConnected?.Invoke();
        }

        public override void Disconnect()
        {
            Debug.Log("[ROSConnection] Disconnecting.");
            OnDisconnected?.Invoke();
        }

        public override void SendTwist(float linearX, float linearY, float angularZ)
        {
            Debug.Log($"[ROSConnection] SendTwist: {linearX}, {linearY}, {angularZ}");
        }

        public override void SetArmEnabled(bool enabled)
        {
            Debug.Log($"[ROSConnection] SetArmEnabled: {enabled}");
        }
    }
}
