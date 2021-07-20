using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Arise.FileSyncer.Core;

namespace Arise.FileSyncer.Common
{
    public class SyncerConfig
    {
        public class ConfigStorage
        {
            public SyncerPeerSettings PeerSettings { get; set; }
            public KeyValuePair<Guid, Guid>[] DeviceKeys { get; set; }
            public KeyValuePair<Guid, SyncProfile>[] Profiles { get; set; }
            public AddressFamily ListenerAddressFamily { get; set; }
            public int DiscoveryPort { get; set; }
        }

        public AddressFamily ListenerAddressFamily { get; set; }
        public int DiscoveryPort { get; set; }

        private const string Filename = "config";
        private const int DefaultDiscoveryPort = 13957;
        private const AddressFamily DefaultListenerAddressFamily = AddressFamily.InterNetwork;

        private readonly string configPath;

        public SyncerConfig()
        {
            configPath = Config.GetConfigFilePath(Filename);

            ListenerAddressFamily = DefaultListenerAddressFamily;
            DiscoveryPort = DefaultDiscoveryPort;
        }

        /// <summary>
        /// Saves the config to the disk
        /// </summary>
        public bool Save(SyncerPeer peer)
        {
            return SaveFileUtility.Save(configPath, new ConfigStorage
            {
                PeerSettings = peer.Settings,
                DeviceKeys = peer.DeviceKeys.Snapshot(),
                Profiles = peer.Profiles.Snapshot(),
                ListenerAddressFamily = ListenerAddressFamily,
                DiscoveryPort = DiscoveryPort,
            });
        }

        /// <summary>
        /// Saves the config to the disk
        /// </summary>
        public bool Save(ConfigStorage config)
        {
            return SaveFileUtility.Save(configPath, config);
        }

        /// <summary>
        /// Loads the config from the disk
        /// </summary>
        public LoadResult Load(SyncerPeerSettings refSettings, out ConfigStorage config)
        {
            config = null;

            if (SaveFileUtility.Load(configPath, ref config))
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
                    config.PeerSettings = refSettings;
                    result = LoadResult.Created;
                }
                else if (!config.PeerSettings.Verify())
                {
                    config.PeerSettings.Fix(refSettings);
                    result = LoadResult.Upgraded;
                }

                ApplyConfig(config);
                return result;
            }

            // If loading the config file failed
            config = GetDefaultConfig(refSettings);
            ApplyConfig(config);
            return LoadResult.Created;
        }

        public void ApplyConfig(ConfigStorage config)
        {
            ListenerAddressFamily = config.ListenerAddressFamily;
            DiscoveryPort = config.DiscoveryPort;
        }

        public static ConfigStorage GetDefaultConfig(SyncerPeerSettings peerSettings)
        {
            return new ConfigStorage
            {
                PeerSettings = peerSettings,
                DeviceKeys = Array.Empty<KeyValuePair<Guid, Guid>>(),
                Profiles = Array.Empty<KeyValuePair<Guid, SyncProfile>>(),
                ListenerAddressFamily = DefaultListenerAddressFamily,
                DiscoveryPort = DefaultDiscoveryPort,
            };
        }
    }
}
