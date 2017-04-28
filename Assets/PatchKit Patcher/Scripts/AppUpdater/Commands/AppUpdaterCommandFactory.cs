using System.IO;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class AppUpdaterCommandFactory
    {
        public IDownloadPackageCommand CreateDownloadContentPackageCommand(int versionId, string keySecret, AppUpdaterContext context)
        {
            var resource = context.App.RemoteData.GetContentPackageResource(versionId, keySecret);
            var destinationFilePath = context.App.DownloadDirectory.GetContentPackagePath(versionId);

            context.App.DownloadDirectory.PrepareForWriting();

            return new DownloadPackageCommand(resource, destinationFilePath, context.Configuration.UseTorrents);
        }

        public IDownloadPackageCommand CreateDownloadDiffPackageCommand(int versionId, string keySecret, AppUpdaterContext context)
        {
            var resource = context.App.RemoteData.GetDiffPackageResource(versionId, keySecret);
            var destinationFilePath = context.App.DownloadDirectory.GetDiffPackagePath(versionId);

            context.App.DownloadDirectory.PrepareForWriting();

            return new DownloadPackageCommand(resource, destinationFilePath, context.Configuration.UseTorrents);
        }

        public IInstallContentCommand CreateInstallContentCommand(int versionId, AppUpdaterContext context)
        {
            var packagePath = context.App.DownloadDirectory.GetContentPackagePath(versionId);
            var versionContentSummary = context.App.RemoteMetaData.GetContentSummary(versionId);
            var packagePassword = context.App.RemoteData.GetContentPackageResourcePassword(versionId);

            return new InstallContentCommand(packagePath,
                packagePassword,
                versionId,
                versionContentSummary,
                context.App.LocalDirectory,
                context.App.LocalMetaData,
                context.App.TemporaryDirectory);
        }

        public IInstallDiffCommand CreateInstallDiffCommand(int versionId, AppUpdaterContext context)
        {
            var packagePath = context.App.DownloadDirectory.GetDiffPackagePath(versionId);
            var versionDiffSummary = context.App.RemoteMetaData.GetDiffSummary(versionId);
            var packagePassword = context.App.RemoteData.GetDiffPackageResourcePassword(versionId);

            return new InstallDiffCommand(packagePath,
                packagePassword,
                versionId,
                versionDiffSummary,
                context.App.LocalDirectory,
                context.App.LocalMetaData,
                context.App.TemporaryDirectory);
        }

        public ICheckVersionIntegrityCommand CreateCheckVersionIntegrityCommand(int versionId, AppUpdaterContext context)
        {
            var versionContentSummary = context.App.RemoteMetaData.GetContentSummary(versionId);

            return new CheckVersionIntegrityCommand(versionId,
                versionContentSummary,
                context.App.LocalDirectory,
                context.App.LocalMetaData);
        }

        public IUninstallCommand CreateUninstallCommand(AppUpdaterContext context)
        {
            return new UninstallCommand(context.App.LocalDirectory, context.App.LocalMetaData);
        }

        public IValidateLicenseCommand CreateValidateLicenseCommand(AppUpdaterContext context)
        {
            return new ValidateLicenseCommand(context.LicenseDialog, context.App.RemoteMetaData);
        }

        public ICheckDiskSpace CreateCheckDiskSpaceCommand(int versionId, AppUpdaterContext context)
        {
            // get project biggest file size
            long biggestFileSize = 0;
            string[] registeredEntries = context.App.LocalMetaData.GetRegisteredEntries();
            foreach (string entry in registeredEntries)
            {
                string filePath = context.App.LocalDirectory.Path.PathCombine(entry);
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists && fileInfo.Length > biggestFileSize)
                {
                    biggestFileSize = fileInfo.Length;
                }
            }

            AppDiffSummary diffSummary = context.App.RemoteMetaData.GetDiffSummary(versionId);
            return new CheckDiskSpaceCommand(diffSummary, context.App.LocalDirectory.Path, biggestFileSize);
        }
    }
}