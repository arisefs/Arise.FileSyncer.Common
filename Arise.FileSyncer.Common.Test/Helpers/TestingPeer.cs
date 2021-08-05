using System;
using Arise.FileSyncer.Core;
using Arise.FileSyncer.Core.Peer;

namespace Arise.FileSyncer.Common.Test.Helpers
{
    internal class TestingPeer : IDisposable
    {
        private const bool SupportTimestamp = true;
        private static readonly Guid sharedId = Guid.NewGuid();

        public readonly NetworkDiscovery discovery;
        public readonly NetworkListener listener;
        public readonly SyncerConfig config;
        public readonly SyncerPeer peer;
        public readonly KeyConfig key;

        public TestingPeer(byte index)
        {
            config = new SyncerConfig { DiscoveryPort = 13965 };

            key = new KeyConfig();
            key.Reset();

            TestingData.CreateTestDirectory(index);

            Guid localId = new(new byte[] { 9, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, index });
            SyncerPeerSettings settings = new(localId, $"TestPeer:{index}", SupportTimestamp);

            int remoteIndex = (index == 0) ? 1 : 0;
            Guid remoteId = new(new byte[] { 9, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)remoteIndex });
            Guid remoteKey = sharedId;
            DeviceKeyManager deviceKeys = new();
            deviceKeys.Add(remoteId, remoteKey);

            ProfileManager profiles = new();
            profiles.AddProfile(sharedId, new SyncProfile()
            {
                Activated = true,
                AllowSend = index == 0,
                AllowReceive = index != 0,
                Key = sharedId,
                RootDirectory = TestingData.GetTestDirectory(index),
            });

            peer = new SyncerPeer(settings, deviceKeys, profiles);
            listener = new NetworkListener(peer, key, config.ListenerAddressFamily);
            discovery = new NetworkDiscovery(config, peer, listener);

            peer.Connections.ConnectionAdded += Peer_ConnectionAdded;
            peer.Connections.ConnectionRemoved += Peer_ConnectionRemoved;
        }

        public void SendDiscoveryMessage()
        {
            discovery.SendDiscoveryMessage();
        }

        private void Peer_ConnectionAdded(object sender, ConnectionEventArgs e)
        {
            Log.Info("Connection Added!");
        }

        private void Peer_ConnectionRemoved(object sender, ConnectionEventArgs e)
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
