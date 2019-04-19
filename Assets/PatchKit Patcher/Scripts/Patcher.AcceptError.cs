using System.Threading.Tasks;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task AcceptError()
    {
        Assert.IsTrue(
            condition: State.Kind == PatcherStateKind.DisplayingError);

        switch (State.Error)
        {
            case PatcherError.InternalError:
            case PatcherError.MultipleInstancesError:
                await Quit2();
                break;
            case PatcherError.NoLauncherError:
                if (!await TryToRestartWithLauncher())
                {
                    await Quit2();
                }

                break;
            case PatcherError.UnauthorizedAccessError:
                if (!await TryToRestartWithHigherPermissions())
                {
                    await Quit2();
                }

                break;
            case PatcherError.OutOfDiskSpaceError:
                ModifyState(x: () => State.Kind = PatcherStateKind.Idle);
                break;
        }
    }
}