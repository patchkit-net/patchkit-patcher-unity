using UniRx;

namespace PatchKit.Apps.Updating.AppUpdater.Status
{
    public class OperationStatus : IReadOnlyOperationStatus
    {
        public ReactiveProperty<double> Progress { get; }
        public ReactiveProperty<double> Weight { get; }
        public ReactiveProperty<bool> IsActive { get; }
        public ReactiveProperty<string> Description { get; }

        public OperationStatus()
        {
            Progress = new ReactiveProperty<double>();
            Weight = new ReactiveProperty<double>();
            IsActive = new ReactiveProperty<bool>();
            Description = new ReactiveProperty<string>();
        }

        IReadOnlyReactiveProperty<double> IReadOnlyOperationStatus.Progress => Progress;

        IReadOnlyReactiveProperty<double> IReadOnlyOperationStatus.Weight => Weight;

        IReadOnlyReactiveProperty<bool> IReadOnlyOperationStatus.IsActive => IsActive;

        IReadOnlyReactiveProperty<string> IReadOnlyOperationStatus.Description => Description;
    }
}