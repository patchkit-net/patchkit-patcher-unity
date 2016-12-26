namespace PatchKit.Unity.Patcher.Progress
{
    public interface IProgressMonitor
    {
        double Progress { get; }

        double DownloadProgress { get; }

        double DownloadSpeed { get; }

        IProgress AddProgress(double weight);

        IDownloadProgress AddDownloadProgress(double weight);
    }
}