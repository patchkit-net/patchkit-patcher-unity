using System;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.Status
{
    public class DownloadStatusReporter : IDownloadStatusReporter
    {
        private readonly DownloadStatusHolder _downloadStatusHolder;

        private readonly DownloadSpeedCalculator _downloadSpeedCalculator = new DownloadSpeedCalculator();

        private DateTime _lastProgress;

        public event Action<DownloadStatusHolder> StatusReported;

        public DownloadStatusReporter(DownloadStatusHolder downloadStatusHolder)
        {
            AssertChecks.ArgumentNotNull(downloadStatusHolder, "downloadStatusHolder");

            _downloadStatusHolder = downloadStatusHolder;
        }

        public void OnDownloadStarted()
        {
            _downloadStatusHolder.Bytes = 0;
            _downloadStatusHolder.TotalBytes = 0;
            _downloadStatusHolder.Speed = 0.0;
            _downloadStatusHolder.IsDownloading = true;

            _lastProgress = DateTime.Now;

            OnStatusReported();
        }

        public void OnDownloadProgressChanged(long bytes, long totalBytes)
        {
            _downloadStatusHolder.Bytes = bytes;
            _downloadStatusHolder.TotalBytes = totalBytes;
            _downloadStatusHolder.Speed = CalculateDownloadSpeed(bytes);

            OnStatusReported();
        }

        public void OnDownloadEnded()
        {
            _downloadStatusHolder.Bytes = _downloadStatusHolder.TotalBytes;
            _downloadStatusHolder.Speed = 0.0;
            _downloadStatusHolder.IsDownloading = false;

            OnStatusReported();
        }

        private double CalculateDownloadSpeed(long bytes)
        {
            long duration = (long) ((DateTime.Now - _lastProgress).TotalMilliseconds);
            _downloadSpeedCalculator.AddSample(bytes, duration);
            _lastProgress = DateTime.Now;

            return _downloadStatusHolder.Speed;
        }

        protected virtual void OnStatusReported()
        {
            if (StatusReported != null) StatusReported(_downloadStatusHolder);
        }
    }
}