using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using Arise.FileSyncer.Core;
using Arise.FileSyncer.Serializer;

namespace Arise.FileSyncer.Common
{
    public class SyncerConfig
    {
        public class ConfigStorage : IBinarySerializable
        {
            public SyncerPeerSettings PeerSettings { get; set; } = new SyncerPeerSettings();
            public KeyValuePair<Guid, Guid>[] DeviceKeys { get; set; } = Array.Empty<KeyValuePair<Guid, Guid>>();
            public KeyValuePair<Guid, SyncProfile>[] Profiles { get; set; } = Array.Empty<KeyValuePair<Guid, SyncProfile>>();
            public AddressFamily ListenerAddressFamily { get; set; }
            public int DiscoveryPort { get; set; }

            public void Deserialize(Stream stream)
            {
                PeerSettings = stream.Read<SyncerPeerSettings>();

                int deviceKeysLength = stream.ReadInt32();
                List<KeyValuePair<Guid, Guid>> deviceKeysList = new();
                for (int i = 0; i < deviceKeysLength; i++)
                {
                    Guid key = stream.ReadGuid();
                    Guid value = stream.ReadGuid();
                    deviceKeysList.Add(new(key, value));
                }
                DeviceKeys = deviceKeysList.ToArray();

                int profilesLength = stream.ReadInt32();
                List<KeyValuePair<Guid, SyncProfile>> profilesList = new();
                for (int i = 0; i < profilesLength; i++)
                {
                    Guid key = stream.ReadGuid();
                    SyncProfile value = stream.Read<SyncProfile>();
                    profilesList.Add(new(key, value));
                }
                Profiles = profilesList.ToArray();

                ListenerAddressFamily = (AddressFamily)stream.ReadInt32();
                DiscoveryPort = stream.ReadInt32();
            }

            public void Serialize(Stream stream)
            {
                stream.WriteAFS(PeerSettings);

                stream.WriteAFS(DeviceKeys.Length);
                foreach (var kvp in DeviceKeys)
                {
                    stream.WriteAFS(kvp.Key);
                    stream.WriteAFS(kvp.Value);
                }

                stream.WriteAFS(Profiles.Length);
                foreach (var kvp in Profiles)
                {
                    stream.WriteAFS(kvp.Key);
                    stream.WriteAFS(kvp.Value);
                }

                stream.WriteAFS((int)ListenerAddressFamily);
                stream.WriteAFS(DiscoveryPort);
            }
        }

        public AddressFamily ListenerAddressFamily { get; set; }
        public int DiscoveryPort { get; set; }

        private const string Filename = "syncer";
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
        public LoadResult Load(SyncerPeerSettings refSettings, out ConfigStorage configStorate)
        {
            ConfigStorage? config = null;

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
                configStorate = config;
                return result;
            }

            // If loading the config file failed
            configStorate = GetDefaultConfig(refSettings);
            ApplyConfig(configStorate);
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
