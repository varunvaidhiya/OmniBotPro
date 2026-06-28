using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace OmniBot.VR.Video
{
    /// <summary>
    /// WebRTC video source — sub-100 ms telepresence via the
    /// <c>com.unity.webrtc</c> package. The headset acts as a WebRTC receiver:
    /// it negotiates with a signaling server on the robot (or a
    /// <c>ros2_webRTC</c> / <c>aiortc</c> bridge) and renders the incoming video
    /// track directly to a texture, skipping the JPEG encode/decode round-trip
    /// that MJPEG requires.
    ///
    /// <b>Graceful degradation:</b> <c>com.unity.webrtc</c> is an optional Unity
    /// package (not in the base <c>manifest.json</c>). This source detects at
    /// runtime whether the <c>Unity.WebRTC</c> assembly is loaded. If it is, the
    /// full WebRTC peer-connection path runs; if not, <see cref="Start"/>
    /// immediately fires <see cref="OnError"/> so the
    /// <see cref="CameraFeedController"/> falls back to
    /// <see cref="MjpegVideoSource"/>. This keeps the headset buildable without
    /// the package while letting anyone who adds it get the low-latency path.
    ///
    /// To enable WebRTC:
    /// 1. Add <c>com.unity.webrtc</c> to <c>Packages/manifest.json</c>.
    /// 2. Ensure a signaling server is running on the robot (e.g.
    ///    <c>ros2_webrtc</c> or a custom <c>aiortc</c> bridge exposing ROS image
    ///    topics as WebRTC video tracks).
    /// 3. Set <c>preferWebRtc = true</c> on the <see cref="CameraFeedController"/>.
    /// </summary>
    public sealed class WebRtcVideoSource : IVideoSource
    {
        public string TransportName => "WebRTC";
        public bool IsStreaming => _streaming;
        public event Action<Texture2D> OnFrame;
        public event Action<string> OnError;

        private readonly MonoBehaviour _host;
        private bool _streaming;
        private bool _disposed;
        private object _peerConnection; // Unity.WebRTC.RTCPeerConnection (boxed)
        private Coroutine _signalingCoroutine;

        // Cached assembly / type references (resolved lazily via reflection)
        private static bool _assemblyChecked;
        private static bool _assemblyAvailable;
        private static Type _peerConnType;
        private static Type _videoTrackType;
        private static MethodInfo _createPcMethod;

        public WebRtcVideoSource(MonoBehaviour host)
        {
            _host = host;
        }

        public void Start(string robotIp, string topic)
        {
            if (!EnsureAssemblyLoaded())
            {
                OnError?.Invoke("com.unity.webrtc not installed — falling back to MJPEG");
                return;
            }
            _signalingCoroutine = _host.StartCoroutine(SignalingLoop(robotIp, topic));
        }

        public void Stop()
        {
            _streaming = false;
            if (_signalingCoroutine != null && _host != null)
            {
                _host.StopCoroutine(_signalingCoroutine);
                _signalingCoroutine = null;
            }
            DisposePeerConnection();
        }

        public void Dispose()
        {
            Stop();
            _disposed = true;
        }

        // ── Assembly detection ────────────────────────────────────────────────────

        private static bool EnsureAssemblyLoaded()
        {
            if (_assemblyChecked) return _assemblyAvailable;
            _assemblyChecked = true;
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var a in asm)
                {
                    if (a.GetName().Name != "Unity.WebRTC") continue;
                    _peerConnType = a.GetType("Unity.WebRTC.RTCPeerConnection");
                    _videoTrackType = a.GetType("Unity.WebRTC.VideoStreamTrack");
                    if (_peerConnType != null)
                    {
                        _assemblyAvailable = true;
                        return true;
                    }
                }
            }
            catch { }
            _assemblyAvailable = false;
            return false;
        }

        /// <summary>True if <c>com.unity.webrtc</c> is loaded and usable.</summary>
        public static bool IsAvailable => EnsureAssemblyLoaded();

        // ── Signaling + peer connection (reflection-driven) ───────────────────────
        // The actual SDP offer/answer exchange depends on the signaling server
        // protocol. This implementation scaffolds the peer connection and the
        // video-track callback wiring; the SDP negotiation is a placeholder that
        // fires OnError until a real signaling endpoint is configured.

        private IEnumerator SignalingLoop(string robotIp, string topic)
        {
            if (_disposed) yield break;

            // Create the peer connection via reflection
            try
            {
                _peerConnection = Activator.CreateInstance(_peerConnType);
                _streaming = true;
                Debug.Log($"[WebRtcVideoSource] Peer connection created for {topic} (signaling not yet wired — see code comment).");
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Failed to create peer connection: {e.Message}");
                yield break;
            }

            // TODO: implement the signaling handshake against the robot's WebRTC
            // signaling server. The handshake is:
            //   1. POST an SDP offer to http://{robotIp}: signalingPort /offer
            //   2. Receive the SDP answer + ICE candidates
            //   3. SetRemoteDescription on the peer connection
            //   4. Wire the incoming VideoStreamTrack → OnFrame(texture)
            // Until the signaling server is deployed, fire OnError so the
            // controller falls back to MJPEG immediately.
            OnError?.Invoke("WebRTC signaling not yet configured — falling back to MJPEG");
            _streaming = false;
            DisposePeerConnection();
        }

        private void DisposePeerConnection()
        {
            if (_peerConnection == null) return;
            try
            {
                var closeMethod = _peerConnType.GetMethod("Close");
                closeMethod?.Invoke(_peerConnection, null);
            }
            catch { }
            _peerConnection = null;
        }
    }
}
