using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

namespace PatchKit.Unity.Patcher.AppUpdater.Status
{
    public class UpdaterStatus : IReadOnlyUpdaterStatus
    {
        private readonly Dictionary<IReadOnlyOperationStatus, IDisposable> _registeredOperations
            = new Dictionary<IReadOnlyOperationStatus, IDisposable>();

        private readonly ReactiveProperty<double> _progress
            = new ReactiveProperty<double>();

        private readonly ReactiveProperty<IReadOnlyOperationStatus> _latestActiveOperation
            = new ReactiveProperty<IReadOnlyOperationStatus>();

        public void RegisterOperation(IReadOnlyOperationStatus operation)
        {
            var subscriptions = new CompositeDisposable();

            operation.IsActive.Subscribe(isActive =>
            {
                if (isActive)
                {
                    _latestActiveOperation.Value = operation;
                }
            }).AddTo(subscriptions);

            operation.Progress.Subscribe(_ =>
            {
                UpdateProgress();
            }).AddTo(subscriptions);

            operation.Weight.Subscribe(_ =>
            {
                UpdateProgress();
            }).AddTo(subscriptions);

            _registeredOperations.Add(operation, subscriptions);
        }

        public void UnregisterOperation(IReadOnlyOperationStatus operation)
        {
            if (_registeredOperations.ContainsKey(operation))
            {
                _registeredOperations[operation].Dispose();
                _registeredOperations.Remove(operation);
            }
        }

        private void UpdateProgress()
        {
            var progressSum = _registeredOperations.Keys.Sum(o => o.Progress.Value * o.Weight.Value);
            var weightSum = _registeredOperations.Keys.Sum(o => o.Weight.Value);

            _progress.Value = weightSum > 0.0 ? progressSum / weightSum : 0.0;
        }


        public IEnumerable<IReadOnlyOperationStatus> Operations
        {
            get { return _registeredOperations.Keys; }
        }

        public IReadOnlyReactiveProperty<IReadOnlyOperationStatus> LatestActiveOperation
        {
            get { return _latestActiveOperation; }
        }

        public IReadOnlyReactiveProperty<double> Progress
        {
            get { return _progress; }
        }
    }
}