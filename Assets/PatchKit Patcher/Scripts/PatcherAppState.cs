using JetBrains.Annotations;
using PatchKit.Api.Models;

public class PatcherAppState
{
    public PatcherAppState(
        [NotNull] string secret,
        [NotNull] string path,
        int? overrideLatestVersionId)
    {
        Secret = secret;
        Path = path;
        OverrideLatestVersionId = overrideLatestVersionId;
    }

    [NotNull]
    public string Secret { get; }

    [NotNull]
    public string Path { get; }

    public bool ShouldBeUpdatedAutomatically { get; set; }

    public bool ShouldBeStartedAutomatically { get; set; }

    public string LicenseKey { get; set; }

    public PatcherLicenseKeyIssue LicenseKeyIssue { get; set; }

    public int? OverrideLatestVersionId { get; }

    public int? InstalledVersionId { get; set; }

    public int? LatestVersionId { get; set; }

    public App? Info { get; set; }

    public AppVersion[] Versions { get; set; }
}