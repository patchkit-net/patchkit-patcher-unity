namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public interface IRemoteData
    {
        /// <summary>
        /// Returns the content package resource.
        /// </summary>
        /// <param name="versionId">The version id.</param>
        /// <param name="keySecret">The key secret.</param>
        RemoteResource GetContentPackageResource(int versionId, string keySecret);

        /// <summary>
        /// Returns the diff package resource.
        /// </summary>
        /// <param name="versionId">The version id.</param>
        /// <param name="keySecret">The key secret.</param>
        RemoteResource GetDiffPackageResource(int versionId, string keySecret);
    }
}