using System;
using System.IO;
using System.Diagnostics;
using PatchKit.Logging;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.AppData.Local;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class CreateDesktopShortcutCommand : BaseAppUpdaterCommand
    {
        private readonly ILogger _logger;

        private const string PowershellProgram = @"$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut(""{0}"")
$Shortcut.TargetPath = ""{1}""
$Shortcut.Save()";

        private readonly string _destinationFilename;
        private readonly string _launcherExePath;

        public CreateDesktopShortcutCommand(string destinationFilename, string launcherExePath)
        {
            _destinationFilename = destinationFilename;
            _launcherExePath = launcherExePath;

            _logger = PatcherLogManager.DefaultLogger;
        }

        public override void Prepare(IStatusMonitor statusMonitor)
        {
            base.Prepare(statusMonitor);
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var desktopShortcutPath = Path.Combine(desktopPath, _destinationFilename);

            string effectiveScript = string.Format(PowershellProgram, desktopShortcutPath, _launcherExePath);

            const string tempDirName = "creatingShortcut";
            const string scriptFilename = "makeShortcut.ps1";

            using (var tempDir = new TemporaryDirectory(tempDirName))
            {
                tempDir.PrepareForWriting();

                var scriptFilePath = Path.Combine(tempDir.Path, scriptFilename);
                File.WriteAllText(scriptFilePath, effectiveScript);

                var processStartInfo = new ProcessStartInfo{
                    FileName = "PowerShell.exe", 
                    Arguments = scriptFilePath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    process.WaitForExit();

                    var output = process.StandardOutput.ReadToEnd();
                    if (!string.IsNullOrEmpty(output))
                    {
                        UnityEngine.Debug.Log("Output: " + output);
                    }

                    var stdErr = process.StandardError.ReadToEnd();
                    if (!string.IsNullOrEmpty(stdErr))
                    {
                        UnityEngine.Debug.LogError("Err: " + stdErr);
                    }
                }

                File.Delete(scriptFilePath);
            }
        }
    }
}