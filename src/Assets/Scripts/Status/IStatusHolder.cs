namespace PatchKit.Unity.Patcher.Status
{
    internal interface IStatusHolder
    {
        double Progress { get; }

        double Weight { get; }
    }
}