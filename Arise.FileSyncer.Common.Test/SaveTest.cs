using System;
using Arise.FileSyncer.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arise.FileSyncer.Common.Test
{
    [TestClass]
    public class SaveTest
    {
        private const string ProfileName = "Just some test profile";

        public SaveTest()
        {
            Config.GetConfigFolderPath = () => "./";
        }

        [TestMethod]
        public void SaveDataTest()
        {
            var config = new SyncerConfig();
            var settings = new SyncerPeerSettings(Guid.NewGuid(), "Test Device", true) { BufferSize = 12345 };
            var peer = new SyncerPeer(settings, null, null);
            peer.DeviceKeys.Add(Guid.NewGuid(), Guid.NewGuid());
            peer.DeviceKeys.Add(Guid.NewGuid(), Guid.NewGuid());
            peer.Profiles.AddProfile(Guid.NewGuid(), new SyncProfile() { Name = ProfileName });

            Assert.IsTrue(config.Save(peer));
            Assert.AreEqual(LoadResult.Loaded, config.Load(null, out var loadedConfig));

            // Test the values
            Assert.AreEqual(peer.Settings.BufferSize, loadedConfig.PeerSettings.BufferSize);
            Assert.AreEqual(2, loadedConfig.DeviceKeys.Length);
            Assert.AreEqual(1, loadedConfig.Profiles.Length);

            // Test the profile values
            var profile = loadedConfig.Profiles[0].Value;
            Assert.AreEqual(ProfileName, profile.Name);
        }
    }
}
