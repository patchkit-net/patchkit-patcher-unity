namespace PatchKit.Unity.Patcher.Status
{
    internal class GeneralStatus : IStatus
    {
        public double Weight { get; private set; }

        public double Progress { get; set; }

        public GeneralStatus(double weight)
        {
            Weight = weight;
        }
    }
}