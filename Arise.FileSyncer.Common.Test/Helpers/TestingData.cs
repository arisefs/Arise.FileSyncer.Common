using System;
using System.IO;
using Arise.FileSyncer.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arise.FileSyncer.Common.Test.Helpers
{
    internal static class TestingData
    {
        public static void CreateTestDirectory(byte index)
        {
            string path = GetTestDirectoryName(index);

            // Delete Directory
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch
            {
                Assert.Fail("Failed to delete test directory!");
            }


            // Generate data or create empty directory
            if (index == 0)
            {
                GenerateTestData(path);
            }
            else
            {
                ClearTestData(path);
            }
        }

        public static string GetTestDirectoryName(byte index)
        {
            return $"AFS_SyncDir_{index}";
        }

        public static string GetTestDirectory(byte index)
        {
            try
            {
                DirectoryInfo info = new(GetTestDirectoryName(index));
                return PathHelper.GetCorrect(info.FullName, true);
            }
            catch (Exception)
            {
                throw new Exception("Failed to get Test Directory");
            }
        }

        private static void ClearTestData(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }

                Directory.CreateDirectory(path);
            }
            catch
            {
                Log.Error("Failed to clear test directory!");
            }
        }

        private static void GenerateTestData(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                CreateContent(path);
                CreateContent(Path.Combine(path, "Sub Directory 0"));
                CreateContent(Path.Combine(path, "Sub Directory 0", "Sub Directory 0"));
            }
            catch
            {
                Log.Error("Failed to generate test directory data!");
            }
        }

        private static void CreateContent(string path)
        {
            for (int i = 0; i < 5; i++)
            {
                string dirPath = Path.Combine(path, $"Sub Directory {i}");
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                string filePath = Path.Combine(path, $"Sub File {i}.txt");
                if (!File.Exists(filePath))
                {
                    using StreamWriter writer = File.CreateText(filePath);
                    for (int j = 0; j < 50 + i; j++)
                    {
                        writer.Write("Some data");
                    }
                }
            }
        }
    }
}
