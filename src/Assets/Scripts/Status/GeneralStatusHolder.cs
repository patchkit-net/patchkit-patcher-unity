namespace PatchKit.Unity.Patcher.Status
{
    internal class GeneralStatusHolder : IStatusHolder
    {
        public double Weight { get; private set; }

        public double Progress { get; set; }

        public GeneralStatusHolder(double weight)
        {
            Weight = weight;
        }
    }
}