namespace PatchKit.Unity.Patcher.Status
{
    public interface IGeneralStatusReporter
    {
        void OnProgressChanged(double progress, string description);
    }
}