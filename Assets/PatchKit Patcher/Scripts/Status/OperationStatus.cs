using UniRx;

namespace PatchKit.Unity.Patcher.AppUpdater.Status
{
    public class OperationStatus : IReadOnlyOperationStatus
    {
        public ReactiveProperty<double> Progress { get; private set; }
        public ReactiveProperty<double> Weight { get; private set; }
        public ReactiveProperty<bool> IsActive { get; private set; }
        public ReactiveProperty<string> Description { get; private set; }

        public OperationStatus()
        {
            Progress = new ReactiveProperty<double>();
            Weight = new ReactiveProperty<double>();
            IsActive = new ReactiveProperty<bool>();
            Description = new ReactiveProperty<string>();
        }

        IReadOnlyReactiveProperty<double> IReadOnlyOperationStatus.Progress
        {
            get { return Progress; }
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