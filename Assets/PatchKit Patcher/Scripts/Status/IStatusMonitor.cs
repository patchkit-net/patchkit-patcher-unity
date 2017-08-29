namespace PatchKit.Unity.Patcher.Status
{
    public interface IStatusMonitor
    {
        event OverallStatusChangedHandler OverallStatusChanged;

        IGeneralStatusReporter CreateGeneralStatusReporter(double weight);

        IDownloadStatusReporter CreateDownloadStatusReporter(double weight);

        void Reset();
    }
}