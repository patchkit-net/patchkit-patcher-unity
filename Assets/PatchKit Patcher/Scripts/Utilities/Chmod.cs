using System.Diagnostics;
using System.IO;
using System.Net;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Utilities
{

public class Chmod
{
    private const string ChmodPath = "/bin/chmod";

    private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(Chmod));

    public static void SetExecutableFlag(string path, bool val = true)
    {
        Execute(val ? "+x" : "-x", path);
    }

    public static void SetMode(string mode, string path)
    {
        Execute(mode, path);
    }

    private static void Execute(string mode, string path)
    {
        Assert.IsTrue(Platform.IsPosix(), "Chmod can be run only on POSIX platforms");
        Assert.IsTrue(File.Exists(ChmodPath), "/bin/chmod should exist");

        var process = new Process
        {
            StartInfo =
            {
                FileName = ChmodPath,
                Arguments = string.Format("{0} \"{1}\"", mode, path)
            }
        };

        DebugLogger.Log(string.Format("Executing {0} {1}...", process.StartInfo.FileName,
            process.StartInfo.Arguments));

        process.Start();
        process.WaitForExit();

        DebugLogger.Log("Done");
    }
}

} // namespace