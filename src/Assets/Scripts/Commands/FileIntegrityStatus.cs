namespace PatchKit.Unity.Patcher.Commands
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