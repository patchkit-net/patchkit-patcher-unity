namespace PatchKit.Unity.Patcher.Status
{
    public class GeneralStatusHolder : IStatusHolder
    {
        public double Weight { get; private set; }

        public double Progress { get; set; }
        
        public string Description { get; set; }

        public GeneralStatusHolder(double weight)
        {
            Weight = weight;
        }
    }
}