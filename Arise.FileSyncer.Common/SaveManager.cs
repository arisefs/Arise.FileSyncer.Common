using System.IO;

namespace Arise.FileSyncer.Common
{
    public static class SaveManager
    {
        private static readonly object saveLock = new object();

        /// <summary>
        /// Loads the file as Json into a specified object
        /// </summary>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <param name="obj">Object to load the data into</param>
        /// <param name="path">Path to the file</param>
        /// <returns>Does succeeded</returns>
        public static bool Load<T>(ref T obj, string path) where T : class
        {
            T loadedObj;

            try
            {
                string json = null;

                lock (saveLock)
                {
                    json = File.ReadAllText(path);
                }

                loadedObj = Json.Deserialize<T>(json);
            }
            catch { return false; }

            if (loadedObj == null) return false;

            obj = loadedObj;

            return true;
        }

        /// <summary>
        /// Saves the object as a Json file
        /// </summary>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <param name="obj">Object to save</param>
        /// <param name="path">Path to the file</param>
        /// <returns>Does succeeded</returns>
        public static bool Save<T>(T obj, string path) where T : class
        {
            if (obj == null) return false;

            string json = Json.Serialize(obj);

            try
            {
                string tempPath = path + ".tmp";

                lock (saveLock)
                {
                    using (StreamWriter writer = new StreamWriter(tempPath, false))
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
