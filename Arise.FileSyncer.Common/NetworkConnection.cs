using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using Arise.FileSyncer.Common.Security;
using Arise.FileSyncer.Core;
using Arise.FileSyncer.Serializer;

namespace Arise.FileSyncer.Common
{
    public class NetworkConnection : INetConnection
    {
        public Guid Id { get; } // Same as remote DeviceId
        public Stream SenderStream => encryptedStream;
        public Stream ReceiverStream => encryptedStream;

        private readonly TcpClient tcpClient;
        private readonly EncryptedStream encryptedStream;

        public NetworkConnection(TcpClient tcpClient, Guid id, KeyInfo keyInfo)
        {
            this.tcpClient = tcpClient;
            Id = id;

            try
            {
                var stream = tcpClient.GetStream();

                // If we have a keyinfo then we are the initiator
                if (keyInfo != null)
                {
                    using (var rsa = new RSACryptoServiceProvider())
                    {
                        rsa.ImportParameters(keyInfo.GetParameters());

                        // Send public key
                        stream.WriteAFS(keyInfo.Modulus);
                        stream.WriteAFS(keyInfo.Exponent);
                        stream.Flush();

                        // Receive and flip seeds
                        int writeSeed = BitConverter.ToInt32(rsa.Decrypt(stream.ReadByteArray(), true), 0);
                        int readSeed = BitConverter.ToInt32(rsa.Decrypt(stream.ReadByteArray(), true), 0);

                        // Create encrypted stream
                        encryptedStream = new EncryptedStream(stream, readSeed, writeSeed);
                    }
                }
                else
                {
                    // Generate seeds
                    var random = new FastRandom();
                    int readSeed = random.NextInt();
                    int writeSeed = random.NextInt();

                    // Receive public key
                    RSAParameters rsaKeyInfo = new RSAParameters
                    {
                        Modulus = stream.ReadByteArray(),
                        Exponent = stream.ReadByteArray()
                    };

                    using (var rsa = new RSACryptoServiceProvider())
                    {
                        rsa.ImportParameters(rsaKeyInfo);

                        // Encrypt and write seeds
                        stream.WriteAFS(rsa.Encrypt(BitConverter.GetBytes(readSeed), true));
                        stream.WriteAFS(rsa.Encrypt(BitConverter.GetBytes(writeSeed), true));
                        stream.Flush();
                    }

                    // Create encrypted stream
                    encryptedStream = new EncryptedStream(stream, readSeed, writeSeed);
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"{this}: Connection encryption initialization failed! Ex.:{ex.Message}");
                throw;
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
