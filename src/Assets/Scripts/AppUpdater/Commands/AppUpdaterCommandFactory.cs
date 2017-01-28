using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData.Local;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class AppUpdaterCommandFactory
    {
        public IInstallDiffCommand CreateInstallDiffCommand(int versionId,
            AppDiffSummary versionDiffSummary,
            ILocalData localData,
            ILocalMetaData localMetaData,
            ITemporaryData temporaryData)
        {
            return new InstallDiffCommand(versionId,
                versionDiffSummary,
                localData,
                localMetaData,
                temporaryData);
        }

        public IDownloadDiffPackageCommand CreateDownloadDiffPackageCommand(int versionId,
            AppUpdaterContext context)
        {
            return new DownloadDiffPackageCommand(versionId, context);
        }

        public IInstallContentCommand CreateInstallContentCommand(int versionId,
            AppContentSummary versionContentSummary,
            ILocalData localData,
            ILocalMetaData localMetaData,
            ITemporaryData temporaryData)
        {
            return new InstallContentCommand(versionId, versionContentSummary,
                localData, localMetaData, temporaryData);
        }

        public IDownloadContentPackageCommand CreateDownloadContentPackageCommand(int versionId,
            AppUpdaterContext context)
        {
            return new DownloadContentPackageCommand(versionId, context);
        }

        public ICheckVersionIntegrityCommand CreateCheckVersionIntegrityCommand(int versionId, AppUpdaterContext context)
        {
            return new CheckVersionIntegrityCommand(versionId, context);
        }

        public IUninstallCommand CreateUninstallCommand(ILocalData localData, ILocalMetaData localMetaData)
        {
            return new UninstallCommand(localData, localMetaData);
        }

        public IValidateLicenseCommand CreateValidateLicenseCommand(AppUpdaterContext context)
        {
            return new ValidateLicenseCommand(context.LicenseDialog, context.App.RemoteMetaData);
        }
    }
}