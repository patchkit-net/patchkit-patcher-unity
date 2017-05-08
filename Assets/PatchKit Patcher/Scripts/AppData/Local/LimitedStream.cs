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
        private long _bytesLeft;

        public LimitedStream(Stream orig, long bytesLimit)
        {
            _orig = orig;
            _bytesLeft = bytesLimit;
        }

        public override void Flush()
        {
            _orig.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _orig.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _orig.SetLength(value);
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
            _orig.Write(buffer, offset, count);
        }

        public override bool CanRead
        {
            get { return _orig.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _orig.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _orig.CanWrite; }
        }

        public override long Length
        {
            get { return _orig.Length; }
        }

        public override long Position
        {
            get { return _orig.Position; }
            set { _orig.Position = value; }

        }
    }
}