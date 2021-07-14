using Arise.FileSyncer.Common.Security;

namespace Arise.FileSyncer.Common
{
    public class KeyConfig
    {
        public KeyInfo KeyInfo { get => keyInfo; }

        private const string Filename = "key";
        private readonly string keyPath;
        private readonly int rsaKeySize;

        private KeyInfo keyInfo;

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
            return SaveManager.Save(keyInfo, keyPath);
        }

        /// <summary>
        /// Loads the key from the disk
        /// </summary>
        /// <returns>Did it upgrade/create the key</returns>
        public LoadResult Load()
        {
            if (SaveManager.Load(ref keyInfo, keyPath))
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
