using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OmniBot.VR.Core;

namespace OmniBot.VR.UI
{
    /// <summary>
    /// VR control panel providing buttons for:
    ///   - Switching base control mode (Nav2 / Teleop / VLA / RL Nav)
    ///   - Switching arm control mode (SmolVLA / RL Arm)
    ///   - Sending mission commands (text input + predefined shortcuts)
    ///   - Sending AI natural-language commands
    ///   - Emergency stop (large red button)
    ///   - Cancelling the current mission
    /// </summary>
    public class ControlPanel : MonoBehaviour
    {
        // ── Inspector — Mode buttons ──────────────────────────────────────────
        [Header("Base Control Mode")]
        [SerializeField] private Button btnNav2;
        [SerializeField] private Button btnTeleop;
        [SerializeField] private Button btnVla;
        [SerializeField] private Button btnRlNav;

        [Header("Arm Control Mode")]
        [SerializeField] private Button btnSmolVla;
        [SerializeField] private Button btnRlArm;

        // ── Inspector — Mission ───────────────────────────────────────────────
        [Header("Mission Command")]
        [SerializeField] private TMP_InputField missionInput;
        [SerializeField] private Button         btnMissionSend;
        [SerializeField] private Button         btnCancelMission;

        [Header("Mission Shortcuts")]
        [SerializeField] private Button btnKitchen;
        [SerializeField] private Button btnLivingRoom;
        [SerializeField] private Button btnReturnHome;

        // ── Inspector — AI command ────────────────────────────────────────────
        [Header("AI Command")]
        [SerializeField] private TMP_InputField aiCommandInput;
        [SerializeField] private Button         btnAiSend;

        // ── Inspector — Emergency stop ────────────────────────────────────────
        [Header("Emergency Stop")]
        [SerializeField] private Button btnEmergencyStop;

        // ── State ─────────────────────────────────────────────────────────────
        private bool _emergencyStopHeld = false;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Start()
        {
            // Base mode buttons
            if (btnNav2)    btnNav2.onClick.AddListener(()  => SendControlMode("nav2"));
            if (btnTeleop)  btnTeleop.onClick.AddListener(() => SendControlMode("teleop"));
            if (btnVla)     btnVla.onClick.AddListener(()    => SendControlMode("vla"));
            if (btnRlNav)   btnRlNav.onClick.AddListener(()  => SendControlMode("rl_nav"));

            // Arm mode buttons (publish to /arm/cmd_mode)
            if (btnSmolVla) btnSmolVla.onClick.AddListener(() => SendArmMode("smolvla"));
            if (btnRlArm)   btnRlArm.onClick.AddListener(()   => SendArmMode("rl_arm"));

            // Mission
            if (btnMissionSend)   btnMissionSend.onClick.AddListener(OnMissionSend);
            if (btnCancelMission) btnCancelMission.onClick.AddListener(OnCancelMission);

            // Predefined mission shortcuts
            if (btnKitchen)    btnKitchen.onClick.AddListener(()    => SendMissionCommand("navigate:kitchen"));
            if (btnLivingRoom) btnLivingRoom.onClick.AddListener(() => SendMissionCommand("navigate:living_room"));
            if (btnReturnHome) btnReturnHome.onClick.AddListener(() => SendMissionCommand("navigate:home"));

            // AI command
            if (btnAiSend) btnAiSend.onClick.AddListener(OnAiCommandSend);

            // Emergency stop: button down = stop, button up = release
            if (btnEmergencyStop != null)
            {
                // Use EventTrigger for press/release or wire via pointer events
                var et = btnEmergencyStop.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

                var pressEntry = new UnityEngine.EventSystems.EventTrigger.Entry
                    { eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown };
                pressEntry.callback.AddListener(_ => OnEmergencyStopPressed());

                var releaseEntry = new UnityEngine.EventSystems.EventTrigger.Entry
                    { eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp };
                releaseEntry.callback.AddListener(_ => OnEmergencyStopReleased());

                et.triggers.Add(pressEntry);
                et.triggers.Add(releaseEntry);
            }
        }

        // ── Button handlers ──────────────────────────────────────────────────

        private void SendControlMode(string mode)
        {
            if (!ROSBridgeClient.Instance.IsConnected)
            {
                Debug.LogWarning("[ControlPanel] Not connected.");
                return;
            }
            ROSBridgeClient.Instance.Publish(RobotConfig.TOPIC_CONTROL_MODE, new StringMsg(mode));
            Debug.Log($"[ControlPanel] control_mode → {mode}");
        }

        private void SendArmMode(string mode)
        {
            if (!ROSBridgeClient.Instance.IsConnected) return;
            // /arm/cmd_mode is not in RobotConfig topic list so we publish directly
            ROSBridgeClient.Instance.Publish("/arm/cmd_mode", new StringMsg(mode));
            Debug.Log($"[ControlPanel] arm/cmd_mode → {mode}");
        }

        private void OnMissionSend()
        {
            if (missionInput == null) return;
            string cmd = missionInput.text.Trim();
            if (!string.IsNullOrEmpty(cmd))
                SendMissionCommand(cmd);
        }

        private void SendMissionCommand(string command)
        {
            if (!ROSBridgeClient.Instance.IsConnected) return;
            ROSBridgeClient.Instance.Publish(RobotConfig.TOPIC_MISSION_COMMAND, new StringMsg(command));
            Debug.Log($"[ControlPanel] mission_command → {command}");
        }

        private void OnCancelMission()
        {
            if (!ROSBridgeClient.Instance.IsConnected) return;
            // /mission/cancel is a std_msgs/String; send empty string as cancel signal
            ROSBridgeClient.Instance.Publish("/mission/cancel", new StringMsg("cancel"));
            Debug.Log("[ControlPanel] mission cancel sent.");
        }

        private void OnAiCommandSend()
        {
            if (aiCommandInput == null) return;
            string cmd = aiCommandInput.text.Trim();
            if (string.IsNullOrEmpty(cmd)) return;
            if (!ROSBridgeClient.Instance.IsConnected) return;
            ROSBridgeClient.Instance.Publish(RobotConfig.TOPIC_AI_COMMAND, new StringMsg(cmd));
            aiCommandInput.text = "";
            Debug.Log($"[ControlPanel] ai/command → {cmd}");
        }

        private void OnEmergencyStopPressed()
        {
            if (!ROSBridgeClient.Instance.IsConnected) return;
            _emergencyStopHeld = true;
            ROSBridgeClient.Instance.Publish(RobotConfig.TOPIC_EMERGENCY_STOP, new BoolMsg(true));
            Debug.LogWarning("[ControlPanel] EMERGENCY STOP pressed.");
        }

        private void OnEmergencyStopReleased()
        {
            if (!ROSBridgeClient.Instance.IsConnected || !_emergencyStopHeld) return;
            _emergencyStopHeld = false;
            ROSBridgeClient.Instance.Publish(RobotConfig.TOPIC_EMERGENCY_STOP, new BoolMsg(false));
            Debug.Log("[ControlPanel] Emergency stop released.");
        }
    }
}
