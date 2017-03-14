using System;
using System.IO;

namespace Shaman.Runtime
{
    public class ReadFullyStream : Stream
    {

        private Stream sourceStream;
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }
        public override bool CanSeek
        {
            get
            {
                return sourceStream.CanSeek;
            }
        }
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }
        public override long Length
        {
            get
            {
                return sourceStream.Length;
            }
        }
        public override long Position
        {
            get
            {
                return sourceStream.Position;
            }
            set
            {
                sourceStream.Position = value;
            }
        }
        public ReadFullyStream(Stream sourceStream)
        {
            this.sourceStream = sourceStream;
        }
        public override void Flush()
        {
            throw new NotSupportedException();
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            var readData = 0;
            while (readData < count)
            {
                var newData = sourceStream.Read(buffer, offset + readData, count - readData);
                if (newData == 0) break;

                readData += newData;
            }
            return readData;
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            return sourceStream.Seek(offset, origin);
        }
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                sourceStream.Dispose();
        }

#if !CORECLR
        public override void Close()
        {
            sourceStream.Close();
        }
#endif
        
        public override int ReadByte()
        {
            return sourceStream.ReadByte();
        }

    }
}
