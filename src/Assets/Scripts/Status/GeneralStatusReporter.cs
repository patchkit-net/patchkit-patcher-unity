using System;

namespace PatchKit.Unity.Patcher.Status
{
    internal class GeneralStatusReporter : IGeneralStatusReporter
    {
        private readonly GeneralStatus _generalStatus;

        public event Action<GeneralStatus> StatusReported;

        public GeneralStatusReporter(GeneralStatus generalStatus)
        {
            _generalStatus = generalStatus;
            _generalStatus.Progress = 0.0;
        }

        public void OnProgressChanged(double progress)
        {
            _generalStatus.Progress = progress;
            OnStatusReported();
        }

        protected virtual void OnStatusReported()
        {
            if (StatusReported != null) StatusReported(_generalStatus);
        }
    }
}