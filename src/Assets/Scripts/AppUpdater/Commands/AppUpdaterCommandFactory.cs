namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class AppUpdaterCommandFactory
    {
        public IInstallDiffCommand CreateInstallDiffCommand(int versionId,
            AppUpdaterContext context)
        {
            return new InstallDiffCommand(versionId, context);
        }

        public IDownloadDiffPackageCommand CreateDownloadDiffPackageCommand(int versionId,
            AppUpdaterContext context)
        {
            return new DownloadDiffPackageCommand(versionId, context);
        }

        public IInstallContentCommand CreateInstallContentCommand(int versionId,
            App app)
        {
            return new InstallContentCommand(versionId, app);
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

        public IUninstallCommand CreateUninstallCommand(AppUpdaterContext context)
        {
            return new UninstallCommand(context);
        }

        public IValidateLicenseCommand CreateValidateLicenseCommand(AppUpdaterContext context)
        {
            return new ValidateLicenseCommand(context);
        }
    }
}