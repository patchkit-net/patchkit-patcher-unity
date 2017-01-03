namespace PatchKit.Unity.Patcher.Status
{
    internal interface IStatus
    {
        double Progress { get; }

        double Weight { get; }
    }
}