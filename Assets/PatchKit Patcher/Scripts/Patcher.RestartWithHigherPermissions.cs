using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

public partial class Patcher
{
    private async Task<bool> RestartWithHigherPermissionsAsync()
    {
        if (!CanPerformNewForegroundTask())
        {
            return false;
        }

        Debug.Log(message: "Restarting with higher permissions...");

        _hasRestartWithHigherPermissionsTask = true;
        SendStateChanged();

        try
        {
            if (Application.platform != RuntimePlatform.WindowsPlayer)
            {
                Debug.Log(
                    message:
                    $"Failed to restart with higher permissions: impossible on platform '{Application.platform}'.");

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

            Debug.Log(
                message: "Successfully restarted with higher permissions.");
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                message: 
                "Failed to restart with higher permissions: unknown error.");
            Debug.LogException(exception: e);

            return false;
        }
        finally
        {
            _hasRestartWithHigherPermissionsTask = false;
            SendStateChanged();
        }

        await QuitAsync();

        return true;
    }
}