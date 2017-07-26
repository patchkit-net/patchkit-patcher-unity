namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public interface IRemoteData
    {
        /// <summary>
        /// Returns the content package resource.
        /// </summary>
        /// <param name="versionId">The version id.</param>
        /// <param name="keySecret">The key secret.</param>
        /// <param name="countryCode"></param>
        RemoteResource GetContentPackageResource(int versionId, string keySecret, string countryCode);

        /// <summary>
        /// Returns the diff package resource.
        /// </summary>
        /// <param name="versionId">The version id.</param>
        /// <param name="keySecret">The key secret.</param>
        RemoteResource GetDiffPackageResource(int versionId, string keySecret, string countryCode);

        /// <summary>
        /// Returns password of the content package resource.
        /// </summary>
        /// <param name="versionId">The version id.</param>
        string GetContentPackageResourcePassword(int versionId);

        /// <summary>
        /// Returns password of the diff package resource.
        /// </summary>
        /// <param name="versionId">The version id.</param>
        string GetDiffPackageResourcePassword(int versionId);
    }
}