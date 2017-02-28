using System;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.Status
{
    public class DownloadStatusReporter : IDownloadStatusReporter
    {
        private readonly DownloadStatusHolder _downloadStatusHolder;

        private readonly DownloadSpeedCalculator _downloadSpeedCalculator = new DownloadSpeedCalculator();

        public event Action<DownloadStatusHolder> StatusReported;

        public DownloadStatusReporter(DownloadStatusHolder downloadStatusHolder)
        {
            Checks.ArgumentNotNull(downloadStatusHolder, "downloadStatusHolder");

            _downloadStatusHolder = downloadStatusHolder;
        }

        public void OnDownloadStarted()
        {
            _downloadSpeedCalculator.Restart(DateTime.Now);

            _downloadStatusHolder.Bytes = 0;
            _downloadStatusHolder.TotalBytes = 0;
            _downloadStatusHolder.BytesPerSecond = 0.0;
            _downloadStatusHolder.IsDownloading = true;

            OnStatusReported();
        }

        public void OnDownloadProgressChanged(long bytes, long totalBytes)
        {
            _downloadSpeedCalculator.AddSample(bytes, DateTime.Now);

            _downloadStatusHolder.Bytes = bytes;
            _downloadStatusHolder.TotalBytes = totalBytes;
            _downloadStatusHolder.BytesPerSecond = _downloadSpeedCalculator.BytesPerSecond;

            OnStatusReported();
        }

        public void OnDownloadEnded()
        {
            _downloadStatusHolder.Bytes = _downloadStatusHolder.TotalBytes;
            _downloadStatusHolder.BytesPerSecond = 0.0;
            _downloadStatusHolder.IsDownloading = false;

            OnStatusReported();
        }

        protected virtual void OnStatusReported()
        {
            if (StatusReported != null) StatusReported(_downloadStatusHolder);
        }
    }
}