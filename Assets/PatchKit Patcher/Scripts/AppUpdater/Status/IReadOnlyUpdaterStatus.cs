using System.Collections.Generic;
using UniRx;

namespace PatchKit.Unity.Patcher.AppUpdater.Status
{
    public interface IReadOnlyUpdaterStatus
    {
        IEnumerable<IReadOnlyOperationStatus> Operations { get; }

        IReadOnlyReactiveProperty<IReadOnlyOperationStatus> LatestActiveOperation { get; }

        IReadOnlyReactiveProperty<double> Progress { get; }
    }
}