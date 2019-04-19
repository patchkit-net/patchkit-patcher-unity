using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

public partial class Patcher
{
    private async Task<bool> TryToRestartWithHigherPermissions()
    {
        Debug.Log(message: "Trying to restart with higher permissions...");

        if (Application.platform != RuntimePlatform.WindowsPlayer)
        {
            Debug.Log(
                message:
                $"Cannot restart with higher permissions on platform '{Application.platform}'.");

            return false;
        }

        Assert.IsNotNull(value: Application.dataPath);

        string fileName = Application.dataPath.Replace(
            oldValue: "_Data",
            newValue: ".exe");

        string arguments = string.Join(
            separator: " ",
            value: Environment.GetCommandLineArgs()
                .Select(selector: s => $"\"{s}\"")
                .ToArray());

        Debug.Log(
            message:
            $"Restarting with higher permissions by executing '{fileName}' with arguments '{arguments}...'.");

        var info = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = true,
            Verb = "runas"
        };

        Process.Start(startInfo: info);

        Debug.Log(message: "Successfully restarted with higher permissions.");

        await Quit2();

        return true;
    }
}