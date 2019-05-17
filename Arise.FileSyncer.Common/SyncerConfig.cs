using System;
using System.IO;
using System.Net.Sockets;
using Arise.FileSyncer.Common.Security;
using Arise.FileSyncer.Core;

namespace Arise.FileSyncer.Common
{
    public class SyncerConfig
    {
        private const AddressFamily DefaultListenerAddressFamily = AddressFamily.InterNetwork;
        private const int DefaultDiscoveryPort = 13957;

        public delegate string DelegateGetConfigFolderPath();
        public static DelegateGetConfigFolderPath GetConfigFolderPath = DefaultGetConfigFolderPath;

        public static int RSAKeySize = 2048;

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

        public KeyInfo KeyInfo { get => keyInfo; }

        private readonly string configPath;
        private readonly string keyPath;

        private ConfigStorage config;
        private KeyInfo keyInfo;

        public SyncerConfig()
        {
            config = new ConfigStorage();
            configPath = GetConfigFilePath();
            keyPath = GetKeyFilePath();
        }

        /// <summary>
        /// Saves the config to the disk
        /// </summary>
        public bool Save()
        {
            return SaveManager.Save(config, configPath);
        }

        /// <summary>
        /// Saves the key info to the disk
        /// </summary>
        public bool SaveKey()
        {
            return SaveManager.Save(keyInfo, keyPath);
        }

        /// <summary>
        /// Loads the config from the disk
        /// </summary>
        public bool Load()
        {
            if (SaveManager.Load(ref config, configPath))
            {
                if (config.ListenerAddressFamily == AddressFamily.Unspecified)
                {
                    config.ListenerAddressFamily = DefaultListenerAddressFamily;
                }

                if (config.DiscoveryPort == 0)
                {
                    config.DiscoveryPort = DefaultDiscoveryPort;
                }

                return config.PeerSettings != null;
            }

            if (!SaveManager.Load(ref keyInfo, keyPath) || keyInfo == null || !keyInfo.Check())
            {
                keyInfo = KeyInfo.Generate(RSAKeySize);
            }

            return false;
        }

        public void Reset(SyncerPeerSettings peerSettings)
        {
            config.PeerSettings = peerSettings;
            ListenerAddressFamily = DefaultListenerAddressFamily;
            DiscoveryPort = DefaultDiscoveryPort;
            keyInfo = KeyInfo.Generate(RSAKeySize);
        }

        private static string GetConfigFilePath()
        {
            string configFolder = GetConfigFolderPath();
            const string fileName = "config.json";

            return Path.Combine(configFolder, fileName);
        }

        private static string GetKeyFilePath()
        {
            string configFolder = GetConfigFolderPath();
            const string fileName = "key.json";

            return Path.Combine(configFolder, fileName);
        }

        private static string DefaultGetConfigFolderPath()
        {
            string appdataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string path = Path.Combine(appdataLocal, "AriseFileSyncer");

            try
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create configuration folder: {ex.Message}");
            }

            return path;
        }

        public class ConfigStorage
        {
            public SyncerPeerSettings PeerSettings;
            public AddressFamily ListenerAddressFamily;
            public int DiscoveryPort;
        }
    }
}
