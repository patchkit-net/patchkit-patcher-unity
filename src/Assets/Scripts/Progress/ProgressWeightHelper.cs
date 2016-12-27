namespace PatchKit.Unity.Patcher.Progress
{
    internal static class ProgressWeightHelper
    {
        public static double GetUnarchiveWeight(long size)
        {
            return BytesToWeight(size)*0.1;
        }

        public static double GetCheckVersionIntegrityWeight(long size)
        {
            return BytesToWeight(size)*0.05;
        }

        public static double GetProcessContentWeight(long size)
        {
            return BytesToWeight(size)*0.01;
        }

        public static double GetProcessDiffWeight(long size)
        {
            return BytesToWeight(size)*0.2;
        }

        public static double GetResourceDownloadWeight(long size)
        {
            return BytesToWeight(size)*1;
        }

        private static double BytesToWeight(long bytes)
        {
            return bytes/1024.0/1024.0;
        }
    }
}