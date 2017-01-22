using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData.Remote;

namespace PatchKit.Unity.Patcher.Status
{
    public static class StatusWeightHelper
    {
        public static double GetUnarchivePackageWeight(long size)
        {
            return BytesToWeight(size)*0.1;
        }

        public static double GetUninstallWeight()
        {
            return 0.0001;
        }

        public static double GetCheckVersionIntegrityWeight(AppContentSummary summary)
        {
            return BytesToWeight(summary.Size)*0.05;
        }

        public static double GetCopyContentFilesWeight(AppContentSummary summary)
        {
            return BytesToWeight(summary.Size) *0.01;
        }

        public static double GetAddDiffFilesWeight(AppDiffSummary summary)
        {
            return BytesToWeight(summary.Size) * 0.01;
        }

        public static double GetModifyDiffFilesWeight(AppDiffSummary summary)
        {
            return BytesToWeight(summary.Size) * 0.2;
        }

        public static double GetRemoveDiffFilesWeight(AppDiffSummary summary)
        {
            return BytesToWeight(summary.Size) * 0.001;
        }

        public static double GetResourceDownloadWeight(RemoteResource resource)
        {
            return BytesToWeight(resource.Size)*1;
        }

        private static double BytesToWeight(long bytes)
        {
            return bytes/1024.0/1024.0;
        }
    }
}