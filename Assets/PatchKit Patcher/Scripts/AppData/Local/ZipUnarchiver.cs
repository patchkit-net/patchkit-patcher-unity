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

        public event UnarchiveProgressChangedHandler UnarchiveProgressChanged;

        public ZipUnarchiver(string packagePath, string destinationDirPath, string password = null)
        {
            Checks.ArgumentFileExists(packagePath, "packagePath");
            Checks.ArgumentDirectoryExists(destinationDirPath, "destinationDirPath");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(packagePath, "packagePath");
            DebugLogger.LogVariable(destinationDirPath, "destinationDirPath");

            _packagePath = packagePath;
            _destinationDirPath = destinationDirPath;
            _password = password;
        }

        public void Unarchive(CancellationToken cancellationToken)
        {
            Assert.MethodCalledOnlyOnce(ref _unarchiveHasBeenCalled, "Unarchive");

            DebugLogger.Log("Unarchiving.");

            using (var zip = ZipFile.Read(_packagePath))
            {
                zip.Password = _password;

                int entry = 0;

                OnUnarchiveProgressChanged(null, false, 0, zip.Count);

                foreach (var zipEntry in zip)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    UnarchiveEntry(zipEntry);

                    entry++;

                    OnUnarchiveProgressChanged(zipEntry.FileName, !zipEntry.FileName.EndsWith("/"), entry, zip.Count);
                }
            }
        }

        private void UnarchiveEntry(ZipEntry zipEntry)
        {
            DebugLogger.Log(string.Format("Unarchiving entry {0}", zipEntry.FileName));

            zipEntry.Extract(_destinationDirPath, ExtractExistingFileAction.OverwriteSilently);
        }

        protected virtual void OnUnarchiveProgressChanged(string name, bool isFile, int entry, int amount)
        {
            if (UnarchiveProgressChanged != null) UnarchiveProgressChanged(name, isFile, entry, amount);
        }
    }
}