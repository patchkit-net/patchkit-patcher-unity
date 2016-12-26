namespace PatchKit.Unity.Patcher.Commands
{
    internal interface IValidateLicenseCommand : ICommand
    {
        string KeySecret { get; }
    }
}