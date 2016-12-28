namespace PatchKit.Unity.Patcher.Commands
{
    internal interface ICheckVersionIntegrityCommand : ICommand
    {
        VersionIntegrity Results { get; }
    }
}