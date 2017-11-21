namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public enum FileIntegrityStatus
    {
        Ok,
        MissingData,
        MissingMetaData,
        InvalidVersion,
        InvalidHash,
        InvalidSize
    }
}