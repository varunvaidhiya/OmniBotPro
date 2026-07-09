using Newtonsoft.Json;

namespace OmniBot.VR.Core
{
    /// <summary>
    /// geometry_msgs/Quaternion
    /// </summary>
    public class QuaternionMsg
    {
        [JsonProperty("x")]
        public float x;

        [JsonProperty("y")]
        public float y;

        [JsonProperty("z")]
        public float z;

        [JsonProperty("w")]
        public float w;

        public QuaternionMsg() { w = 1f; }

        public QuaternionMsg(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }

    /// <summary>
    /// geometry_msgs/Point
    /// </summary>
    public class PointMsg : Vector3Msg
    {
        public PointMsg() : base() { }

        public PointMsg(float x, float y, float z) : base(x, y, z) { }
    }

    /// <summary>
    /// geometry_msgs/Pose — position + orientation.
    /// </summary>
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

    /// <summary>
    /// geometry_msgs/PoseWithCovariance — pose plus 6x6 row-major covariance matrix.
    /// </summary>
    public class PoseWithCovarianceMsg
    {
        [JsonProperty("pose")]
        public PoseMsg pose;

        [JsonProperty("covariance")]
        public double[] covariance;

        public PoseWithCovarianceMsg()
        {
            pose = new PoseMsg();
            covariance = new double[36];
        }
    }

    /// <summary>
    /// geometry_msgs/TwistWithCovariance — twist plus 6x6 row-major covariance matrix.
    /// </summary>
    public class TwistWithCovarianceMsg
    {
        [JsonProperty("twist")]
        public TwistMsg twist;

        [JsonProperty("covariance")]
        public double[] covariance;

        public TwistWithCovarianceMsg()
        {
            twist = new TwistMsg();
            covariance = new double[36];
        }
    }

    /// <summary>
    /// nav_msgs/Odometry — robot pose and velocity estimate with covariances.
    /// </summary>
    public class OdometryMsg
    {
        [JsonProperty("header")]
        public HeaderMsg header;

        [JsonProperty("child_frame_id")]
        public string child_frame_id;

        [JsonProperty("pose")]
        public PoseWithCovarianceMsg pose;

        [JsonProperty("twist")]
        public TwistWithCovarianceMsg twist;

        public OdometryMsg()
        {
            header = new HeaderMsg();
            child_frame_id = "base_link";
            pose = new PoseWithCovarianceMsg();
            twist = new TwistWithCovarianceMsg();
        }
    }
}
