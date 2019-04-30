using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task AcceptError()
    {
        Debug.Log(message: "Accepting error...");

        Assert.IsTrue(
            condition: State.Kind == PatcherStateKind.DisplayingError);
        Assert.IsTrue(condition: State.Error.HasValue);

        Debug.Log(message: $"Error = {State.Error.Value}");

        switch (State.Error.Value)
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
            default:
                throw new ArgumentOutOfRangeException();
        }

        Assert.IsFalse(
            condition: State.Kind == PatcherStateKind.DisplayingError);

        ModifyState(x: () => State.Error = null);

        Debug.Log(message: "Successfully accepted error.");
    }
}