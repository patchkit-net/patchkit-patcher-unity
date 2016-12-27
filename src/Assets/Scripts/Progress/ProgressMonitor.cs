using System.Collections.Generic;
using System.Linq;
using PatchKit.Api.Models;

namespace PatchKit.Unity.Patcher.Progress
{
    internal class ProgressMonitor : IProgressMonitor
    {
        private readonly List<IProgress> _progressList = new List<IProgress>();

        public double OverallProgress
        {
            get
            {
                if (_progressList.Count == 0)
                {
                    return 0.0;
                }

                double weightsSum = _progressList.Sum(p => p.Weight);

                double weightedValuesSum = _progressList.Sum(p => p.Value*p.Weight);

                return weightedValuesSum/weightsSum;
            }
        }

        public IProgress[] ProgressList
        {
            get { return _progressList.ToArray(); }
        }

        public IGeneralProgressReporter AddGeneralProgress(double weight)
        {
            var progress = new GeneralProgress(weight);
            _progressList.Add(progress);

            return new GeneralProgressReporter(progress);
        }

        public IDownloadProgressReporter AddDownloadProgress(double weight)
        {
            var progress = new DownloadProgress(weight);
            _progressList.Add(progress);

            return new DownloadProgressReporter(progress);
        }
    }
}