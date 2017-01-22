using PatchKit.Api.Models.Main;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public interface IRemoteMetaData
    {
        int GetLatestVersionId();

        Api.Models.Main.App GetAppInfo();

        AppContentSummary GetContentSummary(int versionId);

        AppDiffSummary GetDiffSummary(int versionId);

        string GetKeySecret(string key);
    }
}