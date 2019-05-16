using System;
using Arise.FileSyncer.Core;

namespace Arise.FileSyncer.Common.Test.Helpers
{
    internal class TestingPeer : IDisposable
    {
        private static readonly Guid sharedId = Guid.NewGuid();

        public readonly NetworkDiscovery discovery;
        public readonly NetworkListener listener;
        public readonly SyncerConfig config;
        public readonly SyncerPeer peer;

        public TestingPeer(byte index)
        {
            TestingData.CreateTestDirectory(index);

            Guid localId = new Guid(new byte[] { 9, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, index });
            config = new SyncerConfig();
            config.Reset(new SyncerPeerSettings(localId, $"TestPeer:{index}"));
            config.DiscoveryPort = 13965;

            int remoteIndex = (index == 0) ? 1 : 0;
            Guid remoteId = new Guid(new byte[] { 9, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)remoteIndex });
            Guid key = sharedId;
            config.PeerSettings.DeviceKeys.TryAdd(remoteId, key);

            config.PeerSettings.Profiles.TryAdd(sharedId, new SyncProfile.Creator()
            {
                Activated = true,
                AllowSend = index == 0,
                AllowReceive = index != 0,
                Key = sharedId,
                RootDirectory = TestingData.GetTestDirectory(index),
                Plugin = "TestPlugin"
            });

            peer = new SyncerPeer(config.PeerSettings);
            listener = new NetworkListener(config, peer.AddConnection);
            discovery = new NetworkDiscovery(config, peer, listener);

            peer.ConnectionAdded += Peer_ConnectionAdded;
            peer.ConnectionRemoved += Peer_ConnectionRemoved;
            peer.Plugins.Add(new TestingPlugin());
        }

        public void SendDiscoveryMessage()
        {
            discovery.SendDiscoveryMessage();
        }

        private void Peer_ConnectionAdded(object sender, ConnectionAddedEventArgs e)
        {
            Log.Info("Connection Added!");
        }

        private void Peer_ConnectionRemoved(object sender, ConnectionRemovedEventArgs e)
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
        }
        #endregion
    }
}
