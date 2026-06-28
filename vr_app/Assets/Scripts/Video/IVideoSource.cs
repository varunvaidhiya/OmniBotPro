using System;
using UnityEngine;

namespace OmniBot.VR.Video
{
    /// <summary>
    /// Abstraction over a robot camera video transport — one implementation per
    /// delivery method (MJPEG via <c>web_video_server</c>, WebRTC via
    /// <c>com.unity.webrtc</c>). The <see cref="CameraFeedController"/> picks the
    /// best available source per camera topic and decouples the UI from the
    /// transport, so swapping MJPEG → WebRTC is a source swap, not a UI rewrite.
    ///
    /// A source owns its own connection lifecycle (coroutine / peer connection)
    /// and calls <see cref="OnFrame"/> whenever a new decoded texture is ready.
    /// The controller assigns that texture to the display RawImage.
    /// </summary>
    public interface IVideoSource : IDisposable
    {
        /// <summary>Human-readable transport name, e.g. "WebRTC" or "MJPEG".</summary>
        string TransportName { get; }

        /// <summary>True while the source is actively streaming.</summary>
        bool IsStreaming { get; }

        /// <summary>Fired on the main thread when a new video frame is decoded.</summary>
        event Action<Texture2D> OnFrame;

        /// <summary>Fires when the stream loses signal / disconnects.</summary>
        event Action<string> OnError;

        /// <summary>Begin streaming from the given camera topic on the robot.</summary>
        /// <param name="robotIp">Robot IP (for MJPEG HTTP) or signaling server URL.</param>
        /// <param name="topic">ROS camera topic, e.g. <c>/camera/front/image_raw</c>.</param>
        void Start(string robotIp, string topic);

        /// <summary>Stop the stream and release transport resources.</summary>
        void Stop();
    }
}
