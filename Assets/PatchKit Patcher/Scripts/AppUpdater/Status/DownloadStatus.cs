using System;
using PatchKit.Unity.Patcher.Status;
using UniRx;

namespace PatchKit.Unity.Patcher.AppUpdater.Status
{
    public class DownloadStatus : IReadOnlyDownloadStatus
    {
        public ReactiveProperty<long> Bytes { get; private set; }
        public ReactiveProperty<long> TotalBytes { get; private set; }
        public ReactiveProperty<double> Weight { get; private set; }
        public ReactiveProperty<bool> IsActive { get; private set; }
        public ReactiveProperty<string> Description { get; private set; }

        private readonly DownloadSpeedCalculator _downloadSpeedCalculator =
            new DownloadSpeedCalculator();

        private readonly ReactiveProperty<double> _bytesPerSecond =
            new ReactiveProperty<double>();

        public DownloadStatus()
        {
            Bytes = new ReactiveProperty<long>();
            TotalBytes = new ReactiveProperty<long>();
            Progress = Bytes.CombineLatest(TotalBytes, (b, t) => t > 0 ? (double) b / (double) t : 0.0)
                .ToReadOnlyReactiveProperty();
            Weight = new ReactiveProperty<double>();
            IsActive = new ReactiveProperty<bool>();
            Description = new ReactiveProperty<string>();

            IsActive.Subscribe(_ =>
            {
                _downloadSpeedCalculator.Restart(DateTime.Now);
            });

            Bytes.Subscribe(b =>
            {
                _downloadSpeedCalculator.AddSample(b, DateTime.Now);
                _bytesPerSecond.Value = _downloadSpeedCalculator.BytesPerSecond;
            });
        }

        IReadOnlyReactiveProperty<long> IReadOnlyDownloadStatus.Bytes
        {
            get { return Bytes; }
        }

        IReadOnlyReactiveProperty<long> IReadOnlyDownloadStatus.TotalBytes
        {
            get { return TotalBytes; }
        }

        public IReadOnlyReactiveProperty<double> BytesPerSecond
        {
            get { return _bytesPerSecond; }
        }

        public IReadOnlyReactiveProperty<double> Progress { get; private set; }

        IReadOnlyReactiveProperty<double> IReadOnlyOperationStatus.Weight
        {
            get { return Weight; }
        }

        IReadOnlyReactiveProperty<bool> IReadOnlyOperationStatus.IsActive
        {
            get { return IsActive; }
        }

        IReadOnlyReactiveProperty<string> IReadOnlyOperationStatus.Description
        {
            get { return Description; }
        }
    }
}