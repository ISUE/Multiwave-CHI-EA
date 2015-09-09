using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace MediaCenter
{
    public class WaveProviderToWaveStream : WaveStream
    {
        private readonly IWaveProvider source;
        private readonly WaveStream FReferenceStream;
        private long position;

        public WaveProviderToWaveStream(IWaveProvider source, WaveStream referenceStream)
        {
            this.source = source;
            this.FReferenceStream = referenceStream;
        }

        public override WaveFormat WaveFormat
        {
            get { return source.WaveFormat; }
        }

        /// <summary>
        /// Don't know the real length of the source, just return a big number
        /// </summary>
        public override long Length
        {
            get { return Int32.MaxValue; }
        }

        public override long Position
        {
            get
            {
                // we'll just return the number of bytes read so far
                return position;
            }
            set
            {
                if (source != null)
                    FReferenceStream.Position = value;
                else
                    position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = source.Read(buffer, offset, count);
            position += read;
            return read;
        }
    }
}
