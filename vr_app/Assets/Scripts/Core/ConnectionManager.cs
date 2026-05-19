using System;
using System.Collections.Generic;
using UnityEngine;

namespace OmniBot.VR.Core
{
    /// <summary>
    /// Orchestrates the ROSBridge connection lifecycle: reads/writes PlayerPrefs,
    /// advertises all publish topics, and subscribes to all receive topics after
    /// a successful connection.
    /// </summary>
    public class ConnectionManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        private static ConnectionManager _instance;
        public static ConnectionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("ConnectionManager");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<ConnectionManager>();
                }
                return _instance;
            }
        }

        // ── PlayerPrefs keys ─────────────────────────────────────────────────
        private const string PrefKeyIp = "robot_ip";
        private const string PrefKeyPort = "robot_port";
        private const string PrefKeyAutoConnect = "auto_connect";

        // ── Public state ─────────────────────────────────────────────────────
        public bool IsConnected => ROSBridgeClient.Instance.IsConnected;
        public string CurrentIp { get; private set; }
        public int CurrentPort { get; private set; }

        /// <summary>Fires on state changes: true = connected, false = disconnected.</summary>
        public event Action<bool> OnConnectionStateChanged;

        /// <summary>Fires when the robot IP changes (e.g. after a successful connect).</summary>
        public event Action<string> OnRobotIpChanged;

        // ── Topic → ROS type registry ────────────────────────────────────────
        // Publisher topic → ROS type string
        private static readonly Dictionary<string, string> PublishTopics = new Dictionary<string, string>
        {
            { RobotConfig.TOPIC_CMD_VEL,         "geometry_msgs/Twist"       },
            { RobotConfig.TOPIC_CONTROL_MODE,    "std_msgs/String"            },
            { RobotConfig.TOPIC_ARM_COMMANDS,    "sensor_msgs/JointState"     },
            { RobotConfig.TOPIC_ARM_ENABLE,      "std_msgs/Bool"              },
            { RobotConfig.TOPIC_EMERGENCY_STOP,  "std_msgs/Bool"              },
            { RobotConfig.TOPIC_MISSION_COMMAND, "std_msgs/String"            },
            { RobotConfig.TOPIC_AI_COMMAND,      "std_msgs/String"            },
            { RobotConfig.TOPIC_VR_RECORD_START, "std_msgs/String"            },
            { RobotConfig.TOPIC_VR_RECORD_STOP,  "std_msgs/Bool"              },
        };

        // Subscriber topic → ROS type string
        private static readonly Dictionary<string, string> SubscribeTopics = new Dictionary<string, string>
        {
            { RobotConfig.TOPIC_ODOM,                "nav_msgs/Odometry"      },
            { RobotConfig.TOPIC_IMU,                 "sensor_msgs/Imu"        },
            { RobotConfig.TOPIC_ARM_STATES,          "sensor_msgs/JointState" },
            { RobotConfig.TOPIC_MISSION_STATUS,      "std_msgs/String"        },
            { RobotConfig.TOPIC_AI_STATUS,           "std_msgs/String"        },
            { RobotConfig.TOPIC_AI_RESPONSE_NEEDED,  "std_msgs/String"        },
            { RobotConfig.TOPIC_CONTROL_MODE_ACTIVE, "std_msgs/String"        },
            { RobotConfig.TOPIC_ARM_MODE_ACTIVE,     "std_msgs/String"        },
        };

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            CurrentIp = PlayerPrefs.GetString(PrefKeyIp, "192.168.1.101");
            CurrentPort = PlayerPrefs.GetInt(PrefKeyPort, 9090);
        }

        private void Start()
        {
            // Wire ROSBridgeClient events
            ROSBridgeClient.Instance.OnConnected += HandleConnected;
            ROSBridgeClient.Instance.OnDisconnected += HandleDisconnected;
            ROSBridgeClient.Instance.OnError += HandleError;

            // Auto-connect if preference set
            if (PlayerPrefs.GetInt(PrefKeyAutoConnect, 0) == 1)
            {
                Debug.Log("[ConnectionManager] Auto-connect enabled, connecting...");
                Connect(CurrentIp, CurrentPort);
            }
        }

        private void OnDestroy()
        {
            if (ROSBridgeClient.Instance != null)
            {
                ROSBridgeClient.Instance.OnConnected -= HandleConnected;
                ROSBridgeClient.Instance.OnDisconnected -= HandleDisconnected;
                ROSBridgeClient.Instance.OnError -= HandleError;
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Connects to the robot at the given IP and port.</summary>
        public void Connect(string ip, int port)
        {
            CurrentIp = ip;
            CurrentPort = port;
            string url = $"ws://{ip}:{port}";
            Debug.Log($"[ConnectionManager] Connecting to {url}");
            ROSBridgeClient.Instance.Connect(url);
        }

        /// <summary>Unsubscribes from all topics, then disconnects cleanly.</summary>
        public void Disconnect()
        {
            foreach (var topic in SubscribeTopics.Keys)
            {
                ROSBridgeClient.Instance.Unsubscribe(topic);
            }
            ROSBridgeClient.Instance.Disconnect();
        }

        /// <summary>Saves auto-connect preference to PlayerPrefs.</summary>
        public void SetAutoConnect(bool enabled)
        {
            PlayerPrefs.SetInt(PrefKeyAutoConnect, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Returns the current auto-connect preference.</summary>
        public bool GetAutoConnect() => PlayerPrefs.GetInt(PrefKeyAutoConnect, 0) == 1;

        // ── Event handlers ───────────────────────────────────────────────────

        private void HandleConnected()
        {
            Debug.Log("[ConnectionManager] Connected — advertising and subscribing.");

            // Save last-known connection details
            PlayerPrefs.SetString(PrefKeyIp, CurrentIp);
            PlayerPrefs.SetInt(PrefKeyPort, CurrentPort);
            PlayerPrefs.Save();

            // Advertise all publish topics
            foreach (var kvp in PublishTopics)
            {
                ROSBridgeClient.Instance.Advertise(kvp.Key, kvp.Value);
            }

            // Subscribe to all receive topics (callbacks registered elsewhere)
            foreach (var kvp in SubscribeTopics)
            {
                // Only send the subscribe op if not already registered
                // (ROSBridgeClient.Subscribe handles deduplication)
                ROSBridgeClient.Instance.Subscribe(kvp.Key, kvp.Value, _ => { /* placeholder; actual handlers added by feature scripts */ });
            }

            OnConnectionStateChanged?.Invoke(true);
            OnRobotIpChanged?.Invoke(CurrentIp);
        }

        private void HandleDisconnected()
        {
            Debug.Log("[ConnectionManager] Disconnected.");
            OnConnectionStateChanged?.Invoke(false);
        }

        private void HandleError(string error)
        {
            Debug.LogError($"[ConnectionManager] ROSBridge error: {error}");
        }
    }
}
