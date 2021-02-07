using System;
using System.Diagnostics;
using System.IO;
using System.Web;

namespace Universe.SqlInsights.AspNetLegacy
{
    public class StreamWithCounters : Stream
    {
        public readonly Stream BaseStream;
        public long TotalReadBytes { get; private set; } = 0;
        public long TotalWrittenBytes { get; private set; } = 0;
        public Action StreamClosed;

        public StreamWithCounters(Stream baseStream)
        {
            var url = HttpContext.Current?.Request?.Url;
            Debug.WriteLine($"StreamWithCounters() for {baseStream} at {url}");
            
            if (baseStream == null)
                throw new ArgumentNullException("baseStream");

            BaseStream = baseStream;
        }

#if false
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            Debug.WriteLine($"StreamWithCounters.CopyToAsync()");
            return BaseStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Debug.WriteLine($"StreamWithCounters.WriteAsync() {count} bytes");
            return BaseStream.WriteAsync(buffer, offset, count, cancellationToken);
        }
#endif

        public override void Flush()
        {
            BaseStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return BaseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            BaseStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var ret = BaseStream.Read(buffer, offset, count);
            TotalReadBytes += ret;
            return ret;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            BaseStream.Write(buffer, offset, count);
            TotalWrittenBytes += count;
            var url = HttpContext.Current?.Request?.Url;
            Debug.WriteLine($"StreamWithCounters.Write() {count} bytes at {url}");

        }

        public override bool CanRead { get { return BaseStream.CanRead; } }
        public override bool CanSeek { get { return BaseStream.CanSeek; } }
        public override bool CanWrite { get { return BaseStream.CanWrite; } }
        public override bool CanTimeout { get { return BaseStream.CanTimeout; } }
        public override long Length { get { return BaseStream.Length; } }

        public override long Position
        {
            get { return BaseStream.Position; } 
            set { BaseStream.Position = value; }
        }

        public override void Close()
        {
            Debug.WriteLine("StreamWithCounters.Close()");
            var copy = StreamClosed;
            if (copy != null) copy();
            BaseStream.Close();
        }

        protected override void Dispose(bool disposing)
        {
            Debug.WriteLine("StreamWithCounters.Dispose()");
            var copy = StreamClosed;
            if (copy != null) copy();
            BaseStream.Dispose();
        }
    }
}