using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task<bool> TryToRestartWithHigherPermissions()
    {
        if (Application.platform == RuntimePlatform.WindowsPlayer)
        {
            string fileName = Application.dataPath.Replace("_Data", ".exe");

            string arguments = 
                string.Join(
                    " ",
                    Environment.GetCommandLineArgs().Select(s => $"\"{s}\"").ToArray()),

            var info = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true,
                Verb = "runas"
            };

            Process.Start(info);

            Quit2();

            return true;
        }

        return false;
    }
}