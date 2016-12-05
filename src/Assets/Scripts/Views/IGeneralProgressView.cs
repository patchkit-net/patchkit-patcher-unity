namespace PatchKit.Unity.Patcher.Views
{
    public interface IGeneralProgressView : IView
    {
        void AddOperation(double weight);
        void UpdateCurrentOperationProgress(double progress);
        void FinishCurrentOperation();
    }
}