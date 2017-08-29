using System.Collections.Generic;
using System.Linq;

namespace PatchKit.Unity.Patcher.Status
{
    public class StatusMonitor : IStatusMonitor
    {
        private readonly List<IStatusHolder> _statusHolders = new List<IStatusHolder>();

        private OverallStatus _overallStatus;

        public event OverallStatusChangedHandler OverallStatusChanged;

        public IGeneralStatusReporter CreateGeneralStatusReporter(double weight)
        {
            var status = new GeneralStatusHolder(weight);
            _statusHolders.Add(status);

            var reporter = new GeneralStatusReporter(status);
            reporter.StatusReported += ProcessGeneralStatus;

            return reporter;
        }

        public IDownloadStatusReporter CreateDownloadStatusReporter(double weight)
        {
            var status = new DownloadStatusHolder(weight);
            _statusHolders.Add(status);

            var reporter = new DownloadStatusReporter(status);
            reporter.StatusReported += ProcessDownloadStatus;

            return reporter;
        }

        public void Reset()
        {
            _statusHolders.Clear();
        }

        private void ProcessDownloadStatus(DownloadStatusHolder downloadStatusHolder)
        {
            _overallStatus.IsDownloading = downloadStatusHolder.IsDownloading;
            _overallStatus.DownloadBytesPerSecond = downloadStatusHolder.BytesPerSecond;
            _overallStatus.DownloadBytes = downloadStatusHolder.Bytes;
            _overallStatus.DownloadTotalBytes = downloadStatusHolder.TotalBytes;

            _overallStatus.Progress = CalculateOverallProgress();

            OnStatusChanged();
        }

        private void ProcessGeneralStatus(GeneralStatusHolder generalStatusHolder)
        {
            _overallStatus.Description = generalStatusHolder.Description;
            
            _overallStatus.Progress = CalculateOverallProgress();

            OnStatusChanged();
        }

        private double CalculateOverallProgress()
        {
            if (_statusHolders.Count == 0)
            {
                return 0.0;
            }

            double weightsSum = _statusHolders.Sum(s => s.Weight);

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (weightsSum == 0.0)
            {
                return 0.0;
            }

            double progressSum = _statusHolders.Sum(s => s.Progress * s.Weight);

            return progressSum / weightsSum;
        }

        protected virtual void OnStatusChanged()
        {
            if (OverallStatusChanged != null) OverallStatusChanged(_overallStatus);
        }
    }
}