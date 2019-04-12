using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using PatchKit.Logging;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public interface IChunkedStream
    {
        long VerifiedLength { get; }
        long SavedLength { get; }
        long RemainingLength { get; }
        long Length { get; }

        void Write([NotNull] byte[] buffer, int offset, int count);
        void ClearUnverified();
    }

    /// <summary>
    /// A generic implementation of a chunked data stream. Can be used to save chunked data into any stream be it file or buffer.
    ///
    /// Be aware that this class doesn't implement the Stream interface. But you can use the Write method as if it did.
    /// </summary>
    public class ChunkedStream<T> : IChunkedStream, IDisposable where T: Stream
    {
        private readonly ILogger _logger;

        public delegate byte[] HashFunction(byte[] buffer, int offset, int length);

        private readonly ChunksData _chunksData;
        private readonly long _size;
        private readonly HashFunction _hashFunction;

        private readonly byte[] _buffer;
        private int _bufferPos;
        protected int _chunkIndex;

        private int _startChunk;

        private T _targetStream;

        private bool _disposed;

        public long VerifiedLength
        {
            get { return Math.Min(_chunkIndex * _chunksData.ChunkSize, _size); }
        }

        public long SavedLength
        {
            get { return VerifiedLength + _bufferPos; }
        }

        public long RemainingLength
        {
            get { return _size - VerifiedLength; }
        }

        public long Length
        {
            get { return _size; }
        }

        public ChunkedStream([NotNull] T targetStream, long size, ChunksData chunksData,
            [NotNull] HashFunction hashFunction, int startChunk = 0, int endChunk = -1)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException("size");
            if (hashFunction == null) throw new ArgumentNullException("hashFunction");
            if (!targetStream.CanSeek) throw new ArgumentException("Target stream must support seeking", "targetStream");
            if (!targetStream.CanWrite) throw new ArgumentException("Target stream must support writing", "targetStream");

            _targetStream = targetStream;

            _logger = PatcherLogManager.DefaultLogger;
            _chunksData = chunksData;
            _hashFunction = hashFunction;

            _buffer = new byte[_chunksData.ChunkSize];

            _logger.LogTrace("chunksData.ChunkSize = " + chunksData.ChunkSize);

            _startChunk = startChunk;

            bool noEndChunk = endChunk == -1;
            bool isLastChunkIncomplete = !noEndChunk && (endChunk * chunksData.ChunkSize > size);

            if (noEndChunk || isLastChunkIncomplete)
            {
                _size = size - (startChunk * chunksData.ChunkSize);
            }
            else
            {
                _size = (endChunk - startChunk) * chunksData.ChunkSize;
            }

            _logger.LogTrace("size = " + size);

            long currentStreamSize = _targetStream.Seek(0, SeekOrigin.End);

            if (currentStreamSize == 0)
            {
                _logger.LogDebug("Stream is empty. Append is not possible");
            }
            else if (currentStreamSize % chunksData.ChunkSize == 0)
            {
                _chunkIndex = (int) (currentStreamSize / chunksData.ChunkSize);
                _logger.LogDebug(string.Format("Append is possible - starting from {0} chunk index", _chunkIndex));
            }
            else
            {
                _logger.LogError(string.Format("Stream size {0} is not a multiple of chunk size {1}. Stream is invalid."));
                throw new ArgumentException("Stream is invalid", "targetStream");
            }
        }

        /// <summary>
        /// Writes buffer into the file. If false is returned, stop file transfer and resume it
        /// starting from VerifiedLength + 1 byte.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public void Write([NotNull] byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (offset < 0) throw new ArgumentOutOfRangeException("offset");
            if (count < 0) throw new ArgumentOutOfRangeException("count");

            do
            {
                if (RemainingLength == 0)
                {
                    throw new ArgumentOutOfRangeException(
                        "Cannot write bytes over the file size: " + _size);
                }

                int copyNum = (int) Math.Min(Math.Min(count, _chunksData.ChunkSize - _bufferPos), RemainingLength);
                Array.Copy(buffer, offset, _buffer, _bufferPos, copyNum);

                count -= copyNum;
                offset += copyNum;
                _bufferPos += copyNum;

                if (ChunkFullyInBuffer())
                {
                    if (BufferedChunkValid())
                    {
                        FlushBuffer();
                    }
                    else
                    {
                        DiscardBuffer();
                        throw new InvalidChunkDataException("Invalid chunk data.");
                    }
                }
            } while (count > 0);
        }

        public void ClearUnverified()
        {
            DiscardBuffer();
        }

        private bool ChunkFullyInBuffer()
        {
            return _bufferPos == Math.Min(_chunksData.ChunkSize, RemainingLength);
        }

        private bool BufferedChunkValid()
        {
            byte[] bufferHash = _hashFunction(_buffer, 0, (int) Math.Min(_chunksData.ChunkSize, RemainingLength));
            byte[] chunkHash = _chunksData.Chunks[_chunkIndex + _startChunk].Hash;

            return bufferHash.SequenceEqual(chunkHash);
        }

        private void FlushBuffer()
        {
            _targetStream.Write(_buffer, 0, (int) Math.Min(_chunksData.ChunkSize, RemainingLength));
            _bufferPos = 0;
            _chunkIndex++;
        }

        private void DiscardBuffer()
        {
            _bufferPos = 0;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ChunkedStream()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _targetStream.Dispose();
            }

            _disposed = true;
        }
    }
}