using System.IO;
using PatchKit.Core.IO;
using Path = System.IO.Path;

namespace Utilities
{
public static class Files
{
    public static void CreateParents(string path)
    {
        var dirName = Path.GetDirectoryName(path);
        if (dirName != null)
        {
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }
        }
    }

    public static bool IsExecutable(
        string filePath,
        PlatformType platformType)
    {
        return LibPkAppsContainer
            .Resolve<IsFileCurrentPlatformExecutableDelegate>()(
                new PatchKit.Core.IO.Path(filePath),
                null,
                null);
    }
}
}