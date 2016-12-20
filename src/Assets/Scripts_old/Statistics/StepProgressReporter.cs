using System;

namespace PatchKit.Unity.Patcher.Statistics
{
    internal class StepProgressReporter : IProgressReporter
    {
        private long _totalSteps;

        private long _currentStep;

        public event ProgressHandler OnProgress;

        public long TotalSteps
        {
            get { return _totalSteps; }
            set
            {
                if (_totalSteps < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Total steps must be bigger or equal to 0.");
                }
                _totalSteps = value;
                InvokeOnProgress();
            }
        }

        public long CurrentStep
        {
            get { return _currentStep; }
            set
            {
                if (_currentStep < 0 || _currentStep > TotalSteps)
                {
                    throw new ArgumentOutOfRangeException("value",
                        "Current step must be between 0 and TotalSteps value (both inclusive).");
                }
                _currentStep = value;
                InvokeOnProgress();
            }
        }

        public void Step()
        {
            _currentStep++;
        }

        public void Finish()
        {
            CurrentStep = TotalSteps;
        }

        public double Progress
        {
            get
            {
                if (TotalSteps == 0)
                {
                    return 0.0;
                }
                return CurrentStep/(double) TotalSteps;
            }
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