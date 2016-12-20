using PatchKit.Api.Models;

namespace PatchKit.Unity.Patcher.Data.Remote
{
    public interface IRemoteMetaData
    {
        int GetLatestVersionId();

        AppContentSummary GetContentSummary(int versionId);

        AppDiffSummary GetDiffSummary(int versionId);

        string GetKeySecret(string key);
    }
}