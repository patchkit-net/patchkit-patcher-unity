namespace PatchKit.Unity.Patcher.Progress
{
    internal interface IProgressMonitor
    {
        double OverallProgress { get; }

        IProgress[] ProgressList { get; }

        IGeneralProgressReporter AddGeneralProgress(double weight);

        IDownloadProgressReporter AddDownloadProgress(double weight);
    }
}