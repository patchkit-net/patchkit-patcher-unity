using System.Threading.Tasks;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task Startup()
    {
        var updates = Task.WhenAll(
            FetchAppInfo(),
            FetchAppVersions(),
            FetchAppLatestVersionId(),
            FetchAppInstalledVersionId());

        Assert.IsNotNull(value: updates);

        if (State.AppState.ShouldBeUpdatedAutomatically)
        {
            await UpdateApp();
        }

        if (State.AppState.ShouldBeStartedAutomatically)
        {
            await StartApp();
        }

        ModifyState(x: () => State.Kind = PatcherStateKind.Idle);

        await updates;
    }
}