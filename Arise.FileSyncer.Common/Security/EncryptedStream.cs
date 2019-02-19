using System;
using System.IO;

namespace Arise.FileSyncer.Common.Security
{
    internal class EncryptedStream : Stream
    {
        private readonly Stream stream;
        private readonly byte[] key;

        private readonly FastRandom readSelector;
        private readonly FastRandom writeSelector;

        public EncryptedStream(Stream stream, byte[] key, int iv)
        {
            this.stream = stream;
            this.key = key;

            readSelector = new FastRandom(iv);
            writeSelector = new FastRandom(iv);
        }

        public EncryptedStream(Stream stream, byte[] key, byte[] iv) : this(stream, key, BitConverter.ToInt32(iv, 0)) { }

        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => stream.CanWrite;

        public override long Length => stream.Length;

        public override long Position { get => stream.Position; set => throw new NotImplementedException(); }

        public override void Flush()
        {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = stream.Read(buffer, offset, count);

            for (int i = offset; i < offset + read; i++)
            {
                buffer[i] -= key[readSelector.Next(key.Length)];
            }

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            for (int i = offset; i < offset + count; i++)
            {
                buffer[i] += key[writeSelector.Next(key.Length)];
            }

            stream.Write(buffer, offset, count);
        }
    }
}
