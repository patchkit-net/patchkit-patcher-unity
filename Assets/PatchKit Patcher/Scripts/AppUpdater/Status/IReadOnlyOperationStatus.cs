﻿using UniRx;

namespace PatchKit.Apps.Updating.AppUpdater.Status
{
    public interface IReadOnlyOperationStatus
    {
        IReadOnlyReactiveProperty<double> Progress { get; }

        IReadOnlyReactiveProperty<double> Weight { get; }

        IReadOnlyReactiveProperty<bool> IsActive { get; }

        IReadOnlyReactiveProperty<string> Description { get; }
    }
}