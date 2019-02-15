using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using PatchKit.Logging;
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
    /// Use Write() function as usuall to write bytes. As soon as you will catch InvalidChunkDataException stop copying
    /// procedure and restart it from next byte after VerifiedLength. If you will try to write
    /// bytes above the limit, you will get ArgumentOutOfRangeException.
    /// </summary>
    public sealed class ChunkedFileStream : IDisposable
    {
        [Flags]
        public enum WorkFlags
        {
            None = 0,
            PreservePreviousFile = 1
        }

        private readonly ILogger _logger;

        public delegate byte[] HashFunction(byte[] buffer, int offset, int length);

        private readonly ChunksData _chunksData;
        private readonly long _fileSize;
        private readonly HashFunction _hashFunction;

        private readonly byte[] _buffer;
        private int _bufferPos;
        private int _chunkIndex;

        private int _startChunk;

        private FileStream _fileStream;

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

        public ChunkedFileStream([NotNull] string path, long fileSize, ChunksData chunksData,
            [NotNull] HashFunction hashFunction,
            WorkFlags workFlags = WorkFlags.None, int startChunk = 0, int endChunk = -1)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (fileSize <= 0) throw new ArgumentOutOfRangeException("fileSize");
            if (hashFunction == null) throw new ArgumentNullException("hashFunction");

            _logger = PatcherLogManager.DefaultLogger;
            _chunksData = chunksData;
            _hashFunction = hashFunction;

            _buffer = new byte[_chunksData.ChunkSize];

            _logger.LogTrace("path = " + path);
            _logger.LogTrace("chunksData.ChunkSize = " + chunksData.ChunkSize);

            _startChunk = startChunk;

            bool noEndChunk = endChunk == -1;
            bool isLastChunkIncomplete = endChunk * chunksData.ChunkSize > fileSize;

            if (noEndChunk || isLastChunkIncomplete)
            {
                _fileSize = fileSize - (startChunk * chunksData.ChunkSize);
            }
            else
            {
                _fileSize = (endChunk - startChunk) * chunksData.ChunkSize;
            }

            _logger.LogTrace("fileSize = " + fileSize);

            bool preservePreviousFile = (workFlags | WorkFlags.PreservePreviousFile) != 0;
            
            _logger.LogTrace("preservePreviousFile = " + preservePreviousFile);

            if (preservePreviousFile)
            {
                // Often you may want to continue downloading of a file if this exists
                // It tries to open a file and re-download it from the verified position.
                // It does not check the hash of the file. It trusts that the file is already valid up to that point.
                // Because the only way to download the file should be using Chunked Downloader.

                _logger.LogDebug("Opening file stream...");
                _fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                _fileStream.Seek(0, SeekOrigin.End); // seek and stay at the end, so we can append
                long currentFileSize = _fileStream.Position;
                _logger.LogDebug("File stream opened.");
                _logger.LogTrace("currentFileSize = " + currentFileSize);

                _logger.LogDebug("Checking whether stream can append to current file...");

                if (currentFileSize == 0)
                {
                    _logger.LogDebug("File is new. Append is not possible.");
                }
                // Let's make sure that file size is a multiply of chunk size.
                // If not, something is wrong with the file.
                else if (currentFileSize % chunksData.ChunkSize == 0)
                {
                    _chunkIndex = (int) (currentFileSize / chunksData.ChunkSize);
                    _logger.LogDebug(string.Format("Append is possible - starting from {0} chunk index.", _chunkIndex));
                }
                else
                {
                    _logger.LogDebug(string.Format(
                        "File size {0} is not a multiply of chunk size: {1}. Append is not possible - recreating file.",
                        currentFileSize,
                        chunksData.ChunkSize));

                    _logger.LogDebug("Closing previous file stream...");
                    _fileStream.Close();
                    _fileStream.Dispose();
                    _logger.LogDebug("Previous file stream closed.");

                    OpenNewFileStream(path);
                }
            }
            else
            {
                OpenNewFileStream(path);
            }
        }

        private void OpenNewFileStream(string path)
        {
            _logger.LogDebug("Opening new file stream...");
            _fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            _logger.LogDebug("New file stream opened.");
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
                        "Cannot write bytes over the file size: " + _fileSize);
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
            _fileStream.Write(_buffer, 0, (int) Math.Min(_chunksData.ChunkSize, RemainingLength));
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

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _fileStream.Dispose();
            }

            _disposed = true;
        }
    }
}