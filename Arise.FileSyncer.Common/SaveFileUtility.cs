using System.IO;
using System.Text.Json;

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
                byte[] jsonUtf8Bytes = null;

                lock (saveLock)
                {
                    jsonUtf8Bytes = File.ReadAllBytes(path);
                }

                var utf8Reader = new Utf8JsonReader(jsonUtf8Bytes);
                obj = JsonSerializer.Deserialize<T>(ref utf8Reader);
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
            try
            {
                byte[] jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(obj);
                string tempPath = path + ".tmp";

                lock (saveLock)
                {
                    // Write the data into a temp file
                    File.WriteAllBytes(tempPath, jsonUtf8Bytes);

                    // Move the temp file to the main location
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(tempPath, path);
                }
            }
            catch { return false; }

            return true;
        }
    }
}
