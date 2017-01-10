namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    internal enum FileIntegrityStatus
    {
        Ok,
        MissingData,
        MissingMetaData,
        InvalidVersion,
        InvalidHash
    }
}