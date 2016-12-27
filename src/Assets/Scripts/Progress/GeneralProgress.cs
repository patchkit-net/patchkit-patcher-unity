namespace PatchKit.Unity.Patcher.Progress
{
    internal class GeneralProgress : IGeneralProgress
    {
        public GeneralProgress(double weight)
        {
            Weight = weight;
        }

        public double Weight { get; private set; }

        public double Value { get; set; }
    }
}