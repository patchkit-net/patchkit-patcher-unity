using System.Collections.Generic;

namespace PatchKit.Unity.Patcher.Progress
{
    public class ProgressMonitor : IProgressMonitor
    {
        private readonly List<Progress> _progressList = new List<Progress>();
        private readonly List<DownloadProgress> _downloadProgressList = new List<DownloadProgress>();

        public ProgressMonitor()
        {
        }

        public IProgress AddProgress(double weight)
        {
            var progress = new Progress(weight);
            _progressList.Add(progress);
            return progress;
        }

        public IDownloadProgress AddDownloadProgress(double weight)
        {
            var downloadProgress = new DownloadProgress(weight);
            _downloadProgressList.Add(downloadProgress);
            return downloadProgress;
        }
    }
}

