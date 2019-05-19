using System;
using System.IO;
using System.Net.Sockets;
using Arise.FileSyncer.Common.Security;
using Arise.FileSyncer.Core;

namespace Arise.FileSyncer.Common
{
    public class SyncerConfig
    {
        public SyncerPeerSettings PeerSettings
        {
            get => config.PeerSettings;
        }

        public AddressFamily ListenerAddressFamily
        {
            get => config.ListenerAddressFamily;
            set => config.ListenerAddressFamily = value;
        }

        public int DiscoveryPort
        {
            get => config.DiscoveryPort;
            set => config.DiscoveryPort = value;
        }

        private const string Filename = "config";
        private const int DefaultDiscoveryPort = 13957;
        private const AddressFamily DefaultListenerAddressFamily = AddressFamily.InterNetwork;

        private readonly string configPath;
        private ConfigStorage config;

        public SyncerConfig()
        {
            config = new ConfigStorage();
            configPath = Config.GetConfigFilePath(Filename);
        }

        /// <summary>
        /// Saves the config to the disk
        /// </summary>
        public bool SaveConfig()
        {
            return SaveManager.Save(config, configPath);
        }

        /// <summary>
        /// Loads the config from the disk
        /// </summary>
        public LoadResult LoadConfig(Func<SyncerPeerSettings> createPeerSettings)
        {
            if (SaveManager.Load(ref config, configPath))
            {
                LoadResult result = LoadResult.Loaded;

                if (config.ListenerAddressFamily == AddressFamily.Unspecified)
                {
                    config.ListenerAddressFamily = DefaultListenerAddressFamily;
                    result = LoadResult.Upgraded;
                }

                if (config.DiscoveryPort == 0)
                {
                    config.DiscoveryPort = DefaultDiscoveryPort;
                    result = LoadResult.Upgraded;
                }

                if (config.PeerSettings == null)
                {
                    config.PeerSettings = createPeerSettings();
                    result = LoadResult.Created;
                }
                else if (!config.PeerSettings.Verify())
                {
                    config.PeerSettings.Fix(createPeerSettings());
                    result = LoadResult.Upgraded;
                }

                return result;
            }

            ResetConfig(createPeerSettings());
            return LoadResult.Created;
        }

        

        public void ResetConfig(SyncerPeerSettings peerSettings)
        {
            config.PeerSettings = peerSettings;
            config.ListenerAddressFamily = DefaultListenerAddressFamily;
            config.DiscoveryPort = DefaultDiscoveryPort;
        }

        

        public class ConfigStorage
        {
            public SyncerPeerSettings PeerSettings;
            public AddressFamily ListenerAddressFamily;
            public int DiscoveryPort;
        }
    }
}
