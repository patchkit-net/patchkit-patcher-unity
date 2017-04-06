using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
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
        private readonly Pack1MetaData _metaData;
        private readonly string _destinationDirPath;
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public event UnarchiveProgressChangedHandler UnarchiveProgressChanged;

        public Pack1Unarchiver(string packagePath, string metaData, string destinationDirPath, string key)
            : this(packagePath, metaData, destinationDirPath, Encoding.ASCII.GetBytes(key))
        {
            // do nothing
        }

        public Pack1Unarchiver(string packagePath, string metaData, string destinationDirPath, byte[] key)
        {
            Checks.ArgumentFileExists(packagePath, "packagePath");
            Checks.ArgumentDirectoryExists(destinationDirPath, "destinationDirPath");
            Assert.AreEqual(MagicBytes.Pack1, MagicBytes.ReadFileType(packagePath), "Is not Pack1 format");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(packagePath, "packagePath");
            DebugLogger.LogVariable(destinationDirPath, "destinationDirPath");

            _packagePath = packagePath;
            _metaData = JsonConvert.DeserializeObject<Pack1MetaData>(metaData);
            _destinationDirPath = destinationDirPath;

            using (var sha256 = SHA256.Create())
            {
                _key = sha256.ComputeHash(key);
            }

            _iv = Convert.FromBase64String(_metaData.Iv);
        }

        public void Unarchive(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Unpacking " + _metaData.Files.Length + " files...");
            foreach (Pack1MetaData.File file in _metaData.Files)
            {
                Unpack(file);
            }
            DebugLogger.Log("Unpacking finished succesfully!");
        }

        private void Unpack(Pack1MetaData.File file)
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

        private void UnpackDirectory(Pack1MetaData.File file)
        {
            string destPath = Path.Combine(_destinationDirPath, file.Name);

            DebugLogger.Log("Creating directory " + destPath);
            Directory.CreateDirectory(destPath);
            DebugLogger.Log("Directory " + destPath + " created successfully!");
        }

        private void UnpackSymlink(Pack1MetaData.File file)
        {
            string destPath = Path.Combine(_destinationDirPath, file.Name);
            DebugLogger.Log("Creating symlink: " + destPath);
        }

        private void UnpackRegularFile(Pack1MetaData.File file)
        {
            DebugLogger.Log("Unpacking regular file " + file);

            RijndaelManaged rijn = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.None,
                KeySize = 256
            };

            using (var fs = new FileStream(_packagePath, FileMode.Open))
            {
                fs.Seek(file.Offset, SeekOrigin.Begin);

                using (var limitedStream = new LimitedStream(fs, file.Size))
                {
                    ICryptoTransform decryptor = rijn.CreateDecryptor(_key, _iv);
                    using (var cryptoStream = new CryptoStream(limitedStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (var gzipStream = new GZipStream(cryptoStream, CompressionMode.Decompress))
                        {
                            string destPath = Path.Combine(_destinationDirPath, file.Name);
                            using (var fileWritter = new FileStream(destPath, FileMode.CreateNew))
                            {
                                Streams.Copy(gzipStream, fileWritter);
                                Chmod.SetMode(file.Mode.Substring(3), destPath);
                            }
                        }
                    }
                }
            }

            DebugLogger.Log("File " + file.Name + " unpacked successfully!");
        }

        private class Pack1MetaData
        {
            public string Version { get; set; }
            public string Encryption { get; set; }
            public string Compression { get; set; }
            public string Iv { get; set; }

            public File[] Files { get; set; }

            public class File
            {
                public string Name { get; set; }
                public string Type { get; set; }
                public string Target { get; set; }
                public string Mode { get; set; }
                public long Offset { get; set; }
                public long Size { get; set; }

                public override string ToString()
                {
                    return string.Format("Name: {0}, Type: {1}, Target: {2}, Mode: {3}", Name, Type, Target, Mode);
                }
            }
        }
    }
}