namespace PatchKit.Unity.Patcher.AppData.Remote
{
    internal interface IRemoteData
    {
        IRemoteMetaData MetaData { get; }

        RemoteResource GetContentPackageResource(int versionId, string keySecret);

        RemoteResource GetDiffPackageResource(int versionId, string keySecret);
    }
}