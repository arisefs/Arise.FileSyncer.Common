using System.IO;
using System.Threading;
using Arise.FileSyncer.Common.Test.Helpers;
using Arise.FileSyncer.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arise.FileSyncer.Common.Test
{
    [TestClass]
    public class IntegrationTest
    {
        public IntegrationTest()
        {
            Log.Info("Arise FileSyncer Test App");
            SyncerConfig.GetConfigFolderPath = () => "";

            var peerS = new TestingPeer(0);
            var peerR = new TestingPeer(1);

            // Busy-Wait for the necessary systems to start up
            int waitCounter = 0;
            while (!peerS.listener.IsActive || !peerR.discovery.IsActive)
            {
                Assert.IsTrue(500 > waitCounter++);
                Thread.Sleep(10);
            }

            // Connect the testing peers to each other via discovery lookup
            peerS.SendDiscoveryMessage();

            // Wait for the sync process to finish
            waitCounter = 0;
            while (peerS.peer.GetConnectionCount() == 0 || peerS.peer.IsSyncing()
                || peerR.peer.GetConnectionCount() == 0 || peerR.peer.IsSyncing())
            {
                Assert.IsTrue(50 > waitCounter++);
                // Due to threading issues(?) have to wait for some time or the test will fail
                Thread.Sleep(100);
            }

            Log.Info("Arise FileSyncer - Sync Finished");

            peerR.Dispose();
            peerS.Dispose();
        }

        [TestMethod]
        public void ValidateDirectories()
        {
            string[] sourceDirectories = Directory.GetDirectories("AFS_SyncDir_0", "*", SearchOption.AllDirectories);
            string[] targetDirectories = Directory.GetDirectories("AFS_SyncDir_1", "*", SearchOption.AllDirectories);

            Assert.AreEqual(sourceDirectories.Length, targetDirectories.Length);

            for (int i = 0; i < sourceDirectories.Length; i++)
            {
                Assert.AreEqual(sourceDirectories[i].Substring(14), targetDirectories[i].Substring(14));
            }
        }

        [TestMethod]
        public void ValidateFiles()
        {
            string[] sourceFiles = Directory.GetFiles("AFS_SyncDir_0", "*", SearchOption.AllDirectories);
            string[] targetFiles = Directory.GetFiles("AFS_SyncDir_1", "*", SearchOption.AllDirectories);

            Assert.AreEqual(sourceFiles.Length, targetFiles.Length);

            for (int i = 0; i < sourceFiles.Length; i++)
            {
                // Adding .dat to end cause of plugin
                Assert.AreEqual(sourceFiles[i].Substring(14) + ".dat", targetFiles[i].Substring(14));

                FileInfo sourceFile = new FileInfo(sourceFiles[i]);
                FileInfo targetFile = new FileInfo(targetFiles[i]);

                sourceFile.Refresh();
                targetFile.Refresh();

                Assert.AreEqual(sourceFile.Length, targetFile.Length);
                Assert.AreEqual(sourceFile.CreationTimeUtc, targetFile.CreationTimeUtc);
                Assert.AreEqual(sourceFile.LastWriteTimeUtc, targetFile.LastWriteTimeUtc);
            }
        }
    }
}
