namespace PatchKit.Unity.Patcher
{
    internal interface IPatcherStrategy
    {
        void Patch(PatcherContext context);
    }
}
