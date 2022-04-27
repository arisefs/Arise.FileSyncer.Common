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

        private readonly NetworkStream netStream;
        private readonly EncryptedStream encryptedStream;

        public NetworkConnection(NetworkStream netStream, Guid id, KeyInfo keyInfo)
        {
            this.netStream = netStream;
            Id = id;

            SetupSocket(netStream.Socket);
            
            Log.Warning($"NetStream SendBuffer: {netStream.Socket.SendBufferSize}");
            Log.Warning($"NetStream ReceiveBuffer: {netStream.Socket.ReceiveBufferSize}");

            try
            {
                var stream = netStream;

                // If we have a keyinfo then we are the initiator
                if (keyInfo != null)
                {
                    using var rsa = new RSACryptoServiceProvider();
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
                else
                {
                    // Generate seeds
                    var random = new FastRandom();
                    int readSeed = random.NextInt();
                    int writeSeed = random.NextInt();

                    // Receive public key
                    RSAParameters rsaKeyInfo = new()
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

        private static void SetupSocket(Socket socket)
        {
            // Set the buffer sizes, just in case
            socket.SendBufferSize = Math.Max(socket.SendBufferSize, 16384);
            socket.ReceiveBufferSize = Math.Max(socket.ReceiveBufferSize, 16384);

            // Use NoDelay to fix network performance issues
            socket.NoDelay = true;
        }

        #region IDisposable Support
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                    encryptedStream.Dispose();
                    netStream.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
