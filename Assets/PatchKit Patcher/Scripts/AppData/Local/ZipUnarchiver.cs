using System.IO;
using Ionic.Zip;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    /// <summary>
    /// The unarchiver.
    /// </summary>
    public class ZipUnarchiver : IUnarchiver
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(ZipUnarchiver));

        private readonly string _packagePath;
        private readonly string _destinationDirPath;
        private readonly string _password;

        private bool _unarchiveHasBeenCalled;
        private MapHashExtractedFiles _mapHashExtractedFiles;

        public event UnarchiveProgressChangedHandler UnarchiveProgressChanged;

        // not used
        public bool ContinueOnError { private get; set; }

        // not used
        public bool HasErrors { get; private set; }

        public ZipUnarchiver(string packagePath, string destinationDirPath, MapHashExtractedFiles mapHashExtractedFiles, string password = null)
        {
            Checks.ArgumentFileExists(packagePath, "packagePath");
            Checks.ArgumentDirectoryExists(destinationDirPath, "destinationDirPath");
            Checks.ArgumentNotNull(mapHashExtractedFiles, "mapHashExtractedFiles");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(packagePath, "packagePath");
            DebugLogger.LogVariable(destinationDirPath, "destinationDirPath");

            _packagePath = packagePath;
            _destinationDirPath = destinationDirPath;
            _password = password;
            _mapHashExtractedFiles = mapHashExtractedFiles;
        }

        public void Unarchive(CancellationToken cancellationToken)
        {
            Assert.MethodCalledOnlyOnce(ref _unarchiveHasBeenCalled, "Unarchive");

            DebugLogger.Log("Unarchiving.");

            using (var zip = ZipFile.Read(_packagePath))
            {
                zip.Password = _password;

                int entry = 1;

                foreach (var zipEntry in zip)
                {
                    OnUnarchiveProgressChanged(zipEntry.FileName, !zipEntry.IsDirectory, entry, zip.Count, 0.0);

                    cancellationToken.ThrowIfCancellationRequested();

                    UnarchiveEntry(zipEntry);

                    OnUnarchiveProgressChanged(zipEntry.FileName, !zipEntry.IsDirectory, entry, zip.Count, 1.0);
                    
                    entry++;
                }
            }
        }

        private void UnarchiveEntry(ZipEntry zipEntry)
        {
            DebugLogger.Log(string.Format("Unarchiving entry {0}", zipEntry.FileName));
            string destPath = Path.Combine(_destinationDirPath, _mapHashExtractedFiles.GetNameHash(zipEntry.FileName));
            using (var target = new FileStream(destPath, FileMode.Create))
            {
                zipEntry.Extract(target);
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