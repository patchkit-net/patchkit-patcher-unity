using UniRx;

namespace PatchKit.Unity.Patcher.AppUpdater.Status
{
    public interface IReadOnlyDownloadStatus : IReadOnlyOperationStatus
    {
        IReadOnlyReactiveProperty<long> Bytes { get; }
        IReadOnlyReactiveProperty<long> TotalBytes { get; }
        IReadOnlyReactiveProperty<double> BytesPerSecond { get; }
    }
}