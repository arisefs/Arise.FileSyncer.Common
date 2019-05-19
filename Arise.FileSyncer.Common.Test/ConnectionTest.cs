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
            Config.GetConfigFolderPath = () => "";
        }

        [TestMethod]
        public void KeyInfoLoad()
        {
            KeyConfig config = new KeyConfig();
            config.Reset();

            Assert.IsNotNull(config.KeyInfo);
        }

        [TestMethod]
        public void RsaCrypto()
        {
            const int data = 8723563;

            KeyConfig config = new KeyConfig();
            config.Reset();

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
