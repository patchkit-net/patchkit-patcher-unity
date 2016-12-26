namespace PatchKit.Unity.Patcher.Commands
{
    internal interface ICheckVersionIntegrityCommand : ICommand
    {
        FileIntegrity[] Results { get; }
    }
}