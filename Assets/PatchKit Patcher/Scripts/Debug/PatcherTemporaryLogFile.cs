using System;
using System.Collections.Generic;
using System.IO;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppData.FileSystem;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.Debug
{
    public class PatcherTemporaryLogFile : IDisposable
    {
        public readonly string FilePath;

        private readonly List<string> _buffer = new List<string>();

        private readonly object _bufferLock = new object();

        public PatcherTemporaryLogFile()
        {
            FilePath = GetUniqueTemporaryFilePath();
        }

        public void WriteLine(string line)
        {
            lock (_bufferLock)
            {
                _buffer.Add(line);
            }
        }

        public void Flush()
        {
            lock (_bufferLock)
            {
                using (var logFileStream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    logFileStream.Seek(0, SeekOrigin.End);
                    using (var logFileStreamWriter = new StreamWriter(logFileStream))
                    {
                        foreach (var line in _buffer)
                        {
                            logFileStreamWriter.WriteLine(line);
                        }
                        _buffer.Clear();
                    }
                }
            }
        }

        private static string GetUniqueTemporaryFilePath()
        {
            var filePath = string.Empty;

            for (int i = 0; i < 100; i++)
            {
                filePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
                if (!File.Exists(filePath) && !Directory.Exists(filePath))
                {
                    break;
                }
            }
            return filePath;
        }

        private void ReleaseUnmanagedResources()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    FileOperations.Delete(FilePath, CancellationToken.Empty);
                }
            }
            catch
            {
                // ignore
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~PatcherTemporaryLogFile()
        {
            ReleaseUnmanagedResources();
        }
    }
}