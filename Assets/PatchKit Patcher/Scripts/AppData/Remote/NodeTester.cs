using System.Diagnostics;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.Cancellation;
using System;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class NodeTester
    {
        private static readonly TimeSpan MinTimeRemaining = TimeSpan.FromMinutes(2.0);
        private static readonly TimeSpan MinDownloadTime = TimeSpan.FromSeconds(15.0);

        private readonly ResourceUrl _resourceUrl;
        private GarbageNodeTester _garbageNodeTester;
        private Stopwatch _testingStopwatch;
        private DownloadSpeedCalculator _downloadSpeedCalculator;

        public NodeTester(ResourceUrl url)
        {
            _resourceUrl = url;
            _testingStopwatch = new Stopwatch();
            _testingStopwatch.Start();
        }

        public void Start(CancellationToken cancellationToken)
        {
            _garbageNodeTester = new GarbageNodeTester(_resourceUrl.Url);
            _garbageNodeTester.Start(cancellationToken);

            _testingStopwatch.Stop();
        }

        public bool CanStart(long dataSize, DownloadSpeedCalculator calculator)
        {
            return calculator.TimeRemaining(dataSize) > MinTimeRemaining
                && _garbageNodeTester == null
                && _testingStopwatch.IsRunning
                && _testingStopwatch.Elapsed > MinDownloadTime;
        }

        public bool IsDone
        {
            get
            {
                return _garbageNodeTester != null && _garbageNodeTester.IsDone;
            }
        }

        public double? BytesPerSecond
        {
            get
            {
                if (_garbageNodeTester == null)
                {
                    return null;
                }
                return _garbageNodeTester.BytesPerSecond;
            }
        }

    }
}