using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace PatchKit_Patcher.Scripts
{
public class PatcherAppInstalledVersionIdRetriever : IPatcherWorker
{
    [NotNull]
    private readonly PatcherState _state;

    private bool _retrieved;

    private LibPatchKitAppsGetAppInstalledVersionIdContext _context;

    public PatcherAppInstalledVersionIdRetriever([NotNull] PatcherState state)
    {
        _state = state;
    }

    public void ForceRefresh()
    {
        _retrieved = false;
    }

    public void Update()
    {
        if (_retrieved)
        {
            return;
        }

        if (_context == null)
        {
            _context =
                LibPatchKitApps.GetAppInstalledVersionId(
                    path: _state.AppState.Path);

            Assert.IsNotNull(value: _context);
        }

        if (_context.IsExecuting)
        {
            return;
        }

        if (_context.Error == LibPatchKitAppsGetAppInstalledVersionIdError.None)
        {
            _retrieved = true;
            _state.AppState.InstalledVersionId = _context.Result > 0
                ? (int?) _context.Result
                : null;
        }

        _context.Dispose();
        _context = null;
    }
}
}