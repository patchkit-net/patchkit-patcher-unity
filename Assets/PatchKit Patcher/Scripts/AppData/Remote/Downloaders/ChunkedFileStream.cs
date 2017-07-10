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
        [Flags]
        public enum WorkFlags
        {
            None = 0,
            PreservePreviousFile = 1
        }
        
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

        public ChunkedFileStream(string path, long fileSize, ChunksData chunksData, HashFunction hashFunction,
            WorkFlags workFlags = WorkFlags.None)
        {
            Checks.ArgumentNotNullOrEmpty(path, "path");
            Checks.ArgumentMoreThanZero(fileSize, "fileSize");
            Checks.ArgumentNotNull(hashFunction, "hashFunction");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(path, "path");
            DebugLogger.LogVariable(fileSize, "fileSize");

            _fileSize = fileSize;
            _chunksData = chunksData;
            _hashFunction = hashFunction;

            _buffer = new byte[_chunksData.ChunkSize];

            if ((workFlags | WorkFlags.PreservePreviousFile) != 0)
            {
                // Often you may want to continue downloading of a file if this exists
                // It tries to open a file and re-download it from the verified position.
                // It does not check the hash of the file. It trusts that the file is already valid up to that point.
                // Because the only way to download the file should be using Chunked Downloader.
                
                _fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                _fileStream.Seek(0, SeekOrigin.End); // seek and stay at the end, so we can append
                long currentFileSize = _fileStream.Position;

                // Let's make sure that file size is a multiply of chunk size.
                // If not, something is wrong with the file.
                if (currentFileSize % chunksData.ChunkSize == 0)
                {
                    _chunkIndex = (int) (currentFileSize / chunksData.ChunkSize);
                }
                else
                {
                    DebugLogger.LogWarningFormat(
                        "File {0} size {1} is not a multiply of chunk size: {2}. Will recreate it.", path,
                        currentFileSize, chunksData.ChunkSize);
                    
                    _fileStream.Close();
                    _fileStream.Dispose();
                    
                    _fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                }
            }
            else
            {
                _fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
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
        public bool Write(byte[] buffer, int offset, int count)
        {
            Checks.ArgumentNotNull(buffer, "buffer");
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
