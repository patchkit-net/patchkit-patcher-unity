using System.IO;
using Ionic.Zip;
using PatchKit.API.Async;

namespace PatchKit.Unity.Patcher.Utilities
{
    internal class Unarchiver
    {
        public delegate void UnarchiveProgressHandler(float progress);

        public void Unarchive(string packagePath, string destinationPath, UnarchiveProgressHandler onUnarchiveProgress, AsyncCancellationToken cancellationToken)
        {
            using (var zip = ZipFile.Read(packagePath))
            {
                int entryCounter = 0;

                onUnarchiveProgress(0.0f);

                Directory.CreateDirectory(destinationPath);

                foreach (ZipEntry zipEntry in zip)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    zipEntry.Extract(destinationPath, ExtractExistingFileAction.OverwriteSilently);

                    entryCounter++;

                    if (!zipEntry.IsDirectory)
                    {
                        onUnarchiveProgress(entryCounter / (float)zip.Count);
                    }
                }

                onUnarchiveProgress(1.0f);
            }
        }
    }
}