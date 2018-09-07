using System.IO;
using System;

namespace PatchKit.Unity.Utilities
{
    public class GZipReadWrapperStream : Stream
    {
        private readonly int _minimumReadSize;

        private readonly Stream _source;

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
                return _source.CanSeek;
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
                return _source.Length;
            }
        }

        public override long Position
        {
            get
            {
                return _source.Position;
            }
            set
            {
                _source.Position = value;
            }
        }

        public override void Flush()
        {
            throw new System.NotSupportedException();
        }

        public GZipReadWrapperStream(Stream sourceStream)
        {
            _source = sourceStream;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;
            int bytesRead;

            byte[] tempBuffer = new byte[count];

            do
            {
                bytesRead = _source.Read(tempBuffer, totalBytesRead, count - totalBytesRead);

                totalBytesRead += bytesRead;
            }
            while (bytesRead > 0 && totalBytesRead < count);

            Array.ConstrainedCopy(tempBuffer, 0, buffer, offset, count);

            return totalBytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _source.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new System.NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotSupportedException();
        }
    }
}