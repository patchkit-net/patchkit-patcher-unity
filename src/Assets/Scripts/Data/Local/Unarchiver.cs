using System;
using Ionic.Zip;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.Data.Local
{
    internal class Unarchiver
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(Unarchiver));

        private readonly string _packagePath;
        private readonly string _destinationDirPath;

        private bool _started;

        public event UnarchiveProgressChangedHandler UnarchiveProgressChanged;

        public Unarchiver(string packagePath, string destinationDirPath)
        {
            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(packagePath, "packagePath");
            DebugLogger.LogVariable(destinationDirPath, "destinationDirPath");

            Checks.ArgumentFileExists(packagePath, "packagePath");
            Checks.ArgumentDirectoryExists(destinationDirPath, "destinationDirPath");

            _packagePath = packagePath;
            _destinationDirPath = destinationDirPath;
        }

        public void Unarchive(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Unarchiving.");

            if (_started)
            {
                throw new InvalidOperationException("Cannot start the same Unarchiver twice.");
            }
            _started = true;

            using (var zip = ZipFile.Read(_packagePath))
            {
                int entry = 0;

                foreach (var zipEntry in zip)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    OnUnarchiveProgressChanged(zipEntry.FileName, entry, zip.Count);

                    UnarchiveEntry(zipEntry);

                    entry++;
                }
            }
        }

        private void UnarchiveEntry(ZipEntry zipEntry)
        {
            DebugLogger.Log(string.Format("Unarchiving entry {0}", zipEntry.FileName));

            zipEntry.Extract(_destinationDirPath, ExtractExistingFileAction.OverwriteSilently);
        }

        protected virtual void OnUnarchiveProgressChanged(string name, int entry, int amount)
        {
            if (UnarchiveProgressChanged != null) UnarchiveProgressChanged(name, entry, amount);
        }
    }
}