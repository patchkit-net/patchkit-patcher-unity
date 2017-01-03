namespace PatchKit.Unity.Patcher.Status
{
    internal interface IStatusMonitor
    {
        event OverallStatusChangedHandler OverallStatusChanged;

        IGeneralStatusReporter CreateGeneralStatusReporter(double weight);

        IDownloadStatusReporter CreateDownloadStatusReporter(double weight);
    }
}