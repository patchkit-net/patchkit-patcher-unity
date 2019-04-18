using System.Threading.Tasks;

public partial class Patcher
{
    private async Task AcceptError()
    {
        switch (State.Error)
        {
            case PatcherError.InternalError:
            case PatcherError.MultipleInstancesError:
                await Quit();
                break;
            case PatcherError.NoLauncherError:
                if (!await TryToRestartWithLauncher())
                {
                    await Quit();
                }
                break;
            case PatcherError.UnauthorizedAccessError:
                if (!await TryToRestartWithHigherPermissions())
                {
                    await Quit();
                }
                break;
            case PatcherError.OutOfDiskSpaceError:
                ModifyState(x: () => State.Kind = PatcherStateKind.Idle);
                break;
        }
    }
}