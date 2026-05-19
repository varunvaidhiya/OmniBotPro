using Newtonsoft.Json;

namespace OmniBot.VR.Core
{
    /// <summary>
    /// std_msgs/Header — standard ROS message header.
    /// </summary>
    public class HeaderMsg
    {
        [JsonProperty("seq")]
        public uint seq;

        [JsonProperty("stamp")]
        public TimeMsg stamp;

        [JsonProperty("frame_id")]
        public string frame_id;

        public HeaderMsg()
        {
            stamp = new TimeMsg();
            frame_id = "";
        }

        public HeaderMsg(uint seq, string frameId)
        {
            this.seq = seq;
            this.frame_id = frameId;
            stamp = new TimeMsg();
        }
    }

    /// <summary>
    /// builtin_interfaces/Time used inside headers.
    /// </summary>
    public class TimeMsg
    {
        [JsonProperty("secs")]
        public int secs;

        [JsonProperty("nsecs")]
        public int nsecs;

        public TimeMsg() { }

        public TimeMsg(int secs, int nsecs)
        {
            this.secs = secs;
            this.nsecs = nsecs;
        }
    }

    /// <summary>
    /// sensor_msgs/JointState — joint names, positions, velocities, and efforts.
    /// </summary>
    public class JointStateMsg
    {
        [JsonProperty("header")]
        public HeaderMsg header;

        [JsonProperty("name")]
        public string[] name;

        [JsonProperty("position")]
        public float[] position;

        [JsonProperty("velocity")]
        public float[] velocity;

        [JsonProperty("effort")]
        public float[] effort;

        public JointStateMsg()
        {
            header = new HeaderMsg();
        }

        public JointStateMsg(string[] names, float[] positions)
        {
            header = new HeaderMsg();
            name = names;
            position = positions;
            velocity = new float[names.Length];
            effort = new float[names.Length];
        }
    }
}
