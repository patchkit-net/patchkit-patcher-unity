namespace PatchKit.Unity.Patcher
{
    internal interface IPatcherStrategyResolver
    {
        IPatcherStrategy Resolve(PatcherContext context);
    }
}