namespace PatchKit.Unity.Patcher.Progress
{
    internal interface IProgressMonitor
    {
        double OverallProgress { get; }

        IProgress[] ProgressList { get; }

        IGeneralProgressReporter CreateGeneralProgressReporter(double weight);

        IDownloadProgressReporter CreateDownloadProgressReporter(double weight);
    }
}