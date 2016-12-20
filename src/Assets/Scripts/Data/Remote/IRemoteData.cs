namespace PatchKit.Unity.Patcher.Data.Remote
{
    public interface IRemoteData
    {
        IRemoteMetaData MetaData { get; }

        RemoteResource GetContentPackageResource(int versionId, string keySecret);

        RemoteResource GetDiffPackageResource(int versionId, string keySecret);
    }
}