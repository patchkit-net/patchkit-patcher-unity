using System.IO;
using Ionic.Zip;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Statistics;
using UnityEngine;

namespace PatchKit.Unity.Patcher.Zip
{
    internal class Unzipper
    {
        public void Unzip(string packageFilePath, string destinationDirPath, string password,
            CustomProgressReporter<UnzipperProgress> progressReporter, CancellationToken cancellationToken)
        {
            Debug.Log(string.Format("Unzipping package {0} to {1}", packageFilePath, destinationDirPath));

            using (var zip = ZipFile.Read(packageFilePath))
            {
                zip.Password = password;
                int entryCounter = 0;

                progressReporter.Progress = new UnzipperProgress
                {
                    FileName = null,
                    Progress = 0.0
                };

                Directory.CreateDirectory(destinationDirPath);

                foreach (ZipEntry zipEntry in zip)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Debug.Log(string.Format("Extracting {0}", zipEntry.FileName));

                    zipEntry.Extract(destinationDirPath, ExtractExistingFileAction.OverwriteSilently);

                    entryCounter++;

                    if (!zipEntry.IsDirectory)
                    {
                        progressReporter.Progress = new UnzipperProgress
                        {
                            FileName = zipEntry.FileName,
                            Progress = entryCounter/(double) zip.Count
                        };
                    }
                }

                progressReporter.Progress = new UnzipperProgress
                {
                    FileName = null,
                    Progress = 1.0
                };
            }
        }
    }
}