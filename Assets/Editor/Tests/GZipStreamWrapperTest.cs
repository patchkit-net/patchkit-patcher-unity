#if UNITY_2018
using System;
using System.IO;
using System.Text;
using Ionic.Zlib;
using NUnit.Framework;
using PatchKit.Unity.Utilities;

public class GZipStreamWrapperTest
{
    private class MockStream : Stream
    {
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
                return false;
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
                return _internalStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return _internalStream.Position;
            }

            set
            {
                _internalStream.Position = value;
            }
        }

        private readonly int _packetSize;

        private readonly MemoryStream _internalStream;

        public MockStream(byte[] data, int packetSize)
        {
            _packetSize = packetSize;
            _internalStream = new MemoryStream(data);
        }

        public override void Flush()
        {
            throw new System.NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _internalStream.Read(buffer, offset, System.Math.Min(count, _packetSize));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _internalStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _internalStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }
    }

    private const string RawData = "1234567890987654321qwerweqrfa";

    private byte[] compressedData;

    [SetUp]
    public void SetUp()
    {
        using (var targetStream = new MemoryStream())
        {
            using (var sourceStream = new MemoryStream(Encoding.ASCII.GetBytes(RawData)))
            {
                using (var gzipStream = new GZipStream(targetStream, CompressionMode.Compress))
                {
                    Streams.Copy(sourceStream, gzipStream);
                }
            }

            compressedData = targetStream.ToArray();
        }
    }

    [Test]
    public void Raw_GZip_Stream_Fails_On_Mocked_Stream()
    {
        TestDelegate act = () => {
            using (var targetStream = new MemoryStream())
            {
                using (var mockStream = new MockStream(compressedData, 5))
                {
                    using (var gzipStream = new GZipStream(mockStream, CompressionMode.Decompress))
                    {
                        Streams.Copy(gzipStream, targetStream);
                    }
                }
            }
        };

        Assert.Throws(typeof(ZlibException), act);
    }

    [Test]
    public void Wrapped_Read_Succeeds_On_Mocked_Stream()
    {
        string decompressed;
        using (var targetStream = new MemoryStream())
        {
            using (var mockStream = new MockStream(compressedData, 5))
            {
                using (var wrapperStream  = new GZipReadWrapperStream(mockStream))
                {
                    using (var gzipStream = new GZipStream(wrapperStream, CompressionMode.Decompress))
                    {
                        Streams.Copy(gzipStream, targetStream);
                    }
                }
            }

            decompressed = Encoding.ASCII.GetString(targetStream.ToArray());
        }

        Assert.That(decompressed == RawData);
    }
}
#endif