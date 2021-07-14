using System;
using System.IO;

namespace Arise.FileSyncer.Common
{
    public static class Config
    {
        public delegate string DelegateGetConfigFolderPath();

#pragma warning disable CA2211 // Non-constant fields should not be visible
        public static DelegateGetConfigFolderPath GetConfigFolderPath = DefaultGetConfigFolderPath;
#pragma warning restore CA2211 // Non-constant fields should not be visible

        public static string GetConfigFilePath(string name)
        {
            string configFolder = GetConfigFolderPath();
            return Path.Combine(configFolder, name + ".json");
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
    }

    public enum LoadResult
    {
        Loaded,
        Upgraded,
        Created,
    }
}
