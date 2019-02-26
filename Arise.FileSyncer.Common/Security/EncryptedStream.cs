using System;
using System.IO;

namespace Arise.FileSyncer.Common.Security
{
    internal class EncryptedStream : Stream
    {
        private readonly Stream stream;

        private readonly FastRandom readSelector;
        private readonly FastRandom writeSelector;

        public EncryptedStream(Stream stream, int readSeed, int writeSeed)
        {
            this.stream = stream;

            readSelector = new FastRandom(readSeed);
            writeSelector = new FastRandom(writeSeed);
        }

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
                buffer[i] -= (byte)readSelector.Next(byte.MaxValue + 1);
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
                buffer[i] += (byte)writeSelector.Next(byte.MaxValue + 1);
            }

            stream.Write(buffer, offset, count);
        }
    }
}
