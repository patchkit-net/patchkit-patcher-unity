
using System;
using System.IO;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public class PositionAwareStream : Stream
    {
        private readonly Stream _baseStream;

        private long _position = 0;
        
        public PositionAwareStream(Stream stream)
        {
            if (stream.CanSeek)
            {
                throw new ArgumentException("Redundant use of position aware stream.");
            }
            
            _baseStream = stream;
        }

        public override bool CanRead { get { return _baseStream.CanRead; } }

        public override bool CanSeek { get { return true; } }

        public override bool CanWrite { get { return false; } }

        public override long Length { get { return _baseStream.Length; } }

        public override long Position 
        { 
            get 
            { 
                return _position;
            } 
            set
            {
                if (value < _position)
                {
                    throw new NotSupportedException("Cannot seek back");
                }

                long bytesForward = _position - value;
                for (int i = 0; i < bytesForward; i++)
                {
                    _baseStream.ReadByte();
                }

                _position = value;
            }
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = _baseStream.Read(buffer, offset, count);
            _position += bytesRead;
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                
                case SeekOrigin.Current:
                    Position = Position + offset;
                    break;

                case SeekOrigin.End:
                    throw new NotSupportedException();
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            _baseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}