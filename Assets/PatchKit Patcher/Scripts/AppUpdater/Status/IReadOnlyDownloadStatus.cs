using UniRx;

namespace PatchKit.Apps.Updating.AppUpdater.Status
{
    public interface IReadOnlyDownloadStatus : IReadOnlyOperationStatus
    {
        IReadOnlyReactiveProperty<long> Bytes { get; }
        IReadOnlyReactiveProperty<long> TotalBytes { get; }
        IReadOnlyReactiveProperty<double> BytesPerSecond { get; }
    }
}