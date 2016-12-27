using System;

namespace PatchKit.Unity.Patcher.Progress
{
    internal class DownloadProgressReporter : IDownloadProgressReporter
    {
        private readonly DownloadProgress _downloadProgress;

        private readonly DownloadSpeedCalculator _downloadSpeedCalculator = new DownloadSpeedCalculator();

        private DateTime _lastProgress;

        public DownloadProgressReporter(DownloadProgress downloadProgress)
        {
            _downloadProgress = downloadProgress;
            _downloadProgress.Value = 0.0;
            _downloadProgress.Bytes = 0;
            _downloadProgress.TotalBytes = 0;
            _downloadProgress.DownloadSpeed = 0.0;

            _lastProgress = DateTime.Now;
        }

        public void OnDownloadProgressChanged(long bytes, long totalBytes)
        {
            _downloadProgress.Value = bytes / (double)totalBytes;
            _downloadProgress.Bytes = bytes;
            _downloadProgress.TotalBytes = totalBytes;
            _downloadProgress.DownloadSpeed = CalculateDownloadSpeed(bytes);
        }

        private double CalculateDownloadSpeed(long bytes)
        {
            long duration = (long)((DateTime.Now - _lastProgress).TotalMilliseconds);
            _downloadSpeedCalculator.AddSample(bytes, duration);
            _lastProgress = DateTime.Now;

            return _downloadProgress.DownloadSpeed;
        }
    }
}