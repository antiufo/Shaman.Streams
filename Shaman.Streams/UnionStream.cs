/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaman.Runtime
{
    public class UnionStream : ReadOnlyStreamBase
    {
        public UnionStream(Stream first, Stream second)
        {
            this.first = first;
            this.second = second;
        }
        private Stream first;
        private Stream second;

        public override long Length => first.Length + second.Length;

        public override long Position { get => first.Position + second.Position; set => throw new NotSupportedException(); }

       
        private bool hasCompletedFirst;
        public override int Read(byte[] buffer, int offset, int count)
        {

            if (!hasCompletedFirst)
            {
                var r = first.Read(buffer, offset, count);
                if (r != 0) return r;
                hasCompletedFirst = true;

            }

            return second.Read(buffer, offset, count);

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                first.Dispose();
                second.Dispose();
            }
        }

    }
}
*/