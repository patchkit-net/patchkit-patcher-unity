namespace PatchKit.Unity.Patcher.Progress
{
    internal interface IProgress
    {
        double Weight { get; }

        double Value { get; }
    }
}