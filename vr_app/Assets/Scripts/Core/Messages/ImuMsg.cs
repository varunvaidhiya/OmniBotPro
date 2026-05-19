using Newtonsoft.Json;

namespace OmniBot.VR.Core
{
    /// <summary>
    /// sensor_msgs/Imu — IMU measurement including orientation, angular velocity,
    /// and linear acceleration with associated covariance matrices.
    /// </summary>
    public class ImuMsg
    {
        [JsonProperty("header")]
        public HeaderMsg header;

        /// <summary>Estimated orientation of the sensor frame (quaternion).</summary>
        [JsonProperty("orientation")]
        public QuaternionMsg orientation;

        /// <summary>Row-major 3x3 covariance matrix for orientation estimate.</summary>
        [JsonProperty("orientation_covariance")]
        public double[] orientation_covariance;

        /// <summary>Angular velocity in rad/s in the sensor frame.</summary>
        [JsonProperty("angular_velocity")]
        public Vector3Msg angular_velocity;

        /// <summary>Row-major 3x3 covariance matrix for angular velocity.</summary>
        [JsonProperty("angular_velocity_covariance")]
        public double[] angular_velocity_covariance;

        /// <summary>Linear acceleration in m/s² in the sensor frame (includes gravity).</summary>
        [JsonProperty("linear_acceleration")]
        public Vector3Msg linear_acceleration;

        /// <summary>Row-major 3x3 covariance matrix for linear acceleration.</summary>
        [JsonProperty("linear_acceleration_covariance")]
        public double[] linear_acceleration_covariance;

        public ImuMsg()
        {
            header = new HeaderMsg();
            orientation = new QuaternionMsg();
            orientation_covariance = new double[9];
            angular_velocity = new Vector3Msg();
            angular_velocity_covariance = new double[9];
            linear_acceleration = new Vector3Msg();
            linear_acceleration_covariance = new double[9];
        }
    }
}
