using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Ionic.Zlib;
using PatchKit.Network;
using PatchKit.Unity.Patcher.AppData.FileSystem;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Data;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;
using SharpCompress.Compressors.LZMA;
using SharpCompress.Compressors.Xz;
using SharpRaven;
using SharpRaven.Data;
using SharpRaven.Utilities;

namespace PatchKit.Unity.Patcher.AppData.Local
{

    /// <summary>
    /// Pack1 format unarchiver.
    /// http://redmine.patchkit.net/projects/patchkit-documentation/wiki/Pack1_File_Format
    /// </summary>
    public class Pack1Unarchiver : IUnarchiver
    {
        private delegate Stream DecompressorCreator(Stream source);

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(Pack1Unarchiver));

        private readonly string _packagePath;
        private readonly Pack1Meta _metaData;
        private readonly string _destinationDirPath;
        private readonly string _suffix;
        private readonly byte[] _key;
        private readonly byte[] _iv;
        private int _processedFiles = 0; // debugging

        /// <summary>
        /// The range (in bytes) of the partial pack1 source file
        /// </summary>
        private readonly BytesRange _range;

        public event UnarchiveProgressChangedHandler UnarchiveProgressChanged;

        // set to true to continue unpacking on error. Check HasErrors later to see if there are any
        public bool ContinueOnError { private get; set; }

        // After Unarchive() finishes if this set to true, there were unpacking errors.
        public bool HasErrors { get; private set; }

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

        private Pack1Unarchiver(string packagePath, Pack1Meta metaData, string destinationDirPath, byte[] key, string suffix, BytesRange range)
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
            int entry = 1;
            HasErrors = false;

