namespace PatchKit.Unity.Patcher.Progress
{
    public class Progress : IProgress
    {
        public double Value { get; private set; }

        public readonly double Weight;

        public Progress(double weight)
        {
            Weight = weight;
            Value = 0.0;
        }

        public void OnProgress(double value)
        {
            throw new System.NotImplementedException();
        }
    }
}