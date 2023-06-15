using System;
using System.Linq;
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
        public ReactiveProperty<bool> IsIdle { get; private set; }

        public IReadOnlyReactiveProperty<double> Progress { get; private set; }

        public IReadOnlyReactiveProperty<double> BytesPerSecond { get; private set; }

        private readonly DownloadSpeedCalculator _downloadSpeedCalculator =
            new DownloadSpeedCalculator();

        private struct ByteSample
        {
            public long? Bytes;
            public DateTime Timestamp;
        }

        public DownloadStatus()
        {
            Bytes = new ReactiveProperty<long>();
            TotalBytes = new ReactiveProperty<long>();
            Progress = Bytes.CombineLatest(TotalBytes, (b, t) => t > 0 ? (double) b / (double) t : 0.0)
                .ToReadOnlyReactiveProperty();
            Weight = new ReactiveProperty<double>();
            IsActive = new ReactiveProperty<bool>();
            Description = new ReactiveProperty<string>();
            IsIdle = new ReactiveProperty<bool>();

            IsActive.Subscribe(_ =>
            {
                _downloadSpeedCalculator.Restart(DateTime.Now);
            });

            var timedBytes = Bytes.Select(b => new ByteSample{
                Bytes = b,
                Timestamp = DateTime.Now
            });

            var interval = Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Select(_ => new ByteSample{
                    Bytes = Bytes.Value,
                    Timestamp = DateTime.Now
                });

            var updateStream = timedBytes.Merge(interval);

            BytesPerSecond = updateStream
                .Select(b => _downloadSpeedCalculator.Calculate(b.Bytes, b.Timestamp))
                .ToReadOnlyReactiveProperty();
        }

        IReadOnlyReactiveProperty<long> IReadOnlyDownloadStatus.Bytes
        {
            get { return Bytes; }
        }

        IReadOnlyReactiveProperty<long> IReadOnlyDownloadStatus.TotalBytes
        {
            get { return TotalBytes; }
        }

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

        IReadOnlyReactiveProperty<bool> IReadOnlyOperationStatus.IsIdle
        {
            get { return IsIdle; }
        }
    }
}