using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Arise.FileSyncer.Serializer;

namespace Arise.FileSyncer.Common
{
    public static class SaveFileUtility
    {
        private static readonly object saveLock = new();

        /// <summary>
        /// Loads the file as IBinarySerializable into a specified object
        /// </summary>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <param name="obj">Object to load the data into</param>
        /// <param name="path">Path to the file</param>
        /// <returns>Success</returns>
        public static bool Load<T>(string path, [NotNullWhen(returnValue: true)] ref T? obj) where T : IBinarySerializable, new()
        {
            try
            {
                obj ??= new T();

                lock (saveLock)
                {
                    using var stream = File.OpenRead(path);
                    obj.Deserialize(stream);
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Verbose($"SaveFile load failed: {ex}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Saves the object as a IBinarySerializable file
        /// </summary>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <param name="obj">Object to save</param>
        /// <param name="path">Path to the file</param>
        /// <returns>Success</returns>
        public static bool Save<T>(string path, T obj) where T : IBinarySerializable
        {
            try
            {
                string tempPath = path + ".tmp";

                lock (saveLock)
                {
                    // Write the data into a temp file
                    using var stream = File.Open(tempPath, FileMode.Create);
                    obj.Serialize(stream);
                    stream.Flush();
                    stream.Close();

                    // Move the temp file to the main location
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(tempPath, path);
                }
            }
            catch (Exception ex)
            {
                Log.Verbose($"SaveFile save failed: {ex}");
                return false;
            }

            return true;
        }
    }
}
