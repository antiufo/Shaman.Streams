using System;
using System.IO;

namespace Shaman.Runtime
{
    public class LimitedStream : Stream
    {
        private long length;
        private long position;
        private long offset;
        private Stream baseStream;
        public LimitedStream(Stream baseStream, long length)
        {
            this.baseStream = baseStream;
            this.length = length;
            this.offset = -1;
            if (baseStream.CanSeek)
            {
                try
                {
                    this.offset = baseStream.Position;
                }
                catch
                {
                }
            }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return offset != -1 && baseStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
        }

        public override long Length
        {
            get { return length; }
        }

        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                if (value == position) return;
                if(offset == -1) throw new NotSupportedException();
                if (value < 0) throw new ArgumentOutOfRangeException();
                if (value > length) throw new ArgumentOutOfRangeException();
                baseStream.Seek(offset + value, SeekOrigin.Begin);
                position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesToRead = (int)Math.Min(count, length - position);
            if (bytesToRead == 0) return 0;

            var num = baseStream.Read(buffer, offset, bytesToRead);
            position += num;
            return num;

        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
                default:
                    throw new ArgumentException();
            }
            return Position;
        }

        public override int ReadByte()
        {
            if (position == length) return -1;
            var b = base.ReadByte();
            if (b != -1) position++;
            return b;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
