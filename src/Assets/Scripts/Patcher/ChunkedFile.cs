using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Patcher {

    /// <summary>
    /// Helps to save a file making a hash-checking of its chunks during the save process.
    /// 
    /// ChunkedFile is a file that has hashes defined for its segments (chunks). Chunks size
    /// has to be predefined.
    /// 
    /// Usage:
    /// Use Write() function as usuall to write bytes. As soon as you will get false stop copying
    /// procedure and restart it from next byte after VerifiedLength. If you will try to write
    /// bytes above the limit, you will get ArgumentOutOfRangeException.
    /// </summary>
    public class ChunkedFile : IDisposable
    {
        public delegate byte[] HashFunction(byte[] buffer, int offset, int length);

        private readonly int _chunkSize;
        private readonly long _fileSize;
        private readonly byte[][] _hashes;
        private readonly HashFunction _hashFunction;

        private readonly byte[] _buffer;
        private int _bufferPos;
        private int _chunkIndex;
        private readonly FileStream _fileStream;

        public long VerifiedLength
        {
            get { return Math.Min(_chunkIndex * _chunkSize, _fileSize); }
        }

        public long RemainingLength
        {
            get { return _fileSize - VerifiedLength; }
        }

        public ChunkedFile(string path, int chunkSize, long fileSize, byte[][] hashes, HashFunction hashFunction)
        {
            _chunkSize = chunkSize;
            _fileSize = fileSize;
            _hashes = hashes;
            _hashFunction = hashFunction;

            _buffer = new byte[chunkSize];

            _fileStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
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
            do
            {
                if (RemainingLength == 0)
                {
                    throw new ArgumentOutOfRangeException(
                        "Cannot write bytes over the file size: " + _fileSize);
                }
                
                int copyNum = (int) Math.Min(Math.Min(count, _chunkSize - _bufferPos), RemainingLength);
                Array.Copy(buffer, offset, _buffer, _bufferPos, copyNum);

                count -= copyNum;
                _bufferPos += copyNum;

                if (ChunkFullyInBuffer())
                {
                    if (BufferedChunkValid())
                    {
                        FlushBuffer();
                    } else
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
            return _bufferPos == Math.Min(_chunkSize, RemainingLength);
        }

        private bool BufferedChunkValid()
        {
            byte[] bufferHash = _hashFunction(_buffer, 0, _buffer.Length);
            byte[] chunkHash = _hashes[_chunkIndex];

            return bufferHash.SequenceEqual(chunkHash);
        }

        private void FlushBuffer()
        {
            _fileStream.Write(_buffer, 0, _buffer.Length);
            _bufferPos = 0;
            _chunkIndex++;
        }

        private void DiscardBuffer()
        {
            _bufferPos = 0;
        }

        public void Dispose()
        {
            CloseFile();
        }

        private void CloseFile()
        {
            _fileStream.Close();
            _fileStream.Dispose();
        }
    }
}
