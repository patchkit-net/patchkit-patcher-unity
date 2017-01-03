using System;

namespace PatchKit.Unity.Patcher.Status
{
    internal class DownloadStatusReporter : IDownloadStatusReporter
    {
        private readonly DownloadStatus _downloadStatus;

        private readonly DownloadSpeedCalculator _downloadSpeedCalculator = new DownloadSpeedCalculator();

        private DateTime _lastProgress;

        public event Action<DownloadStatus> StatusReported;

        public DownloadStatusReporter(DownloadStatus downloadStatus)
        {
            _downloadStatus = downloadStatus;
        }

        public void OnDownloadStarted()
        {
            _downloadStatus.Bytes = 0;
            _downloadStatus.TotalBytes = 0;
            _downloadStatus.Speed = 0.0;
            _downloadStatus.IsDownloading = true;

            _lastProgress = DateTime.Now;

            OnStatusReported();
        }

        public void OnDownloadProgressChanged(long bytes, long totalBytes)
        {
            _downloadStatus.Bytes = bytes;
            _downloadStatus.TotalBytes = totalBytes;
            _downloadStatus.Speed = CalculateDownloadSpeed(bytes);

            OnStatusReported();
        }

        public void OnDownloadEnded()
        {
            _downloadStatus.Bytes = _downloadStatus.TotalBytes;
            _downloadStatus.Speed = 0.0;
            _downloadStatus.IsDownloading = false;

            OnStatusReported();
        }

        private double CalculateDownloadSpeed(long bytes)
        {
            long duration = (long) ((DateTime.Now - _lastProgress).TotalMilliseconds);
            _downloadSpeedCalculator.AddSample(bytes, duration);
            _lastProgress = DateTime.Now;

            return _downloadStatus.Speed;
        }

        protected virtual void OnStatusReported()
        {
            if (StatusReported != null) StatusReported(_downloadStatus);
        }
    }
}