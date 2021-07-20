using System;
using System.IO;
using Arise.FileSyncer.Common.Test.Helpers;
using Arise.FileSyncer.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arise.FileSyncer.Common.Test
{
    [TestClass]
    public class DiscoveryTest
    {
        [TestMethod]
        public void MessageTest()
        {
            var peerS = new TestingPeer(0);
            var message = peerS.discovery.CreateDiscoveryMessage();
            Assert.AreEqual(NetworkDiscovery.NetVersion, BitConverter.ToInt64(message.AsSpan()[..8]));
        }

        [TestMethod]
        public void MessageTestRead()
        {
            var peerS = new TestingPeer(0);
            var message = peerS.discovery.CreateDiscoveryMessage();
            using var stream = new MemoryStream(message, false);
            Assert.AreEqual(NetworkDiscovery.NetVersion, stream.ReadInt64());
        }
    }
}
