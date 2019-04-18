using System.Threading.Tasks;

public partial class Patcher
{
    private async Task AcceptError()
    {
        switch (State.Error)
        {
            case PatcherError.InternalError:
            case PatcherError.MultipleInstancesError:
            case PatcherError.NoLauncherError:
                Quit();
                break;
            case PatcherError.UnauthorizedAccessError:
                // restart with launcher permissions
                break;
            case PatcherError.OutOfDiskSpaceError:
                ModifyState(x: () => State.Kind = PatcherStateKind.Idle);
                break;
        }
    }
}