using Ionic.Zip;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public class Unarchiver
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(Unarchiver));

        private readonly string _packagePath;
        private readonly string _destinationDirPath;

        private bool _unarchiveHasBeenCalled;

        public event UnarchiveProgressChangedHandler UnarchiveProgressChanged;

        public Unarchiver(string packagePath, string destinationDirPath)
        {
            Checks.ArgumentFileExists(packagePath, "packagePath");
            Checks.ArgumentDirectoryExists(destinationDirPath, "destinationDirPath");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(packagePath, "packagePath");
            DebugLogger.LogVariable(destinationDirPath, "destinationDirPath");

            _packagePath = packagePath;
            _destinationDirPath = destinationDirPath;
        }

        public void Unarchive(CancellationToken cancellationToken)
        {
            AssertChecks.MethodCalledOnlyOnce(ref _unarchiveHasBeenCalled, "Unarchive");

            DebugLogger.Log("Unarchiving.");

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