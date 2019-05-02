using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Arise.FileSyncer.Core;
using Arise.FileSyncer.Core.Serializer;

namespace Arise.FileSyncer.Common
{
    public class NetworkDiscovery : IDisposable
    {
        public bool IsActive => isActive;

        private readonly SyncerConfig syncerConfig;
        private readonly SyncerPeer syncerPeer;
        private readonly NetworkListener listener;

        private Socket discoverySocket;
        private IPEndPoint sendEndPoint;
        private IPEndPoint receiveEndPoint;
        private byte[] message;
        private volatile bool isActive = false;
        private Task task = null;

        private const long NetVersion = 2;

        public NetworkDiscovery(SyncerConfig syncerConfig, SyncerPeer syncerPeer, NetworkListener listener)
        {
            this.syncerConfig = syncerConfig;
            this.syncerPeer = syncerPeer;
            this.listener = listener;

            UpdateEndPoints();
            UpdateMessage();
            CreateSocket();

            task = Task.Factory.StartNew(DiscoveryListener, TaskCreationOptions.LongRunning);
        }

        public bool SendDiscoveryMessage()
        {
            try
            {
                discoverySocket.SendTo(message, sendEndPoint);
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
                discoverySocket.Bind(receiveEndPoint);
                byte[] buffer = new byte[message.Length];
                isActive = true;

                while (true)
                {
                    EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    int read = discoverySocket.ReceiveFrom(buffer, ref remoteEndPoint);

                    if (read != buffer.Length)
                    {
                        Log.Warning($"{this}: received incorrect amount of data");
                        continue;
                    }

                    using (MemoryStream readStream = new MemoryStream(buffer, false))
                    {
                        // Check netVersion
                        long remoteNetVersion = readStream.ReadInt64();
                        if (remoteNetVersion != NetVersion) continue;

                        // Check DeviceId
                        Guid remoteDeviceId = readStream.ReadGuid();
                        if (remoteDeviceId == syncerConfig.PeerSettings.DeviceId) continue;

                        if (syncerPeer.DoesConnectionExist(remoteDeviceId)) continue;

                        // Get listener address
                        var listenerIP = new IPAddress(readStream.ReadByteArray());
                        int listenerPort = readStream.ReadInt32();

                        // Connect to target
                        Log.Info($"{this}: Found discovery target!");
                        listener.Connect(remoteDeviceId, listenerIP, listenerPort);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Verbose($"{this}: {ex.Message}");
            }

            isActive = false;
        }

        private void CreateSocket()
        {
            if (discoverySocket != null)
            {
                discoverySocket.Dispose();
            }

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
                Log.Verbose($"{this}: {ex.Message}");
            }
        }

        private void UpdateEndPoints()
        {
            sendEndPoint = new IPEndPoint(IPAddress.Broadcast, syncerConfig.DiscoveryPort);
            receiveEndPoint = new IPEndPoint(IPAddress.Any, syncerConfig.DiscoveryPort);
        }

        private void UpdateMessage()
        {
            using (MemoryStream writeStream = new MemoryStream())
            {
                // netVersion has to be the first written data
                writeStream.Write(NetVersion);

                writeStream.Write(syncerConfig.PeerSettings.DeviceId);
                writeStream.Write(listener.LocalEndpoint.Address.GetAddressBytes());
                writeStream.Write(listener.LocalEndpoint.Port);

                message = writeStream.ToArray();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (discoverySocket != null)
                    {
                        discoverySocket.Dispose();
                        discoverySocket = null;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
