using System.IO;

namespace Arise.FileSyncer.Common
{
    public static class SaveFileUtility
    {
        private static readonly object saveLock = new();

        /// <summary>
        /// Loads the file as Json into a specified object
        /// </summary>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <param name="obj">Object to load the data into</param>
        /// <param name="path">Path to the file</param>
        /// <returns>Does succeeded</returns>
        public static bool Load<T>(string path, ref T obj) where T : class
        {
            try
            {
                string json = null;

                lock (saveLock)
                {
                    json = File.ReadAllText(path);
                }

                obj = Json.Deserialize<T>(json);
            }
            catch { return false; }

            return true;
        }

        /// <summary>
        /// Saves the object as a Json file
        /// </summary>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <param name="obj">Object to save</param>
        /// <param name="path">Path to the file</param>
        /// <returns>Does succeeded</returns>
        public static bool Save<T>(string path, T obj) where T : class
        {
            string json = Json.Serialize(obj);

            try
            {
                string tempPath = path + ".tmp";

                lock (saveLock)
                {
                    using (var writer = new StreamWriter(tempPath, false))
                    {
                        writer.Write(json);
                    }

                    if (File.Exists(path)) File.Delete(path);
                    File.Move(tempPath, path);
                }
            }
            catch { return false; }

            return true;
        }
    }
}
