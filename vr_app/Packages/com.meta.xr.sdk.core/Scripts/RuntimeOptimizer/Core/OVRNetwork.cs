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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Meta.XR.RuntimeOptimizer.Core
{
    /// <summary>
    /// Categories for network error classification to enable proper handling and recovery strategies.
    /// </summary>
    public enum NetworkErrorCategory
    {
        /// <summary>Issues during server initialization and startup</summary>
        ServerStartup,
        /// <summary>Problems with client connection establishment</summary>
        ClientConnection,
        /// <summary>Errors during data transmission between client and server</summary>
        DataTransmission,
        /// <summary>Errors during server shutdown or disposal</summary>
        ServerShutdown,
        /// <summary>Issues with client disconnection</summary>
        ClientDisconnection,
        /// <summary>Protocol format or validation violations</summary>
        ProtocolViolation,
        /// <summary>Errors during resource cleanup and disposal</summary>
        ResourceCleanup
    }

    /// <summary>
    /// Event arguments for network error notifications containing detailed error context
    /// for centralized logging and error recovery.
    /// </summary>
    public class NetworkErrorEventArgs : EventArgs
    {
        /// <summary>The category of network error that occurred</summary>
        public NetworkErrorCategory Category { get; set; }
        /// <summary>The operation that was being performed when the error occurred</summary>
        public string Operation { get; set; }
        /// <summary>The error message from the exception</summary>
        public string ErrorMessage { get; set; }
        /// <summary>The original exception that caused the error</summary>
        public Exception Exception { get; set; }
        /// <summary>Additional context information about the error</summary>
        public string Context { get; set; }
        /// <summary>Timestamp when the error occurred</summary>
        public DateTime Timestamp { get; set; }
    }

    public class OVRNetwork
    {
        public const int MaxBufferLength = 1048576;
        public const int MaxPayloadLength = MaxBufferLength - FrameHeader.StructSize;

        public const uint FrameHeaderMagicIdentifier = 0x5283A76B;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct FrameHeader
        {
            public uint protocolIdentifier;
            public int payloadType;
            public int payloadLength;

            public const int StructSize = sizeof(uint) + sizeof(int) + sizeof(int);

            // endianness conversion is NOT handled since all our current mobile/PC devices are little-endian
            public byte[] ToBytes()
            {
                int size = Marshal.SizeOf(this);
                Trace.Assert(size == StructSize);

                byte[] arr = new byte[size];

                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(this, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
                Marshal.FreeHGlobal(ptr);
                return arr;
            }
            public static FrameHeader FromBytes(byte[] arr)
            {
                FrameHeader header = new FrameHeader();

                int size = Marshal.SizeOf(header);
                Trace.Assert(size == StructSize);

                IntPtr ptr = Marshal.AllocHGlobal(size);

                Marshal.Copy(arr, 0, ptr, size);

                header = (FrameHeader)Marshal.PtrToStructure(ptr, header.GetType());
                Marshal.FreeHGlobal(ptr);

                return header;
            }

        }

        public class OVRNetworkTcpServer
        {
            public TcpListener tcpListener = null;

            private readonly object clientsLock = new object();
            public readonly List<TcpClient> clients = new List<TcpClient>();

            // Dictionary to track client receive buffers
            private Dictionary<TcpClient, byte[]> clientBuffers = new Dictionary<TcpClient, byte[]>();
            private Dictionary<TcpClient, int> clientBufferSizes = new Dictionary<TcpClient, int>();

            // Callback for received messages from clients
            public Action<TcpClient, int, byte[], int, int> messageReceivedCallback;

            // Network Error Event - Phase 1
            public event EventHandler<NetworkErrorEventArgs> NetworkErrorOccurred;

            // Store the actual port being used
            public int ActivePort { get; private set; } = -1;

            private void RaiseNetworkError(NetworkErrorCategory category, string operation, Exception ex, string context = "")
            {
                NetworkErrorOccurred?.Invoke(this, new NetworkErrorEventArgs
                {
                    Category = category,
                    Operation = operation,
                    ErrorMessage = ex.Message,
                    Exception = ex,
                    Context = context,
                    Timestamp = DateTime.Now
                });
            }

            /// <summary>
            /// Start listening with dynamic port discovery. Tries ports in range 12345-12445.
            /// </summary>
            /// <param name="preferredPort">Preferred port to try first (default: 12345)</param>
            /// <param name="maxPort">Maximum port to try (default: 12445)</param>
            /// <returns>The actual port bound, or -1 if failed</returns>
            public int StartListeningWithPortDiscovery(int preferredPort = 12345, int maxPort = 12445)
            {
                if (tcpListener != null)
                {
                    return ActivePort;
                }

                IPAddress localAddr = IPAddress.Any;

                // Try ports starting from preferred port
                for (int port = preferredPort; port <= maxPort; port++)
                {
                    try
                    {
                        tcpListener = new TcpListener(localAddr, port);
                        tcpListener.Start();
                        ActivePort = port;

                        Debug.Log($"[RuntimeOptimizer] TCP Server started on port {port}");

                        // Begin accepting clients
                        try
                        {
                            tcpListener.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClientCallback), tcpListener);
                        }
                        catch (Exception e)
                        {
                            RaiseNetworkError(NetworkErrorCategory.ServerStartup, "BeginAcceptTcpClient", e,
                                "Failed to begin accepting new TCP clients");
                        }

                        return ActivePort;
                    }
                    catch (SocketException e)
                    {
                        // Port is in use, try next one
                        Debug.Log($"[RuntimeOptimizer] Port {port} unavailable, trying next...");
                        tcpListener = null;
                        ActivePort = -1;

                        if (port == maxPort)
                        {
                            // Last port failed
                            RaiseNetworkError(NetworkErrorCategory.ServerStartup, "StartListeningWithPortDiscovery", e,
                                $"All ports {preferredPort}-{maxPort} unavailable");
                        }
                    }
                }

                return -1;
            }

            // Legacy method for backward compatibility
            public void StartListening(int listeningPort)
            {
                if (tcpListener != null)
                {
                    return;
                }

                IPAddress localAddr = IPAddress.Any;

                tcpListener = new TcpListener(localAddr, listeningPort);
                try
                {
                    tcpListener.Start();
                    ActivePort = listeningPort;
                }
                catch (SocketException e)
                {
                    RaiseNetworkError(NetworkErrorCategory.ServerStartup, "TcpListener.Start", e,
                        $"Port: {listeningPort}, Multiple instances or ADB forwarding conflict suspected");
                    tcpListener = null;
                    ActivePort = -1;
                }

                if (tcpListener != null)
                {
                    try
                    {
                        tcpListener.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClientCallback), tcpListener);
                    }
                    catch (Exception e)
                    {
                        RaiseNetworkError(NetworkErrorCategory.ServerStartup, "BeginAcceptTcpClient", e,
                            "Failed to begin accepting new TCP clients");
                    }
                }
            }

            public void StopListening()
            {
                if (tcpListener == null)
                {
                    RaiseNetworkError(NetworkErrorCategory.ServerShutdown, "StopListening", new InvalidOperationException("TcpListener is null"),
                        "Attempted to stop server that was not properly initialized");
                    return;
                }

                lock (clientsLock)
                {
                    clients.Clear();
                }

                tcpListener.Stop();
                tcpListener = null;
            }

            private void DoAcceptTcpClientCallback(IAsyncResult ar)
            {
                TcpListener listener = ar.AsyncState as TcpListener;
                try
                {
                    TcpClient client = listener.EndAcceptTcpClient(ar);
                    lock (clientsLock)
                    {
                        clients.Add(client);
                        clientBuffers[client] = new byte[MaxBufferLength];
                        clientBufferSizes[client] = 0;
                    }

                    // Start receiving data from this client
                    BeginReceiveData(client);

                    try
                    {
                        tcpListener.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClientCallback), tcpListener);
                    }
                    catch (Exception e)
                    {
                        RaiseNetworkError(NetworkErrorCategory.ClientConnection, "BeginAcceptTcpClient", e,
                            "Failed to begin accepting additional TCP clients");
                    }
                }
                catch (ObjectDisposedException e)
                {
                    RaiseNetworkError(NetworkErrorCategory.ServerShutdown, "EndAcceptTcpClient", e,
                        "Server was disposed during client acceptance");
                }
                catch (Exception e)
                {
                    RaiseNetworkError(NetworkErrorCategory.ClientConnection, "EndAcceptTcpClient", e,
                        "Unexpected error during client acceptance");
                }
            }

            private void BeginReceiveData(TcpClient client)
            {
                if (client == null || !client.Connected)
                    return;

                try
                {
                    NetworkStream stream = client.GetStream();
                    byte[] buffer = clientBuffers[client];
                    int offset = clientBufferSizes[client];
                    int size = MaxBufferLength - offset;

                    stream.BeginRead(buffer, offset, size, new AsyncCallback(OnClientDataReceived), client);
                }
                catch (Exception e)
                {
                    RaiseNetworkError(NetworkErrorCategory.DataTransmission, "BeginRead", e,
                        $"Client: {client?.Client?.RemoteEndPoint}");
                    RemoveClient(client);
                }
            }

            private void OnClientDataReceived(IAsyncResult ar)
            {
                TcpClient client = (TcpClient)ar.AsyncState;

                try
                {
                    NetworkStream stream = client.GetStream();
                    int bytesRead = stream.EndRead(ar);

                    if (bytesRead <= 0)
                    {
                        // Client disconnected
                        RemoveClient(client);
                        return;
                    }

                    byte[] buffer = clientBuffers[client];
                    int currentSize = clientBufferSizes[client] + bytesRead;
                    clientBufferSizes[client] = currentSize;

                    // Process received data
                    ProcessClientData(client);

                    // Continue reading
                    BeginReceiveData(client);
                }
                catch (Exception e)
                {
                    RaiseNetworkError(NetworkErrorCategory.DataTransmission, "EndRead", e,
                        $"Client: {client?.Client?.RemoteEndPoint}");
                    RemoveClient(client);
                }
            }

            private void ProcessClientData(TcpClient client)
            {
                byte[] buffer = clientBuffers[client];
                int dataSize = clientBufferSizes[client];

                while (dataSize >= FrameHeader.StructSize)
                {
                    FrameHeader header = FrameHeader.FromBytes(buffer);

                    if (header.protocolIdentifier != FrameHeaderMagicIdentifier)
                    {
                        RaiseNetworkError(NetworkErrorCategory.ProtocolViolation, "ProcessFrameData",
                            new InvalidDataException("Invalid protocol identifier"),
                            $"Client: {client?.Client?.RemoteEndPoint}");
                        RemoveClient(client);
                        return;
                    }

                    if (header.payloadLength < 0 || header.payloadLength > MaxPayloadLength)
                    {
                        RaiseNetworkError(NetworkErrorCategory.ProtocolViolation, "ProcessFrameData",
                            new InvalidDataException($"Invalid payload length: {header.payloadLength}"),
                            $"Client: {client?.Client?.RemoteEndPoint}");
                        RemoveClient(client);
                        return;
                    }

                    int totalMessageSize = FrameHeader.StructSize + header.payloadLength;

                    if (dataSize >= totalMessageSize)
                    {
                        if (messageReceivedCallback != null)
                        {
                            messageReceivedCallback(client, header.payloadType, buffer, FrameHeader.StructSize, header.payloadLength);
                        }

                        // Move remaining data to beginning of buffer
                        if (dataSize > totalMessageSize)
                        {
                            Array.Copy(buffer, totalMessageSize, buffer, 0, dataSize - totalMessageSize);
                        }

                        clientBufferSizes[client] = dataSize - totalMessageSize;
                        dataSize = clientBufferSizes[client];
                    }
                    else
                    {
                        // Need more data for complete message
                        break;
                    }
                }
            }


            private void RemoveClient(TcpClient client)
            {
                lock (clientsLock)
                {
                    if (clients.Contains(client))
                    {
                        clients.Remove(client);
                        clientBuffers.Remove(client);
                        clientBufferSizes.Remove(client);

                        try
                        {
                            client.GetStream().Close();
                            client.Close();
                        }
                        catch (Exception e)
                        {
                            RaiseNetworkError(NetworkErrorCategory.ResourceCleanup, "RemoveClient", e,
                                $"Client: {client?.Client?.RemoteEndPoint}");
                        }
                    }
                }
            }
            public bool HasConnectedClient()
            {
                lock (clientsLock)
                {
                    return clients.Count > 0;
                }
            }

            public void Broadcast(int payloadType, byte[] payload)
            {
                if (payload.Length > OVRNetwork.MaxPayloadLength)
                {
                    RaiseNetworkError(NetworkErrorCategory.DataTransmission, "Broadcast",
                        new ArgumentException($"Payload too long: {payload.Length} bytes"),
                        $"Maximum payload length is {OVRNetwork.MaxPayloadLength}");
                    return;
                }

                FrameHeader header = new FrameHeader();
                header.protocolIdentifier = FrameHeaderMagicIdentifier;
                header.payloadType = payloadType;
                header.payloadLength = payload.Length;

                byte[] headerBuffer = header.ToBytes();

                byte[] dataBuffer = new byte[headerBuffer.Length + payload.Length];
                headerBuffer.CopyTo(dataBuffer, 0);
                payload.CopyTo(dataBuffer, headerBuffer.Length);

                lock (clientsLock)
                {
                    foreach (TcpClient client in clients)
                    {
                        if (client.Connected)
                        {
                            try
                            {
                                client.GetStream().BeginWrite(dataBuffer, 0, dataBuffer.Length,
                                    new AsyncCallback(DoWriteDataCallback), client.GetStream());
                            }
                            catch (SocketException e)
                            {
                                RaiseNetworkError(NetworkErrorCategory.DataTransmission, "BeginWrite", e,
                                    $"Client: {client?.Client?.RemoteEndPoint}, Payload size: {payload.Length}");
                                client.GetStream().Close();
                                client.Close();
                            }
                        }
                    }
                }
            }

            private void DoWriteDataCallback(IAsyncResult ar)
            {
                NetworkStream stream = ar.AsyncState as NetworkStream;
                stream.EndWrite(ar);
            }
        }
        public class OVRNetworkTcpClient
        {
            public Action connectionStateChangedCallback;
            public Action<int, byte[], int, int> payloadReceivedCallback;

            // Network Error Event - Phase 1
            public event EventHandler<NetworkErrorEventArgs> NetworkErrorOccurred;

            private void RaiseNetworkError(NetworkErrorCategory category, string operation, Exception ex, string context = "")
            {
                NetworkErrorOccurred?.Invoke(this, new NetworkErrorEventArgs
                {
                    Category = category,
                    Operation = operation,
                    ErrorMessage = ex.Message,
                    Exception = ex,
                    Context = context,
                    Timestamp = DateTime.Now
                });
            }

            public enum ConnectionState
            {
                Disconnected,
                Connected,
                Connecting
            }

            public ConnectionState connectionState
            {
                get
                {
                    if (tcpClient == null)
                    {
                        return ConnectionState.Disconnected;
                    }
                    else
                    {
                        if (tcpClient.Connected)
                        {
                            return ConnectionState.Connected;
                        }
                        else
                        {
                            return ConnectionState.Connecting;
                        }
                    }
                }
            }

            public bool Connected
            {
                get { return connectionState == ConnectionState.Connected; }
            }

            TcpClient tcpClient = null;
            byte[] receiveBuffer = new byte[OVRNetwork.MaxBufferLength];
            int receiveBufferSize = 0;

            public void Connect(int listeningPort)
            {

                if (tcpClient == null)
                {
                    receiveBufferSize = 0;

                    string remoteAddress = "127.0.0.1";
                    tcpClient = new TcpClient(AddressFamily.InterNetwork);

                    tcpClient.BeginConnect(remoteAddress, listeningPort, new AsyncCallback(ConnectCallback), tcpClient);

                }
                if (connectionStateChangedCallback != null)
                {
                    connectionStateChangedCallback();
                }
            }

            void ConnectCallback(IAsyncResult ar)
            {
                try
                {
                    TcpClient client = ar.AsyncState as TcpClient;
                    client.EndConnect(ar);
                    BeginReceiveData();
                }
                catch (Exception e)
                {
                    RaiseNetworkError(NetworkErrorCategory.ClientConnection, "EndConnect", e,
                        "Remote address: 127.0.0.1, Connection failed during EndConnect");
                }

                if (connectionStateChangedCallback != null)
                {
                    connectionStateChangedCallback();
                }
            }

            public void Disconnect()
            {
                if (tcpClient != null)
                {
                    try
                    {
                        tcpClient.GetStream().Close();
                        tcpClient.Close();
                    }
                    catch (Exception e)
                    {
                        RaiseNetworkError(NetworkErrorCategory.ClientDisconnection, "Disconnect", e,
                            "Error during client disconnection");
                    }

                    tcpClient = null;

                    if (connectionStateChangedCallback != null)
                    {
                        connectionStateChangedCallback();
                    }
                }
            }

            // New method to send messages to server
            public void Send(int payloadType, byte[] payload)
            {
                if (tcpClient == null || !tcpClient.Connected)
                {
                    RaiseNetworkError(NetworkErrorCategory.ClientDisconnection, "Send",
                        new InvalidOperationException("Cannot send: client not connected"),
                        "Attempted to send data while client is disconnected");
                    return;
                }

                if (payload.Length > OVRNetwork.MaxPayloadLength)
                {
                    RaiseNetworkError(NetworkErrorCategory.DataTransmission, "Send",
                        new ArgumentException($"Payload too long: {payload.Length} bytes"),
                        $"Maximum payload length is {OVRNetwork.MaxPayloadLength}");
                    return;
                }

                FrameHeader header = new FrameHeader();
                header.protocolIdentifier = FrameHeaderMagicIdentifier;
                header.payloadType = payloadType;
                header.payloadLength = payload.Length;

                byte[] headerBuffer = header.ToBytes();
                byte[] dataBuffer = new byte[headerBuffer.Length + payload.Length];
                headerBuffer.CopyTo(dataBuffer, 0);
                payload.CopyTo(dataBuffer, headerBuffer.Length);

                try
                {
                    tcpClient.GetStream().BeginWrite(dataBuffer, 0, dataBuffer.Length,
                        new AsyncCallback(DoWriteDataCallback), tcpClient.GetStream());
                }
                catch (Exception e)
                {
                    RaiseNetworkError(NetworkErrorCategory.DataTransmission, "Send", e,
                        $"Payload type: {payloadType}, Size: {payload.Length}");
                    Disconnect();
                }
            }

            private void DoWriteDataCallback(IAsyncResult ar)
            {
                NetworkStream stream = ar.AsyncState as NetworkStream;
                try
                {
                    stream.EndWrite(ar);
                }
                catch (Exception e)
                {
                    RaiseNetworkError(NetworkErrorCategory.DataTransmission, "DoWriteDataCallback", e,
                        "Error during asynchronous write completion");
                    Disconnect();
                }
            }

            private void BeginReceiveData()
            {
                if (tcpClient == null || !tcpClient.Connected)
                    return;

                try
                {
                    NetworkStream stream = tcpClient.GetStream();
                    stream.BeginRead(receiveBuffer, receiveBufferSize,
                        OVRNetwork.MaxBufferLength - receiveBufferSize,
                        new AsyncCallback(OnDataReceived), null);
                }
                catch (Exception e)
                {
                    RaiseNetworkError(NetworkErrorCategory.DataTransmission, "BeginReceiveData", e,
                        "Error beginning asynchronous receive operation");
                    Disconnect();
                }
            }

            private void OnDataReceived(IAsyncResult ar)
            {
                try
                {
                    if (tcpClient == null || !tcpClient.Connected)
                        return;

                    NetworkStream stream = tcpClient.GetStream();
                    int bytesRead = stream.EndRead(ar);

                    if (bytesRead <= 0)
                    {
                        Disconnect();
                        return;
                    }

                    receiveBufferSize += bytesRead;


                    ProcessReceivedData();

                    // Continue reading
                    BeginReceiveData();
                }
                catch (Exception e)
                {
                    RaiseNetworkError(NetworkErrorCategory.DataTransmission, "OnDataReceived", e,
                        "Error processing received data");
                    Disconnect();
                }
            }

            private void ProcessReceivedData()
            {
                while (receiveBufferSize >= FrameHeader.StructSize)
                {
                    FrameHeader header = FrameHeader.FromBytes(receiveBuffer);

                    if (header.protocolIdentifier != OVRNetwork.FrameHeaderMagicIdentifier)
                    {
                        RaiseNetworkError(NetworkErrorCategory.ProtocolViolation, "ProcessReceivedData",
                            new InvalidDataException("Protocol identifier mismatch"),
                            "Received invalid frame header from server");
                        Disconnect();
                        return;
                    }

                    if (header.payloadLength < 0 || header.payloadLength > OVRNetwork.MaxPayloadLength)
                    {
                        RaiseNetworkError(NetworkErrorCategory.ProtocolViolation, "ProcessReceivedData",
                            new InvalidDataException($"Invalid payload length: {header.payloadLength}"),
                            "Sanity check failed for received data");
                        Disconnect();
                        return;
                    }

                    int totalMessageSize = FrameHeader.StructSize + header.payloadLength;

                    if (receiveBufferSize >= totalMessageSize)
                    {
                        // We have a complete message
                        if (payloadReceivedCallback != null)
                        {
                            payloadReceivedCallback(header.payloadType, receiveBuffer, FrameHeader.StructSize, header.payloadLength);
                        }

                        // Move remaining data to beginning of buffer
                        if (receiveBufferSize > totalMessageSize)
                        {
                            Array.Copy(receiveBuffer, totalMessageSize, receiveBuffer, 0, receiveBufferSize - totalMessageSize);
                        }

                        receiveBufferSize -= totalMessageSize;
                    }
                    else
                    {
                        // Need more data for complete message
                        break;
                    }
                }
            }
        }
    }
}
