using System;
using System.IO;
using Arise.FileSyncer;
using Arise.FileSyncer.Core.Helpers;

namespace Benchmark
{
    internal static class SyncData
    {
        public static void CreateDirectory(byte index)
        {
            var path = ToFullPath(GetDirectoryName(index));

            if (index == 0)
            {
                Directory.CreateDirectory(path);
                Log.Info($"Bench data source: {path}");
            }
            else
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
                Directory.CreateDirectory(path);
            }
        }

        public static string GetDirectory(byte index)
        {
            var path = ToFullPath(GetDirectoryName(index));
            return PathHelper.GetCorrect(path, true);
        }

        private static string GetDirectoryName(byte index)
        {
            var name = (index == 0) ? "Source" : "Target";
            return $"SyncData{name}";
        }

        private static string ToFullPath(string dir)
        {
            var root = new DirectoryInfo(dir);
            return root.FullName;
        }
    }
}
