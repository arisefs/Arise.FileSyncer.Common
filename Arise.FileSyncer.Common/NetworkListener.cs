using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Arise.FileSyncer.Common.Helpers;
using Arise.FileSyncer.Common.Security;
using Arise.FileSyncer.Core;
using Arise.FileSyncer.Serializer;

namespace Arise.FileSyncer.Common
{
    public class NetworkListener : IDisposable
    {
        public bool IsActive => isActive;
        public IPEndPoint LocalEndpoint => tcpListener?.LocalEndPoint as IPEndPoint;

        private readonly SyncerPeer syncerPeer;
        private readonly KeyConfig keyConfig;
        private readonly Socket tcpListener;
        private volatile bool isActive = false;

        public NetworkListener(SyncerPeer syncerPeer, KeyConfig keyConfig, AddressFamily addressFamily)
        {
            this.syncerPeer = syncerPeer;
            this.keyConfig = keyConfig;

            IPAddress address = NetworkHelper.GetLocalIPAddress(addressFamily);

            try
            {
                tcpListener = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
                tcpListener.Bind(new IPEndPoint(address, 0));
                tcpListener.Listen();

                Task.Run(ConnectionAccepter);
            }
            catch (Exception ex)
            {
                Log.Error($"{this}: Failed to create the listener on {address}: {ex.Message}");
                return;
            }
        }

        public void Stop()
        {
            try
            {
                tcpListener?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error($"{this}: Failed to stop the listener: {ex.Message}");
            }
        }

        private async Task ConnectionAccepter()
        {
            isActive = true;

            while (true)
            {
                NetworkStream netStream;

                try
                {
                    Socket socket = await tcpListener.AcceptAsync();
                    netStream = new NetworkStream(socket, true);
                }
                catch (Exception ex)
                {
                    Log.Verbose($"{this}: Failed to accept socket: {ex.Message}");
                    break;
                }

                try
                {
                    Log.Info($"{this}: Accepting connection...");
                    Guid remoteDeviceId = netStream.ReadGuid();
                    AddClientToSyncer(remoteDeviceId, netStream, keyConfig.KeyInfo);
                }
                catch (Exception ex)
                {
                    Log.Verbose($"{this}: Failed to accept connection: {ex.Message}");
                    await netStream.DisposeAsync();
                    continue;
                }
                    
            }

            isActive = false;
        }

        public void Connect(Guid id, IPAddress address, int port)
        {
            Socket socket = null;
            Log.Info($"{this}: Connecting to {address}:{port}...");

            try
            {
                socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                if (!socket.ConnectAsync(new IPEndPoint(address, port)).Wait(5000))
                {
                    throw new Exception("Failed to connect. Timeout.");
                }

                var netStream = new NetworkStream(socket, true);
                netStream.WriteAFS(syncerPeer.Settings.DeviceId);
                AddClientToSyncer(id, netStream, null);
            }
            catch (Exception ex)
            {
                Log.Verbose($"{this}: Failed to connect to {address}:{port} - {ex.Message}");

                // Release resources on error
                socket?.Dispose();
            }
        }

        private void AddClientToSyncer(Guid remoteDeviceId, NetworkStream netStream, KeyInfo keyInfo)
        {
            try
            {
                var connection = new NetworkConnection(netStream, remoteDeviceId, keyInfo);

                if (!syncerPeer.AddConnection(connection))
                {
                    Log.Verbose($"{this}: Connection has not been added");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"{this}: Failed to add connection: {ex.Message}");
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
                    Stop();
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
