using Arise.FileSyncer.Common.Security;

namespace Arise.FileSyncer.Common
{
    public class KeyConfig
    {
        public KeyInfo? KeyInfo { get => keyInfo; }

        private const string Filename = "key";
        private readonly string keyPath;
        private readonly int rsaKeySize;

        private KeyInfo? keyInfo;

        public KeyConfig() : this(2048) { }

        public KeyConfig(int keySize)
        {
            rsaKeySize = keySize;
            keyPath = Config.GetConfigFilePath(Filename);
        }

        /// <summary>
        /// Saves the key to the disk
        /// </summary>
        public bool Save()
        {
            if (keyInfo != null)
            {
                return SaveFileUtility.Save(keyPath, keyInfo);
            }
            else
            {
                Log.Error("Failed to save KeyConfig, KeyInfo is null");
                return false;
            }
        }

        /// <summary>
        /// Loads the key from the disk
        /// </summary>
        /// <returns>Did it upgrade/create the key</returns>
        public LoadResult Load()
        {
            if (SaveFileUtility.Load(keyPath, ref keyInfo))
            {
                LoadResult result = LoadResult.Loaded;

                if (keyInfo == null || !keyInfo.Check())
                {
                    keyInfo = KeyInfo.Generate(rsaKeySize);
                    result = LoadResult.Upgraded;
                }

                return result;
            }

            keyInfo = KeyInfo.Generate(rsaKeySize);
            return LoadResult.Created;
        }

        /// <summary>
        /// Generates a new key
        /// </summary>
        public void Reset()
        {
            keyInfo = KeyInfo.Generate(rsaKeySize);
        }
    }
}
