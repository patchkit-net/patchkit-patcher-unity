using System;

namespace PatchKit.Unity.Patcher.Progress
{
    public class DownloadProgress : IDownloadProgress
    {
        public double Value { get; private set; }

        public readonly double Weight;

        public double DownloadSpeed
        {
            get
            {
                return _downloadSpeedCalculator.DownloadSpeed;
            }
        }

        private readonly DownloadSpeedCalculator _downloadSpeedCalculator = new DownloadSpeedCalculator();

        private DateTime _lastProgress;

        public DownloadProgress(double weight)
        {
            Weight = weight;
            Value = 0.0;
            _lastProgress = DateTime.Now;
        }
            
        public void OnDownloadProgress(long bytes, long totalBytes)
        {
            Value = bytes / (double)totalBytes;

            long duration = (long)((DateTime.Now - _lastProgress).TotalMilliseconds);
            _downloadSpeedCalculator.AddSample(bytes, duration);
            _lastProgress = DateTime.Now;
        }
    }
}