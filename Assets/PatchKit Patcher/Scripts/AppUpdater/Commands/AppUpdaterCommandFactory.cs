using System;
using System.IO;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppData.FileSystem;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class AppUpdaterCommandFactory
    {
        public IDownloadPackageCommand CreateDownloadContentPackageCommand(int versionId, string keySecret,
            string countryCode, AppUpdaterContext context, CancellationToken cancellationToken)
        {
            RemoteResource resource = context.App.RemoteData.GetContentPackageResource(versionId, keySecret, countryCode, cancellationToken);

            IDownloadDirectory appDownloadDirectory = context.App.DownloadDirectory;
            string destinationFilePath = appDownloadDirectory.GetContentPackagePath(versionId);
            string destinationMetaPath = appDownloadDirectory.GetContentPackageMetaPath(versionId);

            appDownloadDirectory.PrepareForWriting();

            return new DownloadPackageCommand(resource, destinationFilePath, destinationMetaPath);
        }

        public IDownloadPackageCommand CreateDownloadDiffPackageCommand(int versionId, string keySecret,
            string countryCode, AppUpdaterContext context, CancellationToken cancellationToken)
        {
            RemoteResource resource = context.App.RemoteData.GetDiffPackageResource(versionId, keySecret, countryCode, cancellationToken);

            IDownloadDirectory appDownloadDirectory = context.App.DownloadDirectory;
            string destinationFilePath = appDownloadDirectory.GetDiffPackagePath(versionId);
            string destinationMetaPath = appDownloadDirectory.GetDiffPackageMetaPath(versionId);

            appDownloadDirectory.PrepareForWriting();

            return new DownloadPackageCommand(resource, destinationFilePath, destinationMetaPath);
        }

        public IRepairFilesCommand CreateRepairFilesCommand(int versionId, AppUpdaterContext context, RemoteResource resource,
            Pack1Meta.FileEntry[] brokenFiles, Pack1Meta meta, CancellationToken cancellationToken)
        {
            string packagePath = context.App.DownloadDirectory.GetContentPackagePath(versionId);
            string packagePassword = context.App.RemoteData.GetContentPackageResourcePassword(versionId);
            AppContentSummary versionContentSummary = context.App.RemoteMetaData.GetContentSummary(versionId, cancellationToken);

            return new RepairFilesCommand(resource, meta, brokenFiles, packagePath, packagePassword, context.App.LocalDirectory, versionContentSummary);
        }

        public IInstallContentCommand CreateInstallContentCommand(int versionId, AppUpdaterContext context, CancellationToken cancellationToken)
        {
            string packagePath = context.App.DownloadDirectory.GetContentPackagePath(versionId);
            string packageMetaPath = context.App.DownloadDirectory.GetContentPackageMetaPath(versionId);
            AppContentSummary versionContentSummary = context.App.RemoteMetaData.GetContentSummary(versionId, cancellationToken);
            string packagePassword = context.App.RemoteData.GetContentPackageResourcePassword(versionId);

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
            string packagePath = context.App.DownloadDirectory.GetDiffPackagePath(versionId);
            string packageMetaPath = context.App.DownloadDirectory.GetDiffPackageMetaPath(versionId);
            string packagePassword = context.App.RemoteData.GetDiffPackageResourcePassword(versionId);

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
            AppContentSummary versionContentSummary = context.App.RemoteMetaData.GetContentSummary(versionId, cancellationToken);

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
            long biggestFileSize = 0, filesToProcessSize = 0;
            string[] registeredEntries = context.App.LocalMetaData.GetRegisteredEntries();
            foreach (string entry in registeredEntries)
            {
                string filePath = context.App.LocalDirectory.Path.PathCombine(entry);
                var fileInfo = new FileInfo(Paths.Fix(filePath));
                if (fileInfo.Exists && fileInfo.Length > biggestFileSize)
                {
                    biggestFileSize = fileInfo.Length;
                }
            }

            AppDiffSummary diffSummary = context.App.RemoteMetaData.GetDiffSummary(versionId, cancellationToken);
            
            IDownloadDirectory appDownloadDirectory = context.App.DownloadDirectory;
            string destinationFilePath = appDownloadDirectory.GetDiffPackagePath(versionId);
            filesToProcessSize += FileOperations.GetSizeFile(destinationFilePath);
            
            string destinationMetaPath = appDownloadDirectory.GetDiffPackageMetaPath(versionId);
            filesToProcessSize += FileOperations.GetSizeFile(destinationMetaPath);
            
            return new CheckDiskSpaceCommand(diffSummary, context.App.LocalDirectory.Path, biggestFileSize, filesToProcessSize);
        }

        public ICheckDiskSpace CreateCheckDiskSpaceCommandForContent(int versionId, AppUpdaterContext context, CancellationToken cancellationToken)
        {
            AppContentSummary contentSummary = context.App.RemoteMetaData.GetContentSummary(versionId, cancellationToken);

            long filesToProcessSize = 0;
            IDownloadDirectory appDownloadDirectory = context.App.DownloadDirectory;
            string destinationFilePath = appDownloadDirectory.GetContentPackagePath(versionId);
            filesToProcessSize += FileOperations.GetSizeFile(destinationFilePath);
            
            string destinationMetaPath = appDownloadDirectory.GetContentPackageMetaPath(versionId);
            filesToProcessSize += FileOperations.GetSizeFile(destinationMetaPath);
            
            return new CheckDiskSpaceCommand(contentSummary, context.App.LocalDirectory.Path, filesToProcessSize);
        }

        public IGeolocateCommand CreateGeolocateCommand()
        {
            var geolocateCommand = new GeolocateCommand();
            return geolocateCommand;
        }
        
        public ICheckPathLengthCommand CreateCheckPathLengthCommand(int versionId, AppUpdaterContext context, CancellationToken cancellationToken)
        {
            AppContentSummary contentSummary = context.App.RemoteMetaData.GetContentSummary(versionId, cancellationToken);
            return new CheckPathLengthCommand(contentSummary, context.App.LocalDirectory.Path);
        }
    }
}