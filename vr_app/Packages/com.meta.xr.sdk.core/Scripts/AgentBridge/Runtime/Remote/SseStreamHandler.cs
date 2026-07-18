/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#nullable enable

using System;
using System.Text;
using UnityEngine.Networking;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Custom DownloadHandler that processes Server-Sent Events (SSE) streams incrementally.
    /// Parses the SSE wire format (event: ...\ndata: ...\n\n) and fires a callback for each complete event.
    /// Designed for use with UnityWebRequest for real-time streaming from the Remote Agent Server.
    /// </summary>
    public class SseStreamHandler : DownloadHandlerScript
    {
        private readonly Action<SseEvent> _onEventReceived;
        private readonly Action<string>? _onError;
        private readonly StringBuilder _buffer = new();

        private SseEventType _currentEventType = SseEventType.Message;
        private readonly StringBuilder _currentData = new();
        private bool _hasData;

        /// <summary>
        /// Total number of bytes received from the stream.
        /// </summary>
        public long BytesReceived { get; private set; }

        /// <summary>
        /// Total number of complete SSE events parsed and delivered.
        /// </summary>
        public int EventsReceived { get; private set; }

        /// <summary>
        /// Create an SSE stream handler.
        /// </summary>
        /// <param name="onEventReceived">Callback fired for each complete SSE event. Called on the main thread.</param>
        /// <param name="onError">Optional callback fired on parse errors.</param>
        public SseStreamHandler(Action<SseEvent> onEventReceived, Action<string>? onError = null)
            : base(new byte[4096])
        {
            _onEventReceived = onEventReceived ?? throw new ArgumentNullException(nameof(onEventReceived));
            _onError = onError;
        }

        /// <summary>
        /// Called by UnityWebRequest when new data arrives from the server.
        /// Processes the incoming bytes as UTF-8 text and parses SSE events.
        /// </summary>
        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength == 0)
                return true;

            BytesReceived += dataLength;

            try
            {
                var text = Encoding.UTF8.GetString(data, 0, dataLength);
                _buffer.Append(text);
                ProcessBuffer();
            }
            catch (Exception ex)
            {
                _onError?.Invoke($"Error processing SSE data: {ex.Message}");
            }

            return true;
        }

        protected override float GetProgress() => 0f;

        /// <summary>
        /// Process the internal buffer, extracting complete SSE events.
        /// SSE format: lines separated by \n, events separated by blank lines (\n\n).
        /// </summary>
        private void ProcessBuffer()
        {
            var bufferStr = _buffer.ToString();

            // Process complete lines (terminated by \n)
            while (true)
            {
                var newlineIndex = bufferStr.IndexOf('\n');
                if (newlineIndex < 0)
                    break;

                var line = bufferStr.Substring(0, newlineIndex).TrimEnd('\r');
                bufferStr = bufferStr.Substring(newlineIndex + 1);

                ProcessLine(line);
            }

            // Keep any remaining partial line in the buffer
            _buffer.Clear();
            _buffer.Append(bufferStr);
        }

        /// <summary>
        /// Process a single SSE line according to the SSE specification.
        /// </summary>
        private void ProcessLine(string line)
        {
            // Empty line = end of event, dispatch it
            if (string.IsNullOrEmpty(line))
            {
                DispatchEvent();
                return;
            }

            // Comment lines (starting with ':') are ignored per SSE spec
            if (line.StartsWith(":"))
                return;

            // Parse field:value
            var colonIndex = line.IndexOf(':');
            string field;
            string value;

            if (colonIndex >= 0)
            {
                field = line.Substring(0, colonIndex);
                value = line.Substring(colonIndex + 1);
                // SSE spec: if value starts with a space, remove it
                if (value.Length > 0 && value[0] == ' ')
                    value = value.Substring(1);
            }
            else
            {
                field = line;
                value = string.Empty;
            }

            switch (field)
            {
                case "event":
                    _currentEventType = SseEvent.ParseWireName(value);
                    break;
                case "data":
                    if (_hasData)
                        _currentData.Append('\n');
                    _currentData.Append(value);
                    _hasData = true;
                    break;
                    // "id" and "retry" fields are defined by SSE spec but not used here
            }
        }

        /// <summary>
        /// Dispatch a complete SSE event to the callback.
        /// </summary>
        private void DispatchEvent()
        {
            // Only dispatch if we received at least one data: field
            if (!_hasData)
            {
                ResetEventState();
                return;
            }

            var sseEvent = new SseEvent
            {
                EventType = _currentEventType,
                Data = _currentData.ToString()
            };

            EventsReceived++;

            try
            {
                _onEventReceived.Invoke(sseEvent);
            }
            catch (Exception ex)
            {
                _onError?.Invoke($"Error in SSE event handler: {ex.Message}");
            }

            ResetEventState();
        }

        /// <summary>
        /// Reset state for the next event.
        /// </summary>
        private void ResetEventState()
        {
            _currentEventType = SseEventType.Message;
            _currentData.Clear();
            _hasData = false;
        }
    }
}
