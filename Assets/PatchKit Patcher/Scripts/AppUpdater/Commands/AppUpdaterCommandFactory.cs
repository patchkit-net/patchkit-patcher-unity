using System;
using System.IO;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class AppUpdaterCommandFactory
    {
        public IDownloadPackageCommand CreateDownloadContentPackageCommand(int versionId, string keySecret,
            string countryCode, AppUpdaterContext context, CancellationToken cancellationToken)
        {
            var resource = context.App.RemoteData.GetContentPackageResource(versionId, keySecret, countryCode, cancellationToken);

            var appDownloadDirectory = context.App.DownloadDirectory;
            var destinationFilePath = appDownloadDirectory.GetContentPackagePath(versionId);
            string destinationMetaPath = appDownloadDirectory.GetContentPackageMetaPath(versionId);

            appDownloadDirectory.PrepareForWriting();

            return new DownloadPackageCommand(resource, destinationFilePath, destinationMetaPath);
        }

        public IDownloadPackageCommand CreateDownloadDiffPackageCommand(int versionId, string keySecret,
            string countryCode, AppUpdaterContext context, CancellationToken cancellationToken)
        {
            var resource = context.App.RemoteData.GetDiffPackageResource(versionId, keySecret, countryCode, cancellationToken);

            var appDownloadDirectory = context.App.DownloadDirectory;
            var destinationFilePath = appDownloadDirectory.GetDiffPackagePath(versionId);
            string destinationMetaPath = appDownloadDirectory.GetDiffPackageMetaPath(versionId);

            appDownloadDirectory.PrepareForWriting();

            return new DownloadPackageCommand(resource, destinationFilePath, destinationMetaPath);
        }

        public IRepairFilesCommand CreateRepairFilesCommand(int versionId, AppUpdaterContext context, RemoteResource resource, Pack1Meta.FileEntry[] brokenFiles, Pack1Meta meta)
        {
            var packagePath = context.App.DownloadDirectory.GetContentPackagePath(versionId);
            var packagePassword = context.App.RemoteData.GetContentPackageResourcePassword(versionId);

            return new RepairFilesCommand(resource, meta, brokenFiles, packagePath, packagePassword, context.App.LocalDirectory);
        }

        public IInstallContentCommand CreateInstallContentCommand(int versionId, AppUpdaterContext context, CancellationToken cancellationToken)
        {
            var packagePath = context.App.DownloadDirectory.GetContentPackagePath(versionId);
            var packageMetaPath = context.App.DownloadDirectory.GetContentPackageMetaPath(versionId);
            var versionContentSummary = context.App.RemoteMetaData.GetContentSummary(versionId, cancellationToken);
            var packagePassword = context.App.RemoteData.GetContentPackageResourcePassword(versionId);

            return new InstallContentCommand(packagePath,
                packageMetaPath,
                packagePassword,
                versionId,
                versionContentSummary,
                context.App.LocalDirectory,
                context.App.LocalMetaData);
        }

        public IInstallDiffCommand CreateInstallDiffCommand(int versionId, AppUpdaterContext context)
        {
            var packagePath = context.App.DownloadDirectory.GetDiffPackagePath(versionId);
            var packageMetaPath = context.App.DownloadDirectory.GetDiffPackageMetaPath(versionId);
            var packagePassword = context.App.RemoteData.GetDiffPackageResourcePassword(versionId);

            return new InstallDiffCommand(packagePath,
                packageMetaPath,
                packagePassword,
                versionId,
                context.App.LocalDirectory,
                context.App.LocalMetaData,
                context.App.RemoteMetaData);
        }

        public ICheckVersionIntegrityCommand CreateCheckVersionIntegrityCommand(int versionId, AppUpdaterContext context,
                bool isCheckingHash, bool isCheckingSize, CancellationToken cancellationToken)
        {
            var versionContentSummary = context.App.RemoteMetaData.GetContentSummary(versionId, cancellationToken);

            return new CheckVersionIntegrityCommand(versionId,
                versionContentSummary,
                context.App.LocalDirectory,
                context.App.LocalMetaData,
                isCheckingHash, isCheckingSize);
        }

        public IUninstallCommand CreateUninstallCommand(AppUpdaterContext context)
        {
            return new UninstallCommand(context.App.LocalDirectory, context.App.LocalMetaData);
        }

        public IValidateLicenseCommand CreateValidateLicenseCommand(AppUpdaterContext context)
        {
            Assert.IsNotNull(Patcher.Instance.Data);

            return new ValidateLicenseCommand(context.LicenseDialog, context.App.RemoteMetaData, context.App.LocalMetaData,
                new UnityCache(Patcher.Instance.Data.Value.AppSecret), PatcherLogManager.DefaultLogger, PatcherLogManager.Instance);
        }

        public ICheckDiskSpace CreateCheckDiskSpaceCommandForDiff(int versionId, AppUpdaterContext context, CancellationToken cancellationToken)
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

            AppDiffSummary diffSummary = context.App.RemoteMetaData.GetDiffSummary(versionId, cancellationToken);
            return new CheckDiskSpaceCommand(diffSummary, context.App.LocalDirectory.Path, biggestFileSize);
        }

        public ICheckDiskSpace CreateCheckDiskSpaceCommandForContent(int versionId, AppUpdaterContext context, CancellationToken cancellationToken)
        {
            AppContentSummary contentSummary = context.App.RemoteMetaData.GetContentSummary(versionId, cancellationToken);
            return new CheckDiskSpaceCommand(contentSummary, context.App.LocalDirectory.Path);
        }

        public IGeolocateCommand CreateGeolocateCommand()
        {
            var geolocateCommand = new GeolocateCommand();
            return geolocateCommand;
        }
    }
}