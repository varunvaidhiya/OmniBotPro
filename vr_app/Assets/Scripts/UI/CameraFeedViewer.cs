using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using OmniBot.VR.Core;

namespace OmniBot.VR.UI
{
    /// <summary>
    /// Displays MJPEG camera streams from web_video_server in a VR panel.
    ///
    /// Parses multipart/x-mixed-replace boundaries: locates the blank line
    /// (\\r\\n\\r\\n) separating headers from body, reads Content-Length bytes
    /// for the JPEG payload, then loads it into a RawImage via ImageConversion.
    ///
    /// If a stream URL is unreachable for more than 5 seconds, a "No Signal"
    /// placeholder is shown. Stream URLs update automatically when the robot
    /// IP changes (via ConnectionManager.OnRobotIpChanged).
    /// </summary>
    public class CameraFeedViewer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Camera URLs (topic path only, e.g. /camera/front/image_raw)")]
        [SerializeField] private string[] cameraTopics = {
            "/camera/front/image_raw",
            "/camera/wrist/image_raw",
            "/camera/base/bev/image_raw",
        };

        [Header("Display")]
        [SerializeField] private RawImage displayImage;
        [SerializeField] private TMP_Text cameraNameOverlay;
        [SerializeField] private Texture2D noSignalTexture;

        [Header("web_video_server")]
        [SerializeField] private int webVideoServerPort = 8080;

        // ── State ─────────────────────────────────────────────────────────────
        private string[] _fullUrls;
        private int      _currentCameraIndex = 0;
        private string   _robotIp            = "192.168.1.101";
        private Coroutine _streamCoroutine   = null;

        private const float NoSignalTimeout = 5f;    // seconds
        private const int   BoundarySearchSize = 512; // bytes to search for boundary

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Start()
        {
            _robotIp = PlayerPrefs.GetString("robot_ip", "192.168.1.101");
            RebuildUrls(_robotIp);

            // Listen for IP changes
            ConnectionManager.Instance.OnRobotIpChanged += OnRobotIpChanged;
            ConnectionManager.Instance.OnConnectionStateChanged += OnConnectionStateChanged;

            // Start streaming current camera
            StartStreaming();
        }

        private void OnDestroy()
        {
            if (ConnectionManager.Instance != null)
            {
                ConnectionManager.Instance.OnRobotIpChanged    -= OnRobotIpChanged;
                ConnectionManager.Instance.OnConnectionStateChanged -= OnConnectionStateChanged;
            }
            if (_streamCoroutine != null) StopCoroutine(_streamCoroutine);
        }

        private void Update()
        {
            // Right thumbstick press → next camera
            if (OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick, OVRInput.Controller.RTouch))
                CycleCamera();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Advances to the next camera in the list and restarts the stream.</summary>
        public void CycleCamera()
        {
            _currentCameraIndex = (_currentCameraIndex + 1) % Mathf.Max(1, _fullUrls.Length);
            StartStreaming();
        }

