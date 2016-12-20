using PatchKit.Unity.Patcher.Zip;

namespace PatchKit.Unity.Patcher.Statistics
{
    internal class CustomProgressReporter<T> : IProgressReporter where T : ICustomProgress
    {
        private T _progress;

        private event ProgressHandler BaseOnProgress;

        public CustomProgressReporter()
        {
            OnProgress += progress => InvokeBaseOnProgress(progress.Progress);
        }

        public event CustomProgressHandler<T> OnProgress;

        public T Progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                InvokeOnProgress();
            }
        }

        double IProgressReporter.Progress
        {
            get { return Progress.Progress; }
        }

        event ProgressHandler IProgressReporter.OnProgress
        {
            add { BaseOnProgress += value; }
            remove { BaseOnProgress -= value; }
        }

        private void InvokeOnProgress()
        {
            if (OnProgress != null)
            {
                OnProgress(Progress);
            }
        }

        private void InvokeBaseOnProgress(double progress)
        {
            if (BaseOnProgress != null)
            {
                BaseOnProgress(progress);
            }
        }
    }
}