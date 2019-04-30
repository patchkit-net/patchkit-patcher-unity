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
    public sealed class ChunkedFileStream : ChunkedStream<FileStream>
    {
        [Flags]
        public enum WorkFlags
        {
            None = 0,
            PreservePreviousFile = 1
        }

        private readonly ILogger _logger;

        private FileStream _fileStream;

        public static ChunkedFileStream CreateChunkedFileStream([NotNull] string path, long fileSize, ChunksData chunksData,
            [NotNull] HashFunction hashFunction, WorkFlags workFlags = WorkFlags.None, int startChunk = 0, int endChunk = -1)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (fileSize <= 0) throw new ArgumentOutOfRangeException("fileSize");
            if (hashFunction == null) throw new ArgumentNullException("hashFunction");

            var logger = PatcherLogManager.DefaultLogger;

            logger.LogTrace("path = " + path);


            bool preservePreviousFile = (workFlags | WorkFlags.PreservePreviousFile) != 0;

            logger.LogTrace("preservePreviousFile = " + preservePreviousFile);

            FileStream fileStream = null;

            if (preservePreviousFile)
            {
                // Often you may want to continue downloading of a file if this exists
                // It tries to open a file and re-download it from the verified position.
                // It does not check the hash of the file. It trusts that the file is already valid up to that point.
                // Because the only way to download the file should be using Chunked Downloader.

                logger.LogDebug("Trying to preserve previous file, opening file stream...");
                fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                fileStream.Seek(0, SeekOrigin.End); // seek and stay at the end, so we can append

                long currentFileSize = fileStream.Position;

                logger.LogDebug("File stream opened.");
                logger.LogTrace("currentFileSize = " + currentFileSize);

                logger.LogDebug("Checking whether stream can append to current file...");

                if (currentFileSize != 0 && currentFileSize % chunksData.ChunkSize != 0)
                {
                    logger.LogDebug(string.Format(
                        "File size {0} is not a multiply of chunk size: {1}. Append is not possible - recreating file.",
                        currentFileSize,
                        chunksData.ChunkSize));

                    logger.LogDebug("Closing previous file stream...");
                    fileStream.Close();
                    fileStream.Dispose();
                    logger.LogDebug("Previous file stream closed.");

                    logger.LogDebug("Opening new file stream");
                    fileStream = OpenNewFileStream(path);
                }
            }
            else
            {
                logger.LogDebug("Not preserving previous file, opening new file stream");
                fileStream = OpenNewFileStream(path);
            }

            return new ChunkedFileStream(fileStream, fileSize, chunksData, hashFunction, startChunk, endChunk);
        }

        private static FileStream OpenNewFileStream(string path)
        {
            return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        }

        public ChunkedFileStream([NotNull] FileStream fileStream, long fileSize, ChunksData chunksData,
            [NotNull] HashFunction hashFunction, int startChunk = 0, int endChunk = -1)
            : base(fileStream, fileSize, chunksData, hashFunction, startChunk, endChunk)
        {

            if (fileSize <= 0) throw new ArgumentOutOfRangeException("fileSize");
            if (hashFunction == null) throw new ArgumentNullException("hashFunction");
        }
    }
}