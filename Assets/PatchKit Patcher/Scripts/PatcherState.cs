using JetBrains.Annotations;

public class PatcherState
{
    public PatcherState(
        [NotNull] string appSecret,
        [NotNull] string appPath,
        int? overrideAppLatestVersionId,
        string lockFilePath)
    {
        AppState = new PatcherAppState(
            secret: appSecret,
            path: appPath,
            overrideLatestVersionId: overrideAppLatestVersionId);

        UpdateAppState = new PatcherUpdateAppState();

        LockFilePath = lockFilePath;
    }

    [NotNull]
    public PatcherAppState AppState { get; }

    [NotNull]
    public PatcherUpdateAppState UpdateAppState { get; }

    public string LockFilePath { get; }

    public PatcherStateKind Kind { get; set; }

    public bool IsOnline { get; set; }

    public bool HasChanged { get; set; }
}