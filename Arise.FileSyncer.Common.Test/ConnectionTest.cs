using System;
using System.Collections.Generic;
using System.Security.Cryptography;
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

        [TestMethod]
        public void RsaCrypto()
        {
            const int data = 8723563;

            SyncerConfig config = new SyncerConfig();
            config.Reset(new Core.SyncerPeerSettings());

            using (var rsaEncryptor = new RSACryptoServiceProvider())
            using (var rsaDecryptor = new RSACryptoServiceProvider())
            {
                rsaDecryptor.ImportParameters(config.KeyInfo.GetParameters());
                rsaEncryptor.ImportParameters(new RSAParameters
                {
                    Modulus = config.KeyInfo.Modulus,
                    Exponent = config.KeyInfo.Exponent
                });

                byte[] encrypted = rsaEncryptor.Encrypt(BitConverter.GetBytes(data), true);
                int decrypted = BitConverter.ToInt32(rsaDecryptor.Decrypt(encrypted, true), 0);

                Assert.AreEqual(data, decrypted);
            }
        }
    }
}
