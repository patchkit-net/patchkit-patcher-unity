using System;
using System.IO;
using System.Text;
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

        private const string PowershellProgramPreamble = @"$WshShell = New-Object -comObject WScript.Shell";
        private const string PowershellProgramShortcutLocation = @"$Shortcut = $WshShell.CreateShortcut(""{0}"")";
        private const string PowershellProgramTargetLocation = @"$Shortcut.TargetPath = ""{0}""";
        private const string PowershellProgramIconLocation = @"$Shortcut.IconLocation = ""{0}""";
        private const string PowershellProgramFinish = @"$Shortcut.Save()";

        private const string ShortcutExtension = ".lnk";

        private readonly string _destinationFilename;
        private readonly string _launcherExePath;
        private readonly string _iconLocation;

        private const string TempScriptFilename = "makeShortcut.ps1";


        public CreateDesktopShortcutCommand(string destinationFilename, string launcherExePath, string iconLocation = null)
        {
            _destinationFilename = destinationFilename;
            _launcherExePath = launcherExePath;
            _iconLocation = iconLocation;

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

            var writeableLocation = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            writeableLocation = Path.Combine(writeableLocation, "temp_dir_" + Path.GetRandomFileName());

            if (!desktopShortcutPath.EndsWith(ShortcutExtension))
            {
                desktopShortcutPath = Path.ChangeExtension(desktopShortcutPath, ShortcutExtension);
            }

            _logger.LogDebug(string.Format("Assembling icon creation script for {0}->{1}, with icon in {2}", desktopShortcutPath, _launcherExePath, _iconLocation));
            string effectiveScript = AssemblePowerShellProgram(desktopShortcutPath, _launcherExePath, _iconLocation);

            using (var tempDir = new TemporaryDirectory(writeableLocation))
            {
                tempDir.PrepareForWriting();

                var scriptFilePath = Path.Combine(tempDir.Path, TempScriptFilename);
                File.WriteAllText(scriptFilePath, effectiveScript);

                var processStartInfo = new ProcessStartInfo{
                    FileName = "PowerShell.exe", 
                    Arguments = "-ExecutionPolicy Unrestricted " + scriptFilePath,
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

        private static string AssemblePowerShellProgram(string shortcutPath, string targetPath, string iconPath = null)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(PowershellProgramPreamble);

            sb.AppendLine(string.Format(PowershellProgramShortcutLocation, shortcutPath));
            sb.AppendLine(string.Format(PowershellProgramTargetLocation, targetPath));

            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
            {
                sb.AppendLine(string.Format(PowershellProgramIconLocation, iconPath));
            }

            sb.AppendLine(PowershellProgramFinish);

            return sb.ToString();
        }
    }
}