namespace PatchKit.Unity.Patcher.Commands
{
    internal class CommandFactory
    {
        public IInstallDiffCommand CreateInstallDiffCommand(string packagePath, int versionId,
            PatcherContext context)
        {
            return new InstallDiffCommand(packagePath, versionId, context);
        }

        public IDownloadDiffPackageCommand CreateDownloadDiffPackageCommand(int versionId, string keySecret,
            PatcherContext context)
        {
            return new DownloadDiffPackageCommand(versionId, keySecret, context);
        }

        public IInstallContentCommand CreateInstallContentCommand(string packagePath, int versionId,
            PatcherContext context)
        {
            return new InstallContentCommand(packagePath, versionId, context);
        }

        public IDownloadContentPackageCommand CreateDownloadContentPackageCommand(int versionId, string keySecret,
            PatcherContext context)
        {
            return new DownloadContentPackageCommand(versionId, keySecret, context);
        }

        public ICheckVersionIntegrityCommand CreateCheckVersionIntegrityCommand(int versionId, PatcherContext context)
        {
            return new CheckVersionIntegrityCommand(versionId, context);
        }

        public IUninstallCommand CreateUninstallCommand(PatcherContext context)
        {
            return new UninstallCommand(context);
        }

        public IValidateLicenseCommand CreateValidateLicenseCommand(PatcherContext context)
        {
            return new ValidateLicenseCommand(context);
        }
    }
}