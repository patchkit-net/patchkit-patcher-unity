using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PatchKit.Unity.Editor
{
    public class PatcherVersionInfoCreator
    {
        [MenuItem("Edit/Save Patcher Version Info")]
        public static void SaveVersionInfo()
        {
            try
            {
                File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "patcher.versioninfo"), GetVersionInfo());
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                UnityEngine.Debug.LogError("Unable to save patcher version info.");
            }
        }

        private static string GetVersionInfo()
        {
            var commitHash = GetCommitHash();
            var commitTags = GetCommitTags(commitHash);
            var branchName = GetBranchName();

            return string.Format("{0} (tags: {1}, branch: {2})", commitHash, commitTags, branchName);
        }

        private static string GetBranchName()
        {
            return RunGitCommand("symbolic-ref HEAD").TrimEnd('\n');
        }

        private static string GetCommitTags(string commitHash)
        {
            return RunGitCommand(string.Format("tag -- contains {0}", commitHash)).TrimEnd('\n');
        }

        private static string GetCommitHash()
        {
            return RunGitCommand("rev-parse HEAD").TrimEnd('\n');
        }

        private static string RunGitCommand(string command)
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
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(1000);

            return output;
        }
    }
}
