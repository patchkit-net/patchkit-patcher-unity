using JetBrains.Annotations;
using PatchKit.Api.Models;
using App = PatchKit.Api.Models.App;

namespace PatchKit_Patcher.Scripts
{
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

    public int? OverrideLatestVersionId { get; }

    public int? InstalledVersionId { get; set; }

    public int? LatestVersionId { get; set; }

    public App? Info { get; set; }

    public AppVersion[] Versions { get; set; }
}
}