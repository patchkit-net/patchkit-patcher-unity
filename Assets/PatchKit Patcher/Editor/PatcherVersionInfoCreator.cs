using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PatchKit.Unity.Editor
{
    public class PatcherVersionInfoCreator
    {
        public static void SaveVersionInfo()
        {
            try
            {
                var versionInfo = GetVersionInfo();
                Debug.Log("Writing version info: " + versionInfo);
                File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "patcher.versioninfo"), versionInfo);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("Unable to save patcher version info.");
            }
        }

        private static string GetVersionInfo()
        {
            var commitHash = GetCommitHash();
            var commitTags = GetCommitTags();
            var branchName = GetBranchName();

            return string.Format("{0} (tags: {1}, branch: {2})", commitHash, commitTags, branchName);
        }

        private static string GetBranchName()
        {
            string output;
            if (RunGitCommand("symbolic-ref HEAD", out output))
            {
                return output.TrimEnd('\n');
            }

            return "(unknown)";
        }

        private static string GetCommitTags()
        {
            string output;
            if (RunGitCommand("describe --abbrev=0 --tags --exact-match", out output))
            {
                return output.TrimEnd('\n');
            }

            return "(unknown)";
        }

        private static string GetCommitHash()
        {
            string output;
            if (RunGitCommand("rev-parse HEAD", out output))
            {
                return output.TrimEnd('\n');
            }

            return "(unknown)";
        }

        private static bool RunGitCommand(string command, out string output)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = "git",
                    Arguments = command,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(1000);

            return process.ExitCode == 0;
        }
    }
}
