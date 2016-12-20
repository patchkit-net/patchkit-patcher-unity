using System;

namespace PatchKit.Unity.Patcher.Statistics
{
    internal class RegularProgressReporter : IProgressReporter
    {
        private double _progress;

        public event ProgressHandler OnProgress;

        public double Progress
        {
            get { return _progress; }
            set
            {
                if (0.0 > _progress || 1.0 < _progress)
                {
                    throw new ArgumentOutOfRangeException("value",
                        "Progress value out of range. Must be between 0 and 1 (both inclusive).");
                }
                _progress = value;
                InvokeOnProgress();
            }
        }

        public void Finish()
        {
            Progress = 1.0;
        }

        private void InvokeOnProgress()
        {
            if (OnProgress != null)
            {
                OnProgress(Progress);
            }
        }
    }
}