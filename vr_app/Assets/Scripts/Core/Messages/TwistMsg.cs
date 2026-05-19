using Newtonsoft.Json;

namespace OmniBot.VR.Core
{
    /// <summary>
    /// geometry_msgs/Vector3 — used inside Twist.
    /// </summary>
    public class Vector3Msg
    {
        [JsonProperty("x")]
        public float x;

        [JsonProperty("y")]
        public float y;

        [JsonProperty("z")]
        public float z;

        public Vector3Msg() { }

        public Vector3Msg(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    /// <summary>
    /// geometry_msgs/Twist — linear and angular velocity.
    /// </summary>
    public class TwistMsg
    {
        [JsonProperty("linear")]
        public Vector3Msg linear;

        [JsonProperty("angular")]
        public Vector3Msg angular;

        public TwistMsg()
        {
            linear = new Vector3Msg();
            angular = new Vector3Msg();
        }

        public TwistMsg(Vector3Msg linear, Vector3Msg angular)
        {
            this.linear = linear;
            this.angular = angular;
        }
    }
}
