using System.Threading.Tasks;

public partial class Patcher
{
    private async Task AcceptError()
    {
        if (State.Kind == PatcherStateKind.DisplayingInternalError)
        {
            Quit();
        }
        else if (State.Kind ==
            PatcherStateKind.DisplayingMultipleInstancesError)
        {
            Quit();
        }
        else if (State.Kind == PatcherStateKind.DisplayingNoLauncherError)
        {
            Quit();
        }
        else if (State.Kind ==
            PatcherStateKind.DisplayingUnauthorizedAccessError)
        {
            // restart with launcher permissions
        }
        else if (State.Kind == PatcherStateKind.DisplyingOutOfDiskSpaceError)
        {
            ModifyState(x: () => State.Kind = PatcherStateKind.Idle);
        }
    }
}