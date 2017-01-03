using System.Collections.Generic;
using System.Linq;

namespace PatchKit.Unity.Patcher.Status
{
    internal class StatusMonitor : IStatusMonitor
    {
        private readonly List<IStatus> _statusList = new List<IStatus>();

        private OverallStatus _overallStatus;

        public event OverallStatusChangedHandler OverallStatusChanged;

        public IGeneralStatusReporter CreateGeneralStatusReporter(double weight)
        {
            var status = new GeneralStatus(weight);
            _statusList.Add(status);

            var reporter = new GeneralStatusReporter(status);
            reporter.StatusReported += ProcessGeneralStatus;

            return reporter;
        }

        public IDownloadStatusReporter CreateDownloadStatusReporter(double weight)
        {
            var status = new DownloadStatus(weight);
            _statusList.Add(status);

            var reporter = new DownloadStatusReporter(status);
            reporter.StatusReported += ProcessDownloadStatus;

            return reporter;
        }

        private void ProcessDownloadStatus(DownloadStatus downloadStatus)
        {
            _overallStatus.IsDownloading = downloadStatus.IsDownloading;
            _overallStatus.DownloadSpeed = downloadStatus.Speed;
            _overallStatus.DownloadBytes = downloadStatus.Bytes;
            _overallStatus.DownloadTotalBytes = downloadStatus.TotalBytes;

            _overallStatus.Progress = CalculateOverallProgress();

            OnStatusChanged();
        }

        private void ProcessGeneralStatus(GeneralStatus generalStatus)
        {
            _overallStatus.Progress = CalculateOverallProgress();

            OnStatusChanged();
        }

        private double CalculateOverallProgress()
        {
            if (_statusList.Count == 0)
            {
                return 0.0;
            }

            double weightsSum = _statusList.Sum(s => s.Weight);

            double progressSum = _statusList.Sum(s => s.Progress * s.Weight);

            return progressSum / weightsSum;
        }

        protected virtual void OnStatusChanged()
        {
            if (OverallStatusChanged != null) OverallStatusChanged(_overallStatus);
        }
    }
}