public struct AppUpdateTaskState
{
    public AppUpdateTaskState(
        long totalBytes,
        long installedBytes,
        double bytesPerSecond,
        double progress,
        bool isConnecting)
    {
        TotalBytes = totalBytes;
        InstalledBytes = installedBytes;
        BytesPerSecond = bytesPerSecond;
        Progress = progress;
        IsConnecting = isConnecting;
    }

    public long TotalBytes { get; }

    public long InstalledBytes { get; }

    public double BytesPerSecond { get; }

    public double Progress { get; }

    public bool IsConnecting { get; }

    public override string ToString()
    {
        return $"{{ \"TotalBytes\": {TotalBytes}, " +
            $"\"InstalledBytes\": {InstalledBytes}, " +
            $"\"BytesPerSecond\": {BytesPerSecond}, " +
            $"\"Progress\": {Progress}, " +
            $"\"IsConnecting\": {IsConnecting.ToString().ToLower()} }}";
    }
}

public struct AppState
{
    public AppState(
        bool isInstalled,
        string path,
        string secret,
        PatchKit.Api.Models.App? info,
        PatchKit.Api.Models.AppVersion[] versions,
        int? installedVersionId,
        string installedVersionLabel,
        int? latestVersionId,
        string latestVersionLabel,
        AppUpdateTaskState? updateTask,
        bool isStarting)
    {
        IsInstalled = isInstalled;
        Path = path;
        Secret = secret;
        Info = info;
        Versions = versions;
        InstalledVersionId = installedVersionId;
        InstalledVersionLabel = installedVersionLabel;
        LatestVersionId = latestVersionId;
        LatestVersionLabel = latestVersionLabel;
        UpdateTask = updateTask;
        IsStarting = isStarting;
    }

    public bool IsInstalled { get; }

    public string Path { get; }

    public string Secret { get; }

    public PatchKit.Api.Models.App? Info { get; }

    public PatchKit.Api.Models.AppVersion[] Versions { get; }

    public int? InstalledVersionId { get; }

    public string InstalledVersionLabel { get; }

    public int? LatestVersionId { get; }

    public string LatestVersionLabel { get; }

    public AppUpdateTaskState? UpdateTask { get; }

    public bool IsStarting { get; }

    public override string ToString()
    {
        return $"{{ \"Info\": {Info?.ToString() ?? "null"}, " +
            $"\"Versions\": {Versions?.ToString() ?? "null"}, " +
            $"\"InstalledVersionId\": {InstalledVersionId?.ToString() ?? "null"}, " +
            $"\"InstalledVersionLabel\": {InstalledVersionLabel?.SurroundWithQuotes() ?? "null"}, " +
            $"\"LatestVersionId\": {LatestVersionId?.ToString() ?? "null"}, " +
            $"\"LatestVersionLabel\": {LatestVersionLabel?.SurroundWithQuotes() ?? "null"}, " +
            $"\"UpdateTask\": {UpdateTask?.ToString() ?? "null"}, " +
            $"\"IsStarting\": {IsStarting.ToString().ToLower()} }}";
    }
}

public struct State
{
    public State(
        bool isInitializing,
        AppState? app,
        bool isOnline,
        bool isQuitting)
    {
        IsInitializing = isInitializing;
        App = app;
        IsOnline = isOnline;
        IsQuitting = isQuitting;
    }

    public bool IsInitializing { get; }

    public AppState? App { get; }

    public bool IsOnline { get; }

    public bool IsQuitting { get; }

    public override string ToString()
    {
        return $"{{ \"IsInitializing\": {IsInitializing.ToString().ToLower()}, " +
            $"\"App\": {App?.ToString() ?? "null"}, " +
            $"\"IsOnline\": {IsOnline.ToString().ToLower()}, " +
            $"\"IsQuitting\": {IsQuitting.ToString().ToLower()} }}";
    }
}