using Newtonsoft.Json;

namespace OmniBot.VR.Core
{
    /// <summary>
    /// geometry_msgs/PoseStamped — a stamped pose with a header, used for the
    /// robot-side IK mode (MoveIt 2 Servo). When <c>RobotProfile.IkLocation</c>
    /// is <c>Robot</c>, the manipulation scheme publishes the Cartesian
    /// end-effector target to a MoveIt Servo topic (e.g.
    /// <c>/servo_server/target_pose</c>) as this message type, instead of solving
    /// IK on the headset and publishing <c>sensor_msgs/JointState</c>.
    ///
    /// The robot's MoveIt Servo stack solves the IK with full collision awareness
    /// — the tradeoff is higher latency (network round-trip + solver) for safety.
    /// </summary>
    public class PoseStampedMsg
    {
        [JsonProperty("header")]
        public HeaderMsg header;

        [JsonProperty("pose")]
        public PoseMsg pose;

        public PoseStampedMsg()
        {
            header = new HeaderMsg();
            pose = new PoseMsg();
        }

        public PoseStampedMsg(Vector3Msg position, QuaternionMsg orientation)
        {
            header = new HeaderMsg();
            pose = new PoseMsg { position = position, orientation = orientation };
        }
    }

    /// <summary>geometry_msgs/Pose — position + orientation.</summary>
    public class PoseMsg
    {
        [JsonProperty("position")]
        public Vector3Msg position;

        [JsonProperty("orientation")]
        public QuaternionMsg orientation;

        public PoseMsg()
        {
            position = new Vector3Msg();
            orientation = new QuaternionMsg();
        }
    }

    /// <summary>geometry_msgs/Quaternion — x, y, z, w.</summary>
    public class QuaternionMsg
    {
        [JsonProperty("x")]
        public float x;
        [JsonProperty("y")]
        public float y;
        [JsonProperty("z")]
        public float z;
        [JsonProperty("w")]
        public float w = 1f;

        public QuaternionMsg() { }

        public QuaternionMsg(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }
}
