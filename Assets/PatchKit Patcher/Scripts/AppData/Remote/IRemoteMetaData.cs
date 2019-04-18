using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public interface IRemoteMetaData
    {
        /// <summary>
        /// Returns latest version id.
        /// </summary>
        int GetLatestVersionId(bool retryRequests, CancellationToken cancellationToken);

        /// <summary>
        /// Returns app info.
        /// </summary>
        Api.Models.Main.App GetAppInfo(bool retryRequests, CancellationToken cancellationToken);

        /// <summary>
        /// Returns certain version content summary.
        /// </summary>
        /// <param name="versionId">The version identifier.</param>
        AppContentSummary GetContentSummary(int versionId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns certain version diff summary.
        /// </summary>
        /// <param name="versionId">The version identifier.</param>
        AppDiffSummary GetDiffSummary(int versionId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the AppVersion model for the specified version id.
        /// </summary>
        /// <param name="versionId">The version identifier.</param>
        AppVersion GetAppVersionInfo(int versionId, bool retryRequests, CancellationToken cancellationToken);

        /// <summary>
        /// Returns key secret for certain key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="cachedKeySecret">Previously cached key secret.</param>
        string GetKeySecret(string key, string cachedKeySecret, CancellationToken cancellationToken);
    }
}