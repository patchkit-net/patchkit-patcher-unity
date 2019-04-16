public class PatcherUpdateAppState
{
    public bool IsConnecting { get; set; }

    public long InstalledBytes { get; set; }

    public long TotalBytes { get; set; }

    public double Progress { get; set; }

    public double BytesPerSecond { get; set; }
}