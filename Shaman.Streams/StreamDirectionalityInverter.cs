using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Shaman.Runtime
{
    public class StreamDirectionalityInverter : Stream
    {

        private class Writer : Stream
        {

            public StreamDirectionalityInverter inverter;

            public override bool CanRead { get { return false; } }

            public override bool CanSeek { get { return false; } }

            public override bool CanWrite { get { return true; } }

            public override long Length { get { throw new NotSupportedException(); } }

            public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                inverter.WriteInternal(buffer, offset, count);
            }
        }

        private int bufferStart;
        private int bufferCount;
        private int BufferEnd => (bufferStart + bufferCount) % mainBuffer.Length;
        private byte[] mainBuffer;
        private Writer writer;
        private object lockObject = new object();
        private bool disposed;

        private AutoResetEvent writeWait;
        private AutoResetEvent readWait;

        private const int DefaultBufferSize = 512 * 1024;

        public StreamDirectionalityInverter(Action<Stream> write)
            : this(write, DefaultBufferSize)
        {

        }
        
        

        public StreamDirectionalityInverter()
            : this(null, DefaultBufferSize)
        {

        }
        
        public StreamDirectionalityInverter(int bufferSize)
            : this(null, bufferSize)
        {

        }
        
        
        public Stream StreamForWriting => writer;
        
        public StreamDirectionalityInverter(Action<Stream> write, int bufferSize)
        {
            this.writeWait = new AutoResetEvent(true);
            this.readWait = new AutoResetEvent(false);
            mainBuffer = new byte[bufferSize];
            writer = new Writer();
            writer.inverter = this;
            if(write != null)
            {
                Task.Run(() =>
                {
                    try
                    {
                        write(writer);
                    }
                    catch (Exception ex)
                    {
                        NotifyFailed(ex);
                    }
                    NotifyCompleted();
                });
            }
        }

        public void NotifyFailed(Exception ex)
        {
            exception = ex;
            NotifyCompleted();
        }

        public void NotifyCompleted()
        {
            completed = true;
            if (!disposed)
            {
                try
                {
                    readWait.Set();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        private volatile Exception exception;
        private volatile bool completed;

        private void WriteInternal(byte[] buffer, int offset, int count)
        {
            while (count != 0)
            {
                lock (lockObject)
                {
                    if (disposed) throw new ObjectDisposedException(nameof(StreamDirectionalityInverter));

                    var bufferEnd = this.BufferEnd;
                    if (bufferCount == mainBuffer.Length)
                    {
                        // nothing to do. wait
                    }
                    else if (bufferStart > bufferEnd)
                    {
                        //Console.WriteLine("Write: " + count + " (inv)");
                        var tocopy = Math.Min(count, bufferStart - bufferEnd);
                        Array.Copy(buffer, offset, mainBuffer, bufferEnd, tocopy);
                        count -= tocopy;
                        offset += tocopy;
                        bufferCount += tocopy;
                        readWait.Set();
                        continue;
                    }
                    else if (bufferStart <= bufferEnd)
                    {
                        //Console.WriteLine("Write: " + count + " (short)");
                        var tocopy = Math.Min(count, mainBuffer.Length - bufferEnd);
                        Array.Copy(buffer, offset, mainBuffer, bufferEnd, tocopy);
                        count -= tocopy;
                        offset += tocopy;
                        bufferCount += tocopy;
                        readWait.Set();
                        continue;
                    }

                }
                //Console.WriteLine("          Write wait");
                writeWait.WaitOne();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0) throw new ArgumentOutOfRangeException();
            while (true)
            {
                lock (lockObject)
                {
                    if (disposed) throw new ObjectDisposedException(nameof(StreamDirectionalityInverter));
                    if (completed)
                    {
                        if (exception != null) throw exception;
                        if (bufferCount == 0) return 0;
                    }

                    /* read fully
                    if (bufferCount != mainBuffer.Length && !completed)
                    {
                    }
                    else 
                    */
                    if (bufferCount == 0)
                    {
                        // nothing to do, wait
                    }
                    else
                    {
                        var bufferEnd = this.BufferEnd;

                        var tocopy = Math.Min(bufferCount, count);
                        if (bufferStart < bufferEnd)
                        {
                            //Console.WriteLine("  Read: " + tocopy + " (simple)");
                            Array.Copy(mainBuffer, bufferStart, buffer, offset, tocopy);
                            bufferStart += tocopy;
                            bufferStart %= mainBuffer.Length;
                            bufferCount -= tocopy;
                            writeWait.Set();
                            return tocopy;
                        }
                        else
                        {
                            var tocopyA = Math.Min(tocopy, mainBuffer.Length - bufferStart);
                            //Console.WriteLine("  Read: " + tocopyA + " (end)");
                            Array.Copy(mainBuffer, bufferStart, buffer, offset, tocopyA);
                            offset += tocopyA;
                            count -= tocopyA;
                            bufferStart += tocopyA;
                            bufferStart %= mainBuffer.Length;
                            bufferCount -= tocopyA;

                            var tocopyB = Math.Min(tocopy - tocopyA, bufferEnd);
                            if (tocopyB != 0)
                            {
                                //Console.WriteLine("  Read: " + tocopyB + " (rest)");
                                Array.Copy(mainBuffer, 0, buffer, offset, tocopyB);
                                bufferStart += tocopyB;
                                bufferCount -= tocopyB;
                            }

                            writeWait.Set();
                            return tocopyA + tocopyB;
                        }
                    }

                }
                //Console.WriteLine("               Read wait");
                readWait.WaitOne();
            }
        }



        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                disposed = true;

                try
                {
                    readWait.Set();
                    writeWait.Set();
                }
                catch (ObjectDisposedException)
                {
                }

                readWait.Dispose();
                writeWait.Dispose();
            }
        }
        
        public override bool CanRead { get { return true; } }

        public override bool CanSeek { get { return false; } }

        public override bool CanWrite { get { return false; } }

        public override long Length { get { throw new NotSupportedException(); } }

        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }



        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
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