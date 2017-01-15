namespace PatchKit.Unity.Patcher.Status
{
    public interface IStatusHolder
    {
        double Progress { get; }

        double Weight { get; }
    }
}