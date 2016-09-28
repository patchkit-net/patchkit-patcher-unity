using System;
using System.IO;
using System.Linq;
using PatchKit.Api;

namespace PatchKit.Unity.Patcher
{
    internal class PatcherData : IDisposable
    {
        /// <summary>
        /// Cache storing local data variables.
        /// </summary>
        public PatcherDataCache Cache { get; private set; }

        /// <summary>
        /// Path to the local data.
        /// </summary>
        public readonly string Path;

        /// <summary>
        /// Temporary data path.
        /// </summary>
        public readonly string TempPath;

        private readonly string _cachePath;

        private void RecreateCache()
        {
            Cache = new PatcherDataCache(_cachePath);
        }

        public PatcherData(string path)
        {
            Path = path;
            _cachePath = System.IO.Path.Combine(Path, "patcher_cache.json");
            RecreateCache();

            {
                int iteration = 0;

                do
                {
                    TempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
                } while (iteration++ < 1000 && Directory.Exists(TempPath));

                Directory.CreateDirectory(TempPath);
            }
        }

        /// <summary>
        /// Runs through all the application files and checks if 
        /// 1) files are there, 
        /// 3) files are in the correct version,
        /// 2) files has the same hash as required. 
        /// Returns false if something is wrong.
        /// </summary>
        public bool CheckFilesConsistency(int version, ApiConnection.AppContentSummary contentSummary)
        {
            foreach (var summaryFile in contentSummary.Files)
            {
                string absolutePath = GetFilePath(summaryFile.Path);
                if (!File.Exists(absolutePath))
                {
                    return false;
                }

                if (Cache.GetFileVersion(summaryFile.Path) != version)
                {
                    return false;
                }

                // TODO: Uncomment this after xxhash implementation.
//                string fileHash = HashUtilities.ComputeFileHash(absolutePath);
//                if (!fileHash.Equals(summaryFile.Hash))
//                {
//                    return false;
//                }
            }

            return true;
        }

        /// <summary>
        /// Clears local data (as well as cache).
        /// </summary>
        public void Clear()
        {
            foreach (var fileName in Cache.GetFileNames().ToArray())
            {
                ClearFile(fileName);
            }

            if (File.Exists(_cachePath))
            {
                File.Delete(_cachePath);
            }

            RecreateCache();
        }

        /// <summary>
        /// Clears data (as well as cache) about specified file.
        /// </summary>
        public void ClearFile(string fileName)
        {
            string filePath = GetFilePath(fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            Cache.ClearFileVersion(fileName);
        }

        /// <summary>
        /// Deletes directory if it's possible.
        /// </summary>
        public bool TryClearDirectory(string directoryName)
        {
            string directoryPath = System.IO.Path.Combine(Path, directoryName);

            if (Directory.Exists(directoryPath))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);

                if (directoryInfo.GetFiles("*", SearchOption.AllDirectories).Length == 0)
                {
                    Directory.Delete(directoryPath, true);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns absolute path to specified file (placed in local data).
        /// </summary>
        public string GetFilePath(string fileName)
        {
            return System.IO.Path.Combine(Path, fileName);
        }

        public void Dispose()
        {
            if (Directory.Exists(TempPath))
            {
                Directory.Delete(TempPath, true);
            }
        }
    }
}