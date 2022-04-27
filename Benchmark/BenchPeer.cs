using System;
using Arise.FileSyncer;
using Arise.FileSyncer.Common;
using Arise.FileSyncer.Core;
using Arise.FileSyncer.Core.Peer;

namespace Benchmark
{
    internal class BenchPeer : IDisposable
    {
        private const bool SupportTimestamp = true;
        private static readonly Guid sharedId = Guid.NewGuid();

        public readonly ProgressTracker ProgressTracker;

        private readonly NetworkDiscovery discovery;
        private readonly NetworkListener listener;
        private readonly SyncerConfig config;
        private readonly SyncerPeer peer;
        private readonly KeyConfig key;

        public BenchPeer(byte index)
        {
            config = new SyncerConfig { DiscoveryPort = 13966 };

            key = new KeyConfig();
            key.Reset();

            SyncData.CreateDirectory(index);

            // Local settings
            var localId = new Guid(new byte[] { 9, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, index });
            var settings = new SyncerPeerSettings(localId, $"TestPeer:{index}", SupportTimestamp);

            // Add remote device
            int remoteIndex = (index == 0) ? 1 : 0;
            Guid remoteId = new(new byte[] { 9, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)remoteIndex });
            Guid remoteKey = sharedId;
            DeviceKeyManager deviceKeys = new();
            deviceKeys.Add(remoteId, remoteKey);

            // Add profile
            ProfileManager profiles = new();
            profiles.AddProfile(sharedId, new SyncProfile()
            {
                Activated = true,
                AllowSend = index == 0,
                AllowReceive = index != 0,
                Key = sharedId,
                RootDirectory = SyncData.GetDirectory(index),
            });

            // Startup
            peer = new SyncerPeer(settings, deviceKeys, profiles);
            listener = new NetworkListener(peer, key, config.ListenerAddressFamily);
            discovery = new NetworkDiscovery(config, peer, listener);
            ProgressTracker = new ProgressTracker(peer, 1000, 2);

            // Handle events
            peer.Connections.ConnectionAdded += Peer_ConnectionAdded;
            peer.Connections.ConnectionRemoved += Peer_ConnectionRemoved;
        }

        public void SendDiscoveryMessage()
        {
            discovery.SendDiscoveryMessage();
        }

        private void Peer_ConnectionAdded(object? sender, ConnectionEventArgs e)
        {
            Log.Info("Connection Added!");
        }

        private void Peer_ConnectionRemoved(object? sender, ConnectionEventArgs e)
        {
            Log.Info("Connection Removed!");
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    listener.Dispose();
                    discovery.Dispose();
                    peer.Dispose();
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
