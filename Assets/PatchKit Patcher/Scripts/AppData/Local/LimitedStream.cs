using System;
using System.IO;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    /// <summary>
    /// Limits the stream to given size in bytes.
    /// </summary>
    public class LimitedStream : Stream
    {
        private readonly Stream _orig;
        private readonly long _bytesLimit;

        private long _bytesLeft;

        public LimitedStream(Stream orig, long bytesLimit)
        {
            _orig = orig;
            _bytesLimit = bytesLimit;
            _bytesLeft = _bytesLimit;
        }

        public override void Flush()
        {
            throw new InvalidOperationException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException();
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_bytesLeft == 0)
            {
                return 0;
            }

            var read = _orig.Read(buffer, offset, (int) Math.Min(_bytesLeft, count));
            _bytesLeft -= read;
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _bytesLimit; }
        }

        public override long Position
        {
            get { return _bytesLimit - _bytesLeft; }
            set
            {
                throw new InvalidOperationException();
            }

        }
    }
}