using System;
using System.IO;
using System.Net.Sockets;
using Arise.FileSyncer.Core;

namespace Arise.FileSyncer.Common
{
    public class SyncerConfig
    {
        private const AddressFamily DefaultListenerAddressFamily = AddressFamily.InterNetwork;
        private const int DefaultDiscoveryPort = 13957;

        public delegate string DelegateGetConfigFolderPath();
        public static DelegateGetConfigFolderPath GetConfigFolderPath = DefaultGetConfigFolderPath;

        public SyncerPeerSettings PeerSettings
        {
            get => config.PeerSettings;
            set => config.PeerSettings = value;
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

        private readonly string filePath;
        private ConfigStorage config;

        public SyncerConfig()
        {
            config = new ConfigStorage();
            filePath = GetConfigFilePath();

            ListenerAddressFamily = DefaultListenerAddressFamily;
            DiscoveryPort = DefaultDiscoveryPort;
        }

        /// <summary>
        /// Saves the config to the disk
        /// </summary>
        public bool Save()
        {
            return SaveManager.Save(config, filePath);
        }

        /// <summary>
        /// Loads the config from the disk
        /// </summary>
        public bool Load()
        {
            if (SaveManager.Load(ref config, filePath))
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

            return false;
        }

        private static string GetConfigFilePath()
        {
            string configFolder = GetConfigFolderPath();
            const string fileName = "config.json";

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
