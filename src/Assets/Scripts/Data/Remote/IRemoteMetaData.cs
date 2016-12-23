using PatchKit.Api.Models;

namespace PatchKit.Unity.Patcher.Data.Remote
{
    public interface IRemoteMetaData
    {
        int GetLatestVersionId();

        App GetAppInfo();

        AppContentSummary GetContentSummary(int versionId);

        AppDiffSummary GetDiffSummary(int versionId);

        string GetKeySecret(string key);
    }
}