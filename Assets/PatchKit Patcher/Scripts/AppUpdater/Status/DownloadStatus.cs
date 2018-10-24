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

        public IReadOnlyReactiveProperty<double> Progress { get; private set; }

        public IReadOnlyReactiveProperty<double> BytesPerSecond { get; private set; }

        private readonly IObservable<double> _bytesDeltas;

        public DownloadStatus()
        {
            Bytes = new ReactiveProperty<long>();
            TotalBytes = new ReactiveProperty<long>();
            Progress = Bytes.CombineLatest(TotalBytes, (b, t) => t > 0 ? (double) b / (double) t : 0.0)
                .ToReadOnlyReactiveProperty();
            Weight = new ReactiveProperty<double>();
            IsActive = new ReactiveProperty<bool>();
            Description = new ReactiveProperty<string>();

            _bytesDeltas = Bytes
                .Zip(Bytes.Skip(1), (lhs, rhs) => rhs - lhs)
                .Select(x => (double) x);

            BytesPerSecond = _bytesDeltas
                .Buffer(TimeSpan.FromSeconds(1))
                .Select(byteCounts => byteCounts.Count > 0 ? byteCounts.Sum() : 0)
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
    }
}