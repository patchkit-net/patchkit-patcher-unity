using System;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.Status
{
    public class GeneralStatusReporter : IGeneralStatusReporter
    {
        private readonly GeneralStatusHolder _generalStatusHolder;

        public event Action<GeneralStatusHolder> StatusReported;

        public GeneralStatusReporter(GeneralStatusHolder generalStatusHolder)
        {
            Checks.ArgumentNotNull(generalStatusHolder, "generalStatusHolder");

            _generalStatusHolder = generalStatusHolder;
            _generalStatusHolder.Progress = 0.0;
        }

        public void OnProgressChanged(double progress, string description)
        {
            _generalStatusHolder.Progress = progress;
            _generalStatusHolder.Description = description;
            OnStatusReported();
        }

        protected virtual void OnStatusReported()
        {
            if (StatusReported != null) StatusReported(_generalStatusHolder);
        }
    }
}