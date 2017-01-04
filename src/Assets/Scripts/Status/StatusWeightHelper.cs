using PatchKit.Api.Models;

namespace PatchKit.Unity.Patcher.Status
{
    internal static class StatusWeightHelper
    {
        public static double GetUnarchivePackageWeight(long size)
        {
            return BytesToWeight(size)*0.1;
        }

        public static double GetCheckVersionIntegrityWeight(AppContentSummary summary)
        {
            return BytesToWeight(summary.Size)*0.05;
        }

        public static double GetCopyFilesWeight(long size)
        {
            return BytesToWeight(size) *0.01;
        }

        public static double GetInstallDiffWeight(AppDiffSummary summary)
        {
            return BytesToWeight(summary.Size)*0.2;
        }

        public static double GetResourceDownloadWeight(Data.Remote.RemoteResource resource)
        {
            return BytesToWeight(resource.Size)*1;
        }

        private static double BytesToWeight(long bytes)
        {
            return bytes/1024.0/1024.0;
        }
    }
}