        /// <summary>Switches to a specific camera by index.</summary>
        public void SetCamera(int index)
        {
            if (_fullUrls == null || _fullUrls.Length == 0) return;
            _currentCameraIndex = Mathf.Clamp(index, 0, _fullUrls.Length - 1);
            StartStreaming();
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void OnRobotIpChanged(string newIp)
        {
            _robotIp = newIp;
            RebuildUrls(newIp);
            StartStreaming();
        }

        private void OnConnectionStateChanged(bool connected)
        {
            if (!connected)
            {
                if (_streamCoroutine != null) { StopCoroutine(_streamCoroutine); _streamCoroutine = null; }
                ShowNoSignal();
            }
            else
            {
                StartStreaming();
            }
        }

        private void RebuildUrls(string ip)
        {
            _fullUrls = new string[cameraTopics.Length];
            for (int i = 0; i < cameraTopics.Length; i++)
            {
                string encoded = Uri.EscapeUriString(cameraTopics[i]);
                _fullUrls[i] = $"http://{ip}:{webVideoServerPort}/stream?topic={encoded}";
            }
        }

        private void StartStreaming()
        {
            if (_streamCoroutine != null) { StopCoroutine(_streamCoroutine); _streamCoroutine = null; }
            if (_fullUrls == null || _fullUrls.Length == 0) { ShowNoSignal(); return; }

            int idx = Mathf.Clamp(_currentCameraIndex, 0, _fullUrls.Length - 1);
            string url = _fullUrls[idx];
            string topicName = cameraTopics[idx];

            if (cameraNameOverlay != null) cameraNameOverlay.text = topicName;

            _streamCoroutine = StartCoroutine(StreamMJPEG(url));
        }

        /// <summary>
        /// Coroutine that maintains a persistent HTTP connection to the MJPEG stream.
        /// On timeout or error, shows the No Signal placeholder.
        /// </summary>
        private IEnumerator StreamMJPEG(string url)
        {
            float noSignalTimer = 0f;
            bool  gotFirstFrame = false;

            while (true) // outer retry loop
            {
                byte[] receiveBuffer = new byte[256 * 1024]; // 256 KB ring buffer
                int    bufferFill    = 0;

                using (UnityWebRequest req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
                {
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Accept", "multipart/x-mixed-replace");
                    req.timeout         = 10;

                    req.SendWebRequest();

                    float startTime = Time.time;

                    while (!req.isDone)
                    {
                        // Check for data
                        byte[] data = req.downloadHandler.data;
                        if (data != null && data.Length > bufferFill)
                        {
                            // Append new bytes to ring buffer
                            int newBytes = data.Length - bufferFill;
                            int copyLen  = Mathf.Min(newBytes, receiveBuffer.Length - bufferFill);
                            if (copyLen > 0)
                            {
                                Buffer.BlockCopy(data, bufferFill, receiveBuffer, bufferFill, copyLen);
                                bufferFill += copyLen;
                            }

                            // Try to extract complete JPEG frames
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

                            // Compact buffer
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

                        // No-signal timeout
                        noSignalTimer += Time.deltaTime;
                        if (noSignalTimer > NoSignalTimeout && !gotFirstFrame)
                        {
                            ShowNoSignal();
                        }

                        yield return null;
                    }

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning($"[CameraFeedViewer] Stream error: {req.error} on {url}");
                        ShowNoSignal();
                    }
                }

                // Wait before retrying
                yield return new WaitForSeconds(2f);
                bufferFill = 0;
            }
        }

        /// <summary>
        /// Searches the buffer for a complete JPEG (starts with FF D8, ends with FF D9).
        /// Also attempts Content-Length–based extraction when a header block is present.
        /// Returns true and sets frameStart/frameLen when a complete frame is found.
        /// </summary>
        private bool TryExtractJpeg(byte[] buf, int len, int offset, out int frameStart, out int frameLen)
        {
            frameStart = 0;
            frameLen   = 0;

            if (len - offset < 4) return false;

            // Strategy 1: Content-Length based (more reliable in multipart streams)
            // Look for \r\n\r\n after "Content-Length:"
            string header = Encoding.ASCII.GetString(buf, offset, Mathf.Min(BoundarySearchSize, len - offset));
            int clIdx = header.IndexOf("Content-Length:", StringComparison.OrdinalIgnoreCase);
            if (clIdx >= 0)
            {
                int eolIdx  = header.IndexOf("\r\n\r\n", clIdx);
                if (eolIdx >= 0)
                {
                    int headerEnd = offset + eolIdx + 4; // byte offset after header
                    // Parse Content-Length value
                    int clEnd    = header.IndexOf('\r', clIdx + 15);
                    if (clEnd < 0) clEnd = header.IndexOf('\n', clIdx + 15);
                    if (clEnd > 0)
                    {
                        string clStr = header.Substring(clIdx + 15, clEnd - (clIdx + 15)).Trim();
                        if (int.TryParse(clStr, out int contentLen))
                        {
                            if (headerEnd + contentLen <= len)
                            {
                                frameStart = headerEnd;
                                frameLen   = contentLen;
                                return true;
                            }
                        }
                    }
                }
            }

            // Strategy 2: JPEG marker scan (FF D8 ... FF D9)
            for (int i = offset; i < len - 1; i++)
            {
                if (buf[i] == 0xFF && buf[i + 1] == 0xD8)
                {
                    // Found SOI; scan for EOI (FF D9)
                    for (int j = i + 2; j < len - 1; j++)
                    {
                        if (buf[j] == 0xFF && buf[j + 1] == 0xD9)
                        {
                            frameStart = i;
                            frameLen   = j + 2 - i;
                            return true;
                        }
                    }
                    break; // SOI found but no EOI yet; need more data
                }
            }

            return false;
        }

        /// <summary>Decodes JPEG bytes into a Texture2D and assigns to the RawImage.</summary>
        private void LoadTexture(byte[] jpegBytes)
        {
            if (displayImage == null) return;

            Texture2D tex = displayImage.texture as Texture2D;
            if (tex == null)
            {
                tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                displayImage.texture = tex;
            }

            if (ImageConversion.LoadImage(tex, jpegBytes, false))
            {
                displayImage.texture = tex;
            }
            else
            {
                Debug.LogWarning("[CameraFeedViewer] Failed to decode JPEG frame.");
            }
        }

        private void ShowNoSignal()
        {
            if (displayImage == null) return;
            if (noSignalTexture != null)
                displayImage.texture = noSignalTexture;
            else
            {
                // Generate a simple grey texture as placeholder
                Texture2D grey = new Texture2D(64, 64);
                Color[] pixels = new Color[64 * 64];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0.2f, 0.2f, 0.2f);
                grey.SetPixels(pixels);
                grey.Apply();
                displayImage.texture = grey;
            }
        }
    }
}
