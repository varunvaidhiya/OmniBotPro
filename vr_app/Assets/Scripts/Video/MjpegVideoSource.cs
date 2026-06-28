using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace OmniBot.VR.Video
{
    /// <summary>
    /// MJPEG video source — streams JPEG frames from <c>web_video_server</c>
    /// over HTTP multipart/x-mixed-replace. Extracted from the legacy
    /// <c>CameraFeedViewer</c> so the transport is decoupled from the UI and
    /// swappable with <see cref="WebRtcVideoSource"/>. Parses Content-Length
    /// headers with a JPEG-marker (FF D8 … FF D9) fallback, loads each frame into
    /// a reusable <see cref="Texture2D"/> via <c>ImageConversion.LoadImage</c>.
    ///
    /// This is the default / fallback source — it works everywhere (no extra
    /// Unity packages), but has ~200–500 ms latency. WebRTC is sub-100 ms.
    /// </summary>
    public sealed class MjpegVideoSource : IVideoSource
    {
        private const float NoSignalTimeout = 5f;
        private const int BoundarySearchSize = 512;
        private const int WebVideoServerPort = 8080;

        public string TransportName => "MJPEG";
        public bool IsStreaming => _coroutine != null;
        public event Action<Texture2D> OnFrame;
        public event Action<string> OnError;

        private readonly MonoBehaviour _host;
        private Coroutine _coroutine;
        private Texture2D _texture;
        private bool _disposed;

        /// <param name="host">A MonoBehaviour to host the streaming coroutine.</param>
        public MjpegVideoSource(MonoBehaviour host)
        {
            _host = host;
        }

        public void Start(string robotIp, string topic)
        {
            Stop();
            string encoded = Uri.EscapeUriString(topic);
            string url = $"http://{robotIp}:{WebVideoServerPort}/stream?topic={encoded}";
            _coroutine = _host.StartCoroutine(StreamMjpeg(url));
        }

        public void Stop()
        {
            if (_coroutine != null && _host != null)
            {
                _host.StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }

        public void Dispose()
        {
            Stop();
            if (_texture != null)
            {
                UnityEngine.Object.Destroy(_texture);
                _texture = null;
            }
            _disposed = true;
        }

        // ── MJPEG streaming coroutine (ported from CameraFeedViewer) ─────────────

        private IEnumerator StreamMjpeg(string url)
        {
            float noSignalTimer = 0f;
            bool gotFirstFrame = false;

            while (!_disposed)
            {
                byte[] receiveBuffer = new byte[256 * 1024];
                int bufferFill = 0;

                using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
                {
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Accept", "multipart/x-mixed-replace");
                    req.timeout = 10;
                    req.SendWebRequest();

                    while (!req.isDone && !_disposed)
                    {
                        byte[] data = req.downloadHandler.data;
                        if (data != null && data.Length > bufferFill)
                        {
                            int newBytes = data.Length - bufferFill;
                            int copyLen = Mathf.Min(newBytes, receiveBuffer.Length - bufferFill);
                            if (copyLen > 0)
                            {
                                Buffer.BlockCopy(data, bufferFill, receiveBuffer, bufferFill, copyLen);
                                bufferFill += copyLen;
                            }

                            int consumed = 0;
                            while (TryExtractJpeg(receiveBuffer, bufferFill, consumed, out int frameStart, out int frameLen))
                            {
                                byte[] jpegBytes = new byte[frameLen];
                                Buffer.BlockCopy(receiveBuffer, frameStart, jpegBytes, 0, frameLen);
                                LoadTexture(jpegBytes);
                                gotFirstFrame = true;
                                noSignalTimer = 0f;
                                consumed = frameStart + frameLen;
                            }

                            if (consumed > 0 && consumed < bufferFill)
                            {
                                Buffer.BlockCopy(receiveBuffer, consumed, receiveBuffer, 0, bufferFill - consumed);
                                bufferFill -= consumed;
                            }
                            else if (consumed >= bufferFill)
                            {
                                bufferFill = 0;
                            }
                        }

                        noSignalTimer += Time.deltaTime;
                        if (noSignalTimer > NoSignalTimeout && !gotFirstFrame)
                            OnError?.Invoke("No signal");

                        yield return null;
                    }

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        OnError?.Invoke($"Stream error: {req.error}");
                    }
                }

                if (_disposed) break;
                yield return new WaitForSeconds(2f);
                bufferFill = 0;
            }
        }

        private void LoadTexture(byte[] jpegBytes)
        {
            if (_disposed) return;
            if (_texture == null)
                _texture = new Texture2D(2, 2, TextureFormat.RGB24, false);

            if (ImageConversion.LoadImage(_texture, jpegBytes, false))
                OnFrame?.Invoke(_texture);
        }

        // ── JPEG frame extraction (ported from CameraFeedViewer) ─────────────────

        private static bool TryExtractJpeg(byte[] buf, int len, int offset, out int frameStart, out int frameLen)
        {
            frameStart = 0;
            frameLen = 0;
            if (len - offset < 4) return false;

            string header = Encoding.ASCII.GetString(buf, offset, Mathf.Min(BoundarySearchSize, len - offset));
            int clIdx = header.IndexOf("Content-Length:", StringComparison.OrdinalIgnoreCase);
            if (clIdx >= 0)
            {
                int eolIdx = header.IndexOf("\r\n\r\n", clIdx);
                if (eolIdx >= 0)
                {
                    int headerEnd = offset + eolIdx + 4;
                    int clEnd = header.IndexOf('\r', clIdx + 15);
                    if (clEnd < 0) clEnd = header.IndexOf('\n', clIdx + 15);
                    if (clEnd > 0)
                    {
                        string clStr = header.Substring(clIdx + 15, clEnd - (clIdx + 15)).Trim();
                        if (int.TryParse(clStr, out int contentLen) && headerEnd + contentLen <= len)
                        {
                            frameStart = headerEnd;
                            frameLen = contentLen;
                            return true;
                        }
                    }
                }
            }

            for (int i = offset; i < len - 1; i++)
            {
                if (buf[i] == 0xFF && buf[i + 1] == 0xD8)
                {
                    for (int j = i + 2; j < len - 1; j++)
                    {
                        if (buf[j] == 0xFF && buf[j + 1] == 0xD9)
                        {
                            frameStart = i;
                            frameLen = j + 2 - i;
                            return true;
                        }
                    }
                    break;
                }
            }
            return false;
        }
    }
}
