﻿using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

namespace PatchKit.Apps.Updating.AppUpdater.Status
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

            subscriptions.Add(operation.IsActive.Subscribe(isActive =>
            {
                if (isActive)
                {
                    _latestActiveOperation.Value = operation;
                }
            }));

            subscriptions.Add(operation.Progress.Subscribe(_ => { UpdateProgress(); }));

            subscriptions.Add(operation.Weight.Subscribe(_ => { UpdateProgress(); }));

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


        public IEnumerable<IReadOnlyOperationStatus> Operations => _registeredOperations.Keys;

        public IReadOnlyReactiveProperty<IReadOnlyOperationStatus> LatestActiveOperation => _latestActiveOperation;

        public IReadOnlyReactiveProperty<double> Progress => _progress;
    }
}