            DebugLogger.Log("Unpacking " + _metaData.Files.Length + " files...");
            foreach (var file in _metaData.Files)
            {
                OnUnarchiveProgressChanged(file.Name, file.Type == Pack1Meta.RegularFileType, entry, _metaData.Files.Length, 0.0);

                var currentFile = file;
                var currentEntry = entry;

                if (CanUnpack(file))
                {
                    Unpack(file, progress =>
                    {
                        OnUnarchiveProgressChanged(currentFile.Name, currentFile.Type == Pack1Meta.RegularFileType, currentEntry, _metaData.Files.Length, progress);
                    }, cancellationToken);
                }
                else
                {
                    DebugLogger.LogWarning(string.Format("The file {0} couldn't be unpacked.", file.Name));
                }

                OnUnarchiveProgressChanged(file.Name, file.Type == Pack1Meta.RegularFileType, entry, _metaData.Files.Length, 1.0);

                entry++;
            }
            DebugLogger.Log("Unpacking finished succesfully!");
        }

        public void UnarchiveSingleFile(Pack1Meta.FileEntry file, CancellationToken cancellationToken, string destinationDirPath = null)
        {
            OnUnarchiveProgressChanged(file.Name, file.Type == Pack1Meta.RegularFileType, 0, 1, 0.0);

            if (!CanUnpack(file))
            {
                throw new ArgumentOutOfRangeException("file", file, null);
            }

            Unpack(file, progress => OnUnarchiveProgressChanged(file.Name, file.Type == Pack1Meta.RegularFileType, 1, 1, progress), cancellationToken, destinationDirPath);

            OnUnarchiveProgressChanged(file.Name, file.Type == Pack1Meta.RegularFileType, 0, 1, 1.0);
        }

        private bool CanUnpack(Pack1Meta.FileEntry file)
        {
            if (file.Type != Pack1Meta.RegularFileType)
            {
                return true;
            }

            if (_range.Start == 0 && _range.End == -1)
            {
                return true;
            }

            return file.Offset >= _range.Start && file.Offset + file.Size <= _range.End;
        }

        private void Unpack(Pack1Meta.FileEntry file, Action<double> progress, CancellationToken cancellationToken, string destinationDirPath = null)
        {
            switch (file.Type)
            {
                case Pack1Meta.RegularFileType:
                    try
                    {
                        UnpackRegularFile(file, progress, cancellationToken, destinationDirPath);
                    }
                    catch (Ionic.Zlib.ZlibException e)
                    {
                        if (ContinueOnError)
                        {
                            DebugLogger.LogWarning("ZlibException caught. The process will continue, but I will try to repair it later.");
                            DebugLogger.LogException(e);
                            HasErrors = true;
                        }
                        else
                        {
                            throw;
                        }
                    }
                    finally
                    {
                        _processedFiles += 1;
                    }
                    
                    break;
                case Pack1Meta.DirectoryFileType:
                    progress(0.0);
                    UnpackDirectory(file, cancellationToken);
                    progress(1.0);
                    break;
                case Pack1Meta.SymlinkFileType:
                    progress(0.0);
                    UnpackSymlink(file);
                    progress(1.0);
                    break;
                default:
                    DebugLogger.LogWarning("Unknown file type: " + file.Type);
                    break;
            }

        }

        private void UnpackDirectory(Pack1Meta.FileEntry file, CancellationToken cancellationToken)
        {
            string destPath = Path.Combine(_destinationDirPath, file.Name);

            DebugLogger.Log("Creating directory " + destPath);
            DirectoryOperations.CreateDirectory(destPath, cancellationToken);
            DebugLogger.Log("Directory " + destPath + " created successfully!");
        }

        private void UnpackSymlink(Pack1Meta.FileEntry file)
        {
            string destPath = Path.Combine(_destinationDirPath, file.Name);
            DebugLogger.Log("Creating symlink: " + destPath);
            // TODO: how to create a symlink?
        }

        private DecompressorCreator ResolveDecompressor(Pack1Meta meta)
        {
            switch (meta.Compression)
            {
                case Pack1Meta.XZCompression:
                    return CreateXzDecompressor;

                case Pack1Meta.GZipCompression:
                    return CreateGzipDecompressor;

                default:
                    return CreateGzipDecompressor;
            }
        }

        private void UnpackRegularFile(Pack1Meta.FileEntry file, Action<double> onProgress, CancellationToken cancellationToken, string destinationDirPath = null)
        {
            string destPath = Path.Combine(destinationDirPath == null ? _destinationDirPath : destinationDirPath, file.Name + _suffix);
            DebugLogger.LogFormat("Unpacking regular file {0} to {1}", file, destPath);

            if (file.Size == null)
            {
                throw new NullReferenceException("File size cannot be null for regular file.");
            }

            if (file.Offset == null)
            {
                throw new NullReferenceException("File offset cannot be null for regular file.");
            }

            Files.CreateParents(destPath);

            var rijn = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.None,
                KeySize = 256
            };

            var aesAlg = new AesCryptoServiceProvider
            {
                IV = _iv,
                Key = _key
            };

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(
                aesAlg.Key,
                aesAlg.IV);

            DecompressorCreator decompressorCreator = ResolveDecompressor(_metaData);

            using (var fs = new FileStream(_packagePath, FileMode.Open))
            {
                fs.Seek(file.Offset.Value - _range.Start, SeekOrigin.Begin);

                using (var limitedStream = new BufferedStream(new LimitedStream(fs, file.Size.Value), 2 * 1024 * 1024))
                {
                    //using (var bufferedLimitedStream = new ThreadBufferedStream(limitedStream, 8 * 1024 * 1024))
                    {
                        using (var target = new FileStream(destPath, FileMode.Create))
                        {
                            ExtractFileFromStream(limitedStream, target, file.Size.Value, decryptor, decompressorCreator, onProgress, cancellationToken);
                            DebugTestCorruption(target);
                        }
                    }

                    if (Platform.IsPosix())
                    {
                        Chmod.SetMode(file.Mode.Substring(3), destPath);
                    }
                }
            }

            DebugLogger.Log("File " + file.Name + " unpacked successfully!");
        }

        // allows to test corruption if valid environment variable is set
        private void DebugTestCorruption(FileStream target)
        {
            if (_processedFiles == 0)
            {
                // do not corrupt the first file
                return;
            }

            if (
                _processedFiles % 10 == 0 && EnvironmentInfo.GetEnvironmentVariable(EnvironmentVariables.CorruptFilesOnUnpack10, "") == "1"
                ||
                _processedFiles % 50 == 0 && EnvironmentInfo.GetEnvironmentVariable(EnvironmentVariables.CorruptFilesOnUnpack50, "") == "1"
                ||
                _processedFiles % 300 == 0 && EnvironmentInfo.GetEnvironmentVariable(EnvironmentVariables.CorruptFilesOnUnpack300, "") == "1"
                )
            {
                DebugLogger.LogWarning("DEBUG: Writing extra byte and triggering zlibexception");

                target.Write(new byte[] { 1 }, 0, 1);
                throw new Ionic.Zlib.ZlibException();
            }
        }

        private Stream CreateXzDecompressor(Stream source)
        {
            if (source.CanSeek)
            {
                return new XZStream(source);
            }
            else
            {
                return new XZStream(new PositionAwareStream(source));
            }
        }

        private Stream CreateGzipDecompressor(Stream source)
        {
            return new GZipStream(source, CompressionMode.Decompress);
        }

        private void ExtractFileFromStream(
            Stream sourceStream,
            Stream targetStream,
            long fileSize,
            ICryptoTransform decryptor,
            DecompressorCreator createDecompressor,
            Action<double> onProgress,
            CancellationToken cancellationToken)
        {
            using (var cryptoStream = new CryptoStream(sourceStream, decryptor, CryptoStreamMode.Read))
            {
                using (var bufferedCryptoStream = new BufferedStream(new ThreadBufferedStream(cryptoStream, 2 * 1024 * 1024), 2 * 1024 * 1024))
                {
                    //using (var wrapperStream = new GZipReadWrapperStream(bufferedCryptoStream))
                    {
                        using (Stream decompressionStream = createDecompressor(bufferedCryptoStream))
                        {
                            using (var bufferedDecompressionStream = new ThreadBufferedStream(decompressionStream, 4 * 1024 * 1024))
                            {
                                try
                                {
                                    const int bufferSize = 2 * 1024 * 1024;
                                    var buffer = new byte[bufferSize];
                                    int count;

                                    while ((count = bufferedDecompressionStream.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        cancellationToken.ThrowIfCancellationRequested();
                                        targetStream.Write(buffer, 0, count);

                                        long bytesProcessed = sourceStream.Position;
                                        onProgress(bytesProcessed / (double) fileSize);
                                    }
                                }
                                catch (OperationCanceledException)
                                {
                                    throw;
                                }
                                catch (Exception e)
                                {
                                    DebugLogger.LogException(e);

                                    PatcherLogManager logManager = PatcherLogManager.Instance;
                                    PatcherLogSentryRegistry sentryRegistry = logManager.SentryRegistry;
                                    RavenClient ravenClient = sentryRegistry.RavenClient;

                                    var sentryEvent = new SentryEvent(e);
                                    PatcherLogSentryRegistry.AddDataToSentryEvent(sentryEvent,
                                        logManager.Storage.Guid.ToString());
                                    ravenClient.Capture(sentryEvent);

                                    throw;
                                }
                            }
                        }
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