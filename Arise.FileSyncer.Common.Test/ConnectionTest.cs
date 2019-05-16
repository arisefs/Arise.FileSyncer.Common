using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arise.FileSyncer.Common.Test
{
    [TestClass]
    public class ConnectionTest
    {
        public ConnectionTest()
        {
            SyncerConfig.GetConfigFolderPath = () => "";
        }

        [TestMethod]
        public void KeyInfoLoad()
        {
            SyncerConfig config = new SyncerConfig();
            config.Reset(new Core.SyncerPeerSettings());

            Assert.IsNotNull(config.KeyInfo);
        }
    }
}
