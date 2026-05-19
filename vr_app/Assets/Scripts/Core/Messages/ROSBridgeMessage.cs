using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OmniBot.VR.Core
{
    /// <summary>
    /// Wraps a ROS publish operation for serialization over ROSBridge v2.
    /// </summary>
    public class ROSPublishMessage<T>
    {
        [JsonProperty("op")]
        public string Op { get; } = "publish";

        [JsonProperty("topic")]
        public string Topic { get; set; }

        [JsonProperty("msg")]
        public T Msg { get; set; }

        public ROSPublishMessage(string topic, T msg)
        {
            Topic = topic;
            Msg = msg;
        }
    }

    /// <summary>
    /// Sent to ROSBridge to advertise a publisher on a topic.
    /// </summary>
    public class ROSAdvertiseMessage
    {
        [JsonProperty("op")]
        public string Op { get; } = "advertise";

        [JsonProperty("topic")]
        public string Topic { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        public ROSAdvertiseMessage(string topic, string type)
        {
            Topic = topic;
            Type = type;
        }
    }

    /// <summary>
    /// Sent to ROSBridge to subscribe to a topic.
    /// </summary>
    public class ROSSubscribeMessage
    {
        [JsonProperty("op")]
        public string Op { get; } = "subscribe";

        [JsonProperty("topic")]
        public string Topic { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        public ROSSubscribeMessage(string topic, string type)
        {
            Topic = topic;
            Type = type;
        }
    }

    /// <summary>
    /// Sent to ROSBridge to unsubscribe from a topic.
    /// </summary>
    public class ROSUnsubscribeMessage
    {
        [JsonProperty("op")]
        public string Op { get; } = "unsubscribe";

        [JsonProperty("topic")]
        public string Topic { get; set; }

        public ROSUnsubscribeMessage(string topic)
        {
            Topic = topic;
        }
    }

    /// <summary>
    /// Represents an incoming message from ROSBridge.
    /// The msg field is kept as JObject for flexible deserialization by subscribers.
    /// </summary>
    public class ROSIncomingMessage
    {
        [JsonProperty("op")]
        public string Op { get; set; }

        [JsonProperty("topic")]
        public string Topic { get; set; }

        [JsonProperty("msg")]
        public JObject Msg { get; set; }
    }
}
