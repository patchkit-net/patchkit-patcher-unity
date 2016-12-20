namespace PatchKit.Unity.Patcher.Statistics
{
    internal interface IProgressReporter
    {
        double Progress { get; }

        event ProgressHandler OnProgress;
    }
}