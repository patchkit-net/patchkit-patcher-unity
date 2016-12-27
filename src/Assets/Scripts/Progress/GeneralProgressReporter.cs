namespace PatchKit.Unity.Patcher.Progress
{
    internal class GeneralProgressReporter : IGeneralProgressReporter
    {
        private readonly GeneralProgress _generalProgress;

        public GeneralProgressReporter(GeneralProgress generalProgress)
        {
            _generalProgress = generalProgress;
            _generalProgress.Value = 0.0;
        }

        public void OnProgressChanged(double value)
        {
            _generalProgress.Value = value;
        }
    }
}