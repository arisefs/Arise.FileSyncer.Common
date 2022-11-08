using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Arise.FileSyncer.Core;
using Arise.FileSyncer.Serializer;

namespace Arise.FileSyncer.Common
{
    public class NetworkDiscovery : IDisposable
    {
        public bool IsActive => isActive;

        private readonly SyncerConfig syncerConfig;
        private readonly SyncerPeer syncerPeer;
        private readonly NetworkListener listener;

        private Socket? discoverySocket;
        private IPEndPoint sendEndPoint = new(IPAddress.Any, 0); // Default value before update
        private IPEndPoint receiveEndPoint = new(IPAddress.Any, 0); // Default value before update
        private readonly byte[] message;
        private volatile bool isActive = false;
        private Task? task = null;

        public const long NetVersion = 4;

        public NetworkDiscovery(SyncerConfig syncerConfig, SyncerPeer syncerPeer, NetworkListener listener)
        {
            this.syncerConfig = syncerConfig;
            this.syncerPeer = syncerPeer;
            this.listener = listener;

            message = CreateDiscoveryMessage();
            UpdateEndPoints();
            CreateSocket();

            task = Task.Factory.StartNew(DiscoveryListener, TaskCreationOptions.LongRunning);
        }

        public bool SendDiscoveryMessage()
        {
            try
            {
                discoverySocket!.SendTo(message, sendEndPoint);
            }
            catch
            {
                Log.Error($"{this}: Failed to send discovery message");
                return false;
            }

            return true;
        }

        public void RefreshPort()
        {
            UpdateEndPoints();
            CreateSocket();

            try
            {
                task?.Wait(5000);
            }
            catch (Exception)
            {
                Log.Error($"{this}: Failed to shut down discovery task. (timeout)");
                return;
            }

            task = Task.Factory.StartNew(DiscoveryListener, TaskCreationOptions.LongRunning);
        }

        private void DiscoveryListener()
        {
            try
            {
                discoverySocket!.Bind(receiveEndPoint);
                byte[] buffer = new byte[message.Length];
                isActive = true;

                while (true)
                {
                    EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    int read = discoverySocket.ReceiveFrom(buffer, ref remoteEndPoint);

                    if (read != buffer.Length)
                    {
                        Log.Verbose($"{this}: Received incorrect amount of data. Ignoring connection...");
                        continue;
                    }

                    using var readStream = new MemoryStream(buffer, false);
                    // Check netVersion
                    long remoteNetVersion = readStream.ReadInt64();
                    if (remoteNetVersion != NetVersion)
                    {
                        Log.Verbose($"{this}: Discovery target has different NetVersion. Ignoring connection...");
                        continue;
                    }

                    // Check DeviceId
                    Guid remoteDeviceId = readStream.ReadGuid();
                    if (remoteDeviceId == syncerPeer.Settings.DeviceId) continue;

                    if (syncerPeer.Connections.DoesConnectionExist(remoteDeviceId)) continue;

                    // Don't connect if not paired and not pairing
                    if (!syncerPeer.AllowPairing)
                    {
                        if (!syncerPeer.DeviceKeys.ContainsId(remoteDeviceId))
                        {
                            Log.Verbose($"{this}: Device is not paired and self is not pairing. Ignoring connection...");
                            continue;
                        }
                    }

                    // Get listener address
                    var listenerIP = new IPAddress(readStream.ReadByteArray());
                    int listenerPort = readStream.ReadInt32();

                    // Connect to target
                    Log.Info($"{this}: Found discovery target! Connecting...");
                    listener.Connect(remoteDeviceId, listenerIP, listenerPort);
                }
            }
            catch (Exception ex)
            {
                Log.Verbose($"{this}: {nameof(DiscoveryListener)}: {ex.Message}");
            }

            isActive = false;
        }

        private void CreateSocket()
        {
            discoverySocket?.Dispose();

            try
            {
                discoverySocket = new Socket(syncerConfig.ListenerAddressFamily, SocketType.Dgram, ProtocolType.Udp)
                {
                    ExclusiveAddressUse = false
                };

                discoverySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                discoverySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
            catch (Exception ex)
            {
                Log.Verbose($"{this}: {nameof(CreateSocket)}: {ex.Message}");
            }
        }

        private void UpdateEndPoints()
        {
            sendEndPoint = new IPEndPoint(IPAddress.Broadcast, syncerConfig.DiscoveryPort);
            receiveEndPoint = new IPEndPoint(IPAddress.Any, syncerConfig.DiscoveryPort);
        }

        public byte[] CreateDiscoveryMessage()
        {
            using var writeStream = new MemoryStream();
            writeStream.WriteAFS(NetVersion); // netVersion has to be the first written data

            writeStream.WriteAFS(syncerPeer.Settings.DeviceId);
            writeStream.WriteAFS(listener.LocalEndpoint.Address.GetAddressBytes());
            writeStream.WriteAFS(listener.LocalEndpoint.Port);

            return writeStream.ToArray();
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    discoverySocket?.Dispose();
                    discoverySocket = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
