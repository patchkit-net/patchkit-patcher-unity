using System.IO;
using Ionic.Zip;
using PatchKit.Async;

namespace PatchKit.Unity.Patcher
{
    internal class Unarchiver
    {
        public delegate void UnarchiverProgressHandler(float progress);

        public void Unarchive(string packagePath, string destinationPath, UnarchiverProgressHandler onUnarchiverProgress, AsyncCancellationToken cancellationToken)
        {
            using (var zip = ZipFile.Read(packagePath))
            {
                int entryCounter = 0;

                onUnarchiverProgress(0.0f);

                Directory.CreateDirectory(destinationPath);

                foreach (ZipEntry zipEntry in zip)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    zipEntry.Extract(destinationPath, ExtractExistingFileAction.OverwriteSilently);

                    entryCounter++;

                    if (!zipEntry.IsDirectory)
                    {
                        onUnarchiverProgress(entryCounter / (float)zip.Count);
                    }
                }

                onUnarchiverProgress(1.0f);
            }
        }
    }
}