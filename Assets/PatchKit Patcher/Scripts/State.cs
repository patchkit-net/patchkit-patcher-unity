public struct AppUpdateTaskState
{
    public long TotalBytes { get; }

    public long InstalledBytes { get; }

    public double BytesPerSecond { get; }

    public double Progress { get; }

    public bool IsConnecting { get; }

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

    public override string ToString()
    {
        return $"{{ \"TotalBytes\": {TotalBytes}, " +
            $"\"InstalledBytes\": {InstalledBytes}, " +
            $"\"BytesPerSecond\": {BytesPerSecond} }}";
    }
}

public struct AppState
{
    public string Secret { get; }

    public string Name { get; }

    public PatchKit.Api.Models.App? Info { get; }

    public PatchKit.Api.Models.AppVersion[] Versions { get; }

    public int? InstalledVersionId { get; }

    public string InstalledVersionLabel { get; }

    public int? LatestVersionId { get; }

    public string LatestVersionLabel { get; }

    public AppUpdateTaskState? UpdateTask { get; }

    public AppState(
        string secret,
        string name,
        PatchKit.Api.Models.App? info,
        PatchKit.Api.Models.AppVersion[] versions,
        int? installedVersionId,
        string installedVersionLabel,
        int? latestVersionId,
        string latestVersionLabel,
        AppUpdateTaskState? updateTask)
    {
        Secret = secret;
        Name = name;
        Info = info;
        Versions = versions;
        InstalledVersionId = installedVersionId;
        InstalledVersionLabel = installedVersionLabel;
        LatestVersionId = latestVersionId;
        LatestVersionLabel = latestVersionLabel;
        UpdateTask = updateTask;
    }

    public override string ToString()
    {
        return $"{{ \"Name\": {Name?.SurroundWithQuotes() ?? "null"}, " +
            $"\"Info\": {Info?.ToString() ?? "null"}, " +
            $"\"Versions\": {Versions?.ToString() ?? "null"}, " +
            $"\"InstalledVersionId\": {InstalledVersionId?.ToString() ?? "null"}, " +
            $"\"InstalledVersionLabel\": {InstalledVersionLabel?.SurroundWithQuotes() ?? "null"}, " +
            $"\"LatestVersionId\": {LatestVersionId?.ToString() ?? "null"}, " +
            $"\"LatestVersionLabel\": {LatestVersionLabel?.SurroundWithQuotes() ?? "null"}, " +
            $"\"UpdateTask\": {UpdateTask?.ToString() ?? "null"} }}";
    }
}

public struct State
{
    public State(
        AppState? app,
        bool isOnline)
    {
        App = app;
        IsOnline = isOnline;
    }

    public AppState? App { get; }

    public bool IsOnline { get; }

    public override string ToString()
    {
        return $"{{ \"App\": {App?.ToString() ?? "null"}, " +
            $"\"IsOnline\": {IsOnline.ToString().ToLower()} }}";
    }
}