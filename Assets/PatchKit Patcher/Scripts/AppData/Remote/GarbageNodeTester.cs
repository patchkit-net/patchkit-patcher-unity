using System;
using System.Diagnostics;
using System.Threading;
using PatchKit.Logging;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders;
using CancellationToken = PatchKit.Unity.Patcher.Cancellation.CancellationToken;
using CancellationTokenSource = PatchKit.Unity.Patcher.Cancellation.CancellationTokenSource;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public interface IGarbageNodeTester
    {
        void Start(CancellationToken cancellationToken);

        bool IsDone { get; }

        bool WasSuccess { get; }

        double BytesPerSecond { get; }
    }

    public class GarbageNodeTester : IGarbageNodeTester
    {
        private static readonly int Timeout = 10000;

        private readonly string _url;
        private readonly ulong _size;
        private readonly double _seed;

        private Thread _thread = null;

        private DownloadSpeedCalculator _calculator = new DownloadSpeedCalculator();

        private bool _isDone;

        private bool _wasSuccess;

        private static readonly ulong DefaultSize = 50;
        private static readonly double DefaultSeed = 0.123;

        private static readonly TimeSpan MaxTestDuration = TimeSpan.FromSeconds(15.0);

        private static string PruneUrl(string url, ulong size, double seed)
        {
            var uri = new Uri(url);
            return string.Format("{3}://{0}:8888/garbage.php?r={1}&ckSize={2}", uri.Host, seed, size, uri.Scheme);
        }

        public GarbageNodeTester(string url)
            : this(url, DefaultSize, DefaultSeed)
        {
        }

        public GarbageNodeTester(string url, ulong size, double seed)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("url cannot be null or empty", "url");
            }

            if (size == 0) 
            {
                throw new ArgumentException("size must be a positive value", "size");
            }

            _url = url;
            _size = size;
            _seed = seed;
        }

        public bool IsDone
        {
            get
            {
                return _isDone;
            }
        }

        public bool WasSuccess
        {
            get
            {
                return _wasSuccess;
            }
        }

        public double BytesPerSecond
        {
            get
            {
                if (!_isDone)
                {
                    PatcherLogManager.DefaultLogger.LogError("Reading BPS from an unfinished GarbageNodeTester.");
                }
                lock (_calculator)

                {
                    return _calculator.BytesPerSecond;
                }
            }
        }

        public void Start(CancellationToken cancellationToken)
        {
            _wasSuccess = false;
            _isDone = false;

            _thread = new Thread(() =>
            {
                CancellationTokenSource cancellationSource = new CancellationTokenSource();
                cancellationToken.Register(cancellationSource.Cancel);

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var downloader = new BaseHttpDownloader(PruneUrl(_url, _size, _seed), Timeout);

                long totalBytes = 0;

                foreach (var packet in downloader.ReadPackets(cancellationSource.Token))
                {
                    totalBytes += packet.Length;
                    lock (_calculator)
                    {
                        _calculator.AddSample(totalBytes, DateTime.Now);
                    }

                    if (stopwatch.Elapsed > MaxTestDuration)
                    {
                        break;
                    }
                }

                _isDone = true;
                _wasSuccess = true;
            });

            _thread.Start();
        }
    }
}