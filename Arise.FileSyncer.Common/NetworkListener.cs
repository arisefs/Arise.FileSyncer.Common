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
        public IPEndPoint LocalEndpoint => tcpListener?.LocalEndpoint as IPEndPoint;

        private readonly SyncerPeer syncerPeer;
        private readonly KeyConfig keyConfig;
        private readonly TcpListener tcpListener;
        private volatile bool isActive = false;

        public NetworkListener(SyncerPeer syncerPeer, KeyConfig keyConfig, AddressFamily addressFamily)
        {
            this.syncerPeer = syncerPeer;
            this.keyConfig = keyConfig;

            IPAddress address = NetworkHelper.GetLocalIPAddress(addressFamily);
            Log.Verbose($"{this}: Creating listener on {address}");

            try
            {
                tcpListener = new TcpListener(address, 0);
                tcpListener.Start();

                Task.Factory.StartNew(ConnectionAccepter, TaskCreationOptions.LongRunning);
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
                tcpListener?.Stop();
            }
            catch (Exception ex)
            {
                Log.Error($"{this}: Failed to stop the listener: {ex.Message}");
            }
        }

        private void ConnectionAccepter()
        {
            TcpClient client = null;
            isActive = true;

            try
            {
                while (true)
                {
                    client = tcpListener.AcceptTcpClient();
                    Guid remoteDeviceId = client.GetStream().ReadGuid();

                    Log.Info($"{this}: Accepting connection...");

                    AddClientToSyncer(remoteDeviceId, client, keyConfig.KeyInfo);
                    client = null;
                }
            }
            catch (Exception ex)
            {
                Log.Verbose($"{this}: Failed to accept connection: {ex.Message}");

                // Release resources on error
                client?.Dispose();
            }

            isActive = false;
        }

        public void Connect(Guid id, IPAddress address, int port)
        {
            TcpClient client = new();
            Log.Info($"{this}: Connecting to {address}:{port}...");

            try
            {
                if (!client.ConnectAsync(address, port).Wait(5000))
                {
                    throw new Exception("Failed to connect. Timeout.");
                }

                client.GetStream().WriteAFS(syncerPeer.Settings.DeviceId);
                AddClientToSyncer(id, client, null);
            }
            catch (Exception ex)
            {
                Log.Verbose($"{this}: Failed to connect to {address}:{port} - {ex.Message}");

                // Release resources on error
                client.Dispose();
            }
        }

        private void AddClientToSyncer(Guid remoteDeviceId, TcpClient client, KeyInfo keyInfo)
        {
            NetworkConnection connection = new(client, remoteDeviceId, keyInfo);

            try
            {
                if (!syncerPeer.AddConnection(connection))
                {
                    Log.Verbose($"{this}: Connection has not been added");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"{this}: Failed to add connection: {ex.Message}");

                // Release resources on error
                connection?.Dispose();
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
