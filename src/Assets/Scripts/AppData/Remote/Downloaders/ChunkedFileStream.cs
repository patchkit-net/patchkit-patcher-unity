using System;
using System.IO;
using System.Linq;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    /// <summary>
    /// Helps to save a file making a hash-checking of its chunks during the save process.
    /// 
    /// ChunkedFileStream is a file that has hashes defined for its segments (chunks). Chunks size
    /// has to be predefined.
    /// 
    /// Usage:
    /// Use Write() function as usuall to write bytes. As soon as you will get false stop copying
    /// procedure and restart it from next byte after VerifiedLength. If you will try to write
    /// bytes above the limit, you will get ArgumentOutOfRangeException.
    /// </summary>
    public class ChunkedFileStream : IDisposable
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(ChunkedFileStream));

        public delegate byte[] HashFunction(byte[] buffer, int offset, int length);

        private readonly ChunksData _chunksData;
        private readonly long _fileSize;
        private readonly HashFunction _hashFunction;

        private readonly byte[] _buffer;
        private int _bufferPos;
        private int _chunkIndex;
        private readonly FileStream _fileStream;

        private bool _disposed;

        public long VerifiedLength
        {
            get { return Math.Min(_chunkIndex * _chunksData.ChunkSize, _fileSize); }
        }

        public long SavedLength
        {
            get { return VerifiedLength + _bufferPos; }
        }

        public long RemainingLength
        {
            get { return _fileSize - VerifiedLength; }
        }

        public long Length
        {
            get { return _fileSize; }
        }

        public ChunkedFileStream(string path, long fileSize, ChunksData chunksData, HashFunction hashFunction)
        {
            Checks.ArgumentNotNullOrEmpty(path, "path");
            Checks.ArgumentMoreThanZero(fileSize, "fileSize");
            AssertChecks.ArgumentNotNull(hashFunction, "hashFunction");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(path, "path");
            DebugLogger.LogVariable(fileSize, "fileSize");

            _fileSize = fileSize;
            _chunksData = chunksData;
            _hashFunction = hashFunction;

            _buffer = new byte[_chunksData.ChunkSize];

            _fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        }

        /// <summary>
        /// Writes buffer into the file. If false is returned, stop file transfer and resume it
        /// starting from VerifiedLength + 1 byte.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public bool Write(byte[] buffer, int offset, int count)
        {
            AssertChecks.ArgumentNotNull(buffer, "buffer");
            // TODO: Rest of assertions

            do
            {
                if (RemainingLength == 0)
                {
                    throw new ArgumentOutOfRangeException(
                        "Cannot write bytes over the file size: " + _fileSize);
                }

                int copyNum = (int)Math.Min(Math.Min(count, _chunksData.ChunkSize - _bufferPos), RemainingLength);
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
                        return false;
                    }

                }
            } while (count > 0);

            return true;
        }

        private bool ChunkFullyInBuffer()
        {
            return _bufferPos == Math.Min(_chunksData.ChunkSize, RemainingLength);
        }

        private bool BufferedChunkValid()
        {
            byte[] bufferHash = _hashFunction(_buffer, 0, (int)Math.Min(_chunksData.ChunkSize, RemainingLength));
            byte[] chunkHash = _chunksData.Chunks[_chunkIndex].Hash;

            return bufferHash.SequenceEqual(chunkHash);
        }

        private void FlushBuffer()
        {
            _fileStream.Write(_buffer, 0, (int)Math.Min(_chunksData.ChunkSize, RemainingLength));
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

        ~ChunkedFileStream()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(_disposed)
            {
                return;
            }

            DebugLogger.LogDispose();

            if(disposing)
            {
                _fileStream.Dispose();
            }

            _disposed = true;
        }
    }
}
