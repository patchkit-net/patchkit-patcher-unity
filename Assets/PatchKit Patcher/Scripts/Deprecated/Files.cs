using PatchKit.Core.IO;

namespace Deprecated
{
public static class Files
{
    public static bool IsExecutable(
        string filePath)
    {
        return LibPkAppsContainer
            .Resolve<IsFileCurrentPlatformExecutableDelegate>()(
                new PatchKit.Core.IO.Path(filePath),
                null,
                null);
    }
}
}