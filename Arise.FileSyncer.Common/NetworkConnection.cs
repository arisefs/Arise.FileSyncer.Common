using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using Arise.FileSyncer.Common.Security;
using Arise.FileSyncer.Core;
using Arise.FileSyncer.Core.Serializer;

namespace Arise.FileSyncer.Common
{
    public class NetworkConnection : INetConnection
    {
        public Guid Id { get; } // Same as remote DeviceId
        public Stream SenderStream => encryptedStream;
        public Stream ReceiverStream => encryptedStream;

        private readonly TcpClient tcpClient;
        private readonly EncryptedStream encryptedStream;

        private const int keySize = 800;
        private const int keyChunkSize = 80;
        private const int keyChunkCount = keySize / keyChunkSize;

        public NetworkConnection(TcpClient tcpClient, Guid id, bool initiator)
        {
            this.tcpClient = tcpClient;
            Id = id;

            try
            {
                var stream = tcpClient.GetStream();

                if (initiator)
                {
                    using (var rsa = new RSACryptoServiceProvider(1024))
                    {
                        RSAParameters rsaKeyInfo = rsa.ExportParameters(false);

                        // Send public key
                        stream.Write(rsaKeyInfo.Modulus);
                        stream.Write(rsaKeyInfo.Exponent);
                        stream.Flush();

                        // Receive symmetric key
                        byte[] key = new byte[keySize];
                        for (int i = 0; i < keyChunkCount; i++)
                        {
                            var keyChunk = rsa.Decrypt(stream.ReadByteArray(), true);
                            Buffer.BlockCopy(keyChunk, 0, key, i * keyChunkSize, keyChunkSize);
                        }

                        // And IV
                        byte[] iv = rsa.Decrypt(stream.ReadByteArray(), true);

                        // Create encrypted stream
                        encryptedStream = new EncryptedStream(stream, key, iv);
                    }
                }
                else
                {
                    // Generate symmetric key
                    byte[] key = GenerateRandomArray(800);
                    byte[] iv = GenerateRandomArray(4);

                    // Receive public key
                    RSAParameters rsaKeyInfo = new RSAParameters
                    {
                        Modulus = stream.ReadByteArray(),
                        Exponent = stream.ReadByteArray()
                    };

                    using (var rsa = new RSACryptoServiceProvider())
                    {
                        rsa.ImportParameters(rsaKeyInfo);

                        // Encrypt and write symmetric key
                        byte[] keyChunk = new byte[keyChunkSize];
                        for (int i = 0; i < keyChunkCount; i++)
                        {
                            Buffer.BlockCopy(key, i * keyChunkSize, keyChunk, 0, keyChunkSize);
                            stream.Write(rsa.Encrypt(keyChunk, true));
                        }

                        // And IV
                        stream.Write(rsa.Encrypt(iv, true));
                        stream.Flush();
                    }

                    // Create encrypted stream
                    encryptedStream = new EncryptedStream(stream, key, iv);
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"{this}: Connection encryption initialization failed! Ex.:{ex.Message}");
                throw ex;
            }
        }

        private byte[] GenerateRandomArray(int length)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] random = new byte[length];
                rng.GetBytes(random);
                return random;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    encryptedStream.Dispose();
                    tcpClient.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
