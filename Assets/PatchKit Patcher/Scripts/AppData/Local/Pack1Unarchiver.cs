using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Ionic.Zlib;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Data;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;
using CompressionMode = System.IO.Compression.CompressionMode;

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

        public event UnarchiveProgressChangedHandler UnarchiveProgressChanged;

        public Pack1Unarchiver(string packagePath, Pack1Meta metaData, string destinationDirPath, string key, string suffix = "")
            : this(packagePath, metaData, destinationDirPath, Encoding.ASCII.GetBytes(key), suffix)
        {
            // do nothing
        }

        public Pack1Unarchiver(string packagePath, Pack1Meta metaData, string destinationDirPath, byte[] key, string suffix)
        {
            Checks.ArgumentFileExists(packagePath, "packagePath");
            Checks.ArgumentDirectoryExists(destinationDirPath, "destinationDirPath");
            Assert.AreEqual(MagicBytes.Pack1, MagicBytes.ReadFileType(packagePath), "Is not Pack1 format");
            Checks.ArgumentNotNull(suffix, "suffix");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(packagePath, "packagePath");
            DebugLogger.LogVariable(destinationDirPath, "destinationDirPath");
            DebugLogger.LogVariable(suffix, "suffix");

            _packagePath = packagePath;
            _metaData = metaData;
            _destinationDirPath = destinationDirPath;
            _suffix = suffix;

            using (var sha256 = SHA256.Create())
            {
                _key = sha256.ComputeHash(key);
            }

            _iv = Convert.FromBase64String(_metaData.Iv);
        }

        public void Unarchive(CancellationToken cancellationToken)
        {
            OnUnarchiveProgressChanged(null, false, 0, _metaData.Files.Length);

            int entry = 0;
            
            DebugLogger.Log("Unpacking " + _metaData.Files.Length + " files...");
            foreach (Pack1Meta.FileEntry file in _metaData.Files)
            {
                Unpack(file);

                entry++;

                OnUnarchiveProgressChanged(file.Name, file.Type == "regular", entry, _metaData.Files.Length);
            }
            DebugLogger.Log("Unpacking finished succesfully!");
        }

        private void Unpack(Pack1Meta.FileEntry file)
        {
            switch (file.Type)
            {
                case "regular":
                    UnpackRegularFile(file);
                    break;
                case "directory":
                    UnpackDirectory(file);
                    break;
                case "symlink":
                    UnpackSymlink(file);
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

        private void UnpackRegularFile(Pack1Meta.FileEntry file)
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

            using (var fs = new FileStream(_packagePath, FileMode.Open))
            {
                fs.Seek(file.Offset.Value, SeekOrigin.Begin);

                using (var limitedStream = new LimitedStream(fs, file.Size.Value))
                {
                    ICryptoTransform decryptor = rijn.CreateDecryptor(_key, _iv);
                    using (var cryptoStream = new CryptoStream(limitedStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (var gzipStream = new GZipStream(cryptoStream, Ionic.Zlib.CompressionMode.Decompress))
                        {
                            using (var fileWritter = new FileStream(destPath, FileMode.Create))
                            {
                                Streams.Copy(gzipStream, fileWritter);
                                if (Platform.IsPosix())
                                {
                                    Chmod.SetMode(file.Mode.Substring(3), destPath);
                                }
                            }
                        }
                    }
                }
            }

            DebugLogger.Log("File " + file.Name + " unpacked successfully!");
        }

        protected virtual void OnUnarchiveProgressChanged(string name, bool isFile, int entry, int amount)
        {
            var handler = UnarchiveProgressChanged;
            if (handler != null)
            {
                handler(name, isFile, entry, amount);
            }
        }
    }
}