using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Ionic.Zlib;
using PatchKit.Network;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Data;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;

namespace PatchKit.Unity.Patcher.AppData.Local
{

    /// <summary>
    /// Pack1 format unarchiver.
    /// http://redmine.patchkit.net/projects/patchkit-documentation/wiki/Pack1_File_Format
    /// </summary>
    public class Pack1Unarchiver : IUnarchiver
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(Pack1Unarchiver));

        private readonly string _packagePath;
        private readonly Pack1Meta _metaData;
        private readonly string _destinationDirPath;
        private readonly string _suffix;
        private readonly byte[] _key;
        private readonly byte[] _iv;

        private readonly BytesRange _range;

        public event UnarchiveProgressChangedHandler UnarchiveProgressChanged;

        public Pack1Unarchiver(string packagePath, Pack1Meta metaData, string destinationDirPath, string key, string suffix = "")
            : this(packagePath, metaData, destinationDirPath, Encoding.ASCII.GetBytes(key), suffix, new BytesRange(0, -1))
        {
            // do nothing
        }

        public Pack1Unarchiver(string packagePath, Pack1Meta metaData, string destinationDirPath, string key, string suffix, BytesRange range)
            : this(packagePath, metaData, destinationDirPath, Encoding.ASCII.GetBytes(key), suffix, range)
        {
            // do nothing
        }

        public Pack1Unarchiver(string packagePath, Pack1Meta metaData, string destinationDirPath, byte[] key, string suffix, BytesRange range)
        {
            Checks.ArgumentFileExists(packagePath, "packagePath");
            Checks.ArgumentDirectoryExists(destinationDirPath, "destinationDirPath");
            Checks.ArgumentNotNull(suffix, "suffix");

            if (range.Start == 0)
            {
                Assert.AreEqual(MagicBytes.Pack1, MagicBytes.ReadFileType(packagePath), "Is not Pack1 format");
            }

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(packagePath, "packagePath");
            DebugLogger.LogVariable(destinationDirPath, "destinationDirPath");
            DebugLogger.LogVariable(suffix, "suffix");

            _packagePath = packagePath;
            _metaData = metaData;
            _destinationDirPath = destinationDirPath;
            _suffix = suffix;
            _range = range;

            using (var sha256 = SHA256.Create())
            {
                _key = sha256.ComputeHash(key);
            }

            _iv = Convert.FromBase64String(_metaData.Iv);
        }

        public void Unarchive(CancellationToken cancellationToken)
        {
            OnUnarchiveProgressChanged(null, false, 0, _metaData.Files.Length, 0.0);

            int entry = 1;
            
            DebugLogger.Log("Unpacking " + _metaData.Files.Length + " files...");
            foreach (var file in _metaData.Files)
            {
                OnUnarchiveProgressChanged(file.Name, file.Type == Pack1Meta.regularFileType, entry, _metaData.Files.Length, 0.0);

                var currentFile = file;
                var currentEntry = entry;

                if (CanUnpack(file))
                {
                    Unpack(file, progress =>
                    {
                        OnUnarchiveProgressChanged(currentFile.Name, currentFile.Type == Pack1Meta.regularFileType, currentEntry, _metaData.Files.Length, progress);
                    });
                }

                OnUnarchiveProgressChanged(file.Name, file.Type == Pack1Meta.regularFileType, entry, _metaData.Files.Length, 1.0);

                entry++;
            }
            DebugLogger.Log("Unpacking finished succesfully!");
        }

        public void UnarchiveSingleFile(Pack1Meta.FileEntry file, CancellationToken cancellationToken)
        {
            OnUnarchiveProgressChanged(file.Name, file.Type == Pack1Meta.regularFileType, 0, 1, 0.0);

            if (!CanUnpack(file))
            {
                throw new ArgumentOutOfRangeException("file", file, null);
            }

            Unpack(file, progress => OnUnarchiveProgressChanged(file.Name, file.Type == Pack1Meta.regularFileType, 1, 1, progress));

            OnUnarchiveProgressChanged(file.Name, file.Type == Pack1Meta.regularFileType, 0, 1, 1.0);
        }

        private bool CanUnpack(Pack1Meta.FileEntry file)
        {
            if (file.Type != Pack1Meta.regularFileType)
            {
                return true;
            }

            if (_range.Start == 0 && _range.End == -1)
            {
                return true;
            }

            return file.Offset > _range.Start && file.Offset + file.Size < _range.End;
        }

        private void Unpack(Pack1Meta.FileEntry file, Action<double> progress)
        {
            switch (file.Type)
            {
                case Pack1Meta.regularFileType:
                    UnpackRegularFile(file, progress);
                    break;
                case Pack1Meta.directoryFileType:
                    progress(0.0);
                    UnpackDirectory(file);
                    progress(1.0);
                    break;
                case Pack1Meta.symlinkFileType:
                    progress(0.0);
                    UnpackSymlink(file);
                    progress(1.0);
                    break;
                default:
                    DebugLogger.LogWarning("Unknown file type: " + file.Type);
                    break;
            }

        }

        private void UnpackDirectory(Pack1Meta.FileEntry file)
        {
            string destPath = Path.Combine(_destinationDirPath, file.Name);

            DebugLogger.Log("Creating directory " + destPath);
            Directory.CreateDirectory(destPath);
            DebugLogger.Log("Directory " + destPath + " created successfully!");
        }

        private void UnpackSymlink(Pack1Meta.FileEntry file)
        {
            string destPath = Path.Combine(_destinationDirPath, file.Name);
            DebugLogger.Log("Creating symlink: " + destPath);
            // TODO: how to create a symlink?
        }

        private void UnpackRegularFile(Pack1Meta.FileEntry file, Action<double> onProgress)
        {
            string destPath = Path.Combine(_destinationDirPath, file.Name + _suffix);
            DebugLogger.LogFormat("Unpacking regular file {0} to {1}", file, destPath);

            Files.CreateParents(destPath);

            RijndaelManaged rijn = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.None,
                KeySize = 256
            };

            ICryptoTransform decryptor = rijn.CreateDecryptor(_key, _iv);

            using (var fs = new FileStream(_packagePath, FileMode.Open))
            {
                fs.Seek(file.Offset.Value - _range.Start, SeekOrigin.Begin);

                using (var limitedStream = new LimitedStream(fs, file.Size.Value))
                {
                    using (var target = new FileStream(destPath, FileMode.Create))
                    {
                        ExtractFileFromStream(limitedStream, target, file, decryptor, onProgress);
                    }

                    if (Platform.IsPosix())
                    {
                        Chmod.SetMode(file.Mode.Substring(3), destPath);
                    }
                }
            }

            DebugLogger.Log("File " + file.Name + " unpacked successfully!");
        }

        private void ExtractFileFromStream(
            Stream sourceStream,
            Stream targetStream,
            Pack1Meta.FileEntry file,
            ICryptoTransform decryptor,
            Action<double> onProgress)
        {
            using (var cryptoStream = new CryptoStream(sourceStream, decryptor, CryptoStreamMode.Read))
            {
                using (var gzipStream = new GZipStream(cryptoStream, Ionic.Zlib.CompressionMode.Decompress))
                {
                    long bytesProcessed = 0;
                    const int bufferSize = 128 * 1024;
                    var buffer = new byte[bufferSize];
                    int count;
                    while ((count = gzipStream.Read(buffer, 0, bufferSize)) != 0)
                    {
                        targetStream.Write(buffer, 0, count);
                        bytesProcessed += count;
                        onProgress(bytesProcessed / (double) file.Size.Value);
                    }
                }
            }
        }

        protected virtual void OnUnarchiveProgressChanged(string name, bool isFile, int entry, int amount, double entryProgress)
        {
            var handler = UnarchiveProgressChanged;
            if (handler != null)
            {
                handler(name, isFile, entry, amount, entryProgress);
            }
        }
    }
}