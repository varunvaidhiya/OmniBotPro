using UnityEngine;

namespace OmniBot.VR.Control
{
    /// <summary>
    /// Persists the last-selected robot id + connection details so a returning
    /// user can quick-resume teleoperation without re-browsing the garage. The
    /// "account link" polish: the headset remembers which robot you drove last
    /// and offers a one-tap resume on the next session.
    ///
    /// Stored in <c>PlayerPrefs</c> as plain keys (no secrets). The robot id is
    /// the catalog <c>hardware_model_id</c>; the IP/port are the ROSBridge
    /// endpoint the user was connected to.
    /// </summary>
    public static class LastRobotStore
    {
        private const string KeyRobotId = "ohho.last_robot_id";
        private const string KeyRobotName = "ohho.last_robot_name";
        private const string KeyRobotIp = "ohho.last_robot_ip";
        private const string KeyRobotPort = "ohho.last_robot_port";
        private const string KeyHasLast = "ohho.has_last_robot";

        /// <summary>True if a previous teleop session was recorded.</summary>
        public static bool HasLastRobot => PlayerPrefs.GetInt(KeyHasLast, 0) == 1;

        /// <summary>The hardware model id of the last-driven robot, or null.</summary>
        public static string LastRobotId => PlayerPrefs.GetString(KeyRobotId, null);

        /// <summary>The display name of the last-driven robot, or null.</summary>
        public static string LastRobotName => PlayerPrefs.GetString(KeyRobotName, null);

        /// <summary>The robot IP from the last session.</summary>
        public static string LastRobotIp => PlayerPrefs.GetString(KeyRobotIp, "192.168.1.101");

        /// <summary>The ROSBridge port from the last session.</summary>
        public static int LastRobotPort => PlayerPrefs.GetInt(KeyRobotPort, 9090);

        /// <summary>
        /// Record the robot the user just started teleoperating, so the next
        /// session can offer a quick-resume.
        /// </summary>
        public static void Save(string robotId, string robotName, string ip, int port)
        {
            PlayerPrefs.SetString(KeyRobotId, robotId ?? "");
            PlayerPrefs.SetString(KeyRobotName, robotName ?? "");
            PlayerPrefs.SetString(KeyRobotIp, ip ?? "192.168.1.101");
            PlayerPrefs.SetInt(KeyRobotPort, port);
            PlayerPrefs.SetInt(KeyHasLast, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Clear the saved last robot (e.g. on sign-out).</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(KeyRobotId);
            PlayerPrefs.DeleteKey(KeyRobotName);
            PlayerPrefs.DeleteKey(KeyRobotIp);
            PlayerPrefs.DeleteKey(KeyRobotPort);
            PlayerPrefs.DeleteKey(KeyHasLast);
            PlayerPrefs.Save();
        }
    }
}
