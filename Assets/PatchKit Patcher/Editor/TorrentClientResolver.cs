using System;
using System.IO;
using Ionic.Zip;
using JetBrains.Annotations;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents;
using UnityEditor;
using UnityEditor.Callbacks;

namespace PatchKit.Unity.Editor
{
    public static class TorrentClientResolver
    {
        private const string PackagesDirectory = "Assets/Editor/TorrentClientPackages";

        private static string ResolveClientPackage(BuildTarget buildTarget)
        {
            string packageFilename;

            switch (buildTarget)
            {
                case BuildTarget.StandaloneLinux:
                    packageFilename = "linux32.zip";
                    break;

                case BuildTarget.StandaloneLinux64:
                    packageFilename = "linux64.zip";
                    break;

                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    packageFilename = "win.zip";
                    break;

                case BuildTarget.StandaloneOSXIntel64:
                    packageFilename = "osx64.zip";
                    break;

                default:
                    throw new ArgumentException(buildTarget.ToString());
            }

            return Path.Combine(PackagesDirectory, packageFilename);
        }

        [PostProcessBuild, UsedImplicitly]
        private static void CopyTorrentClient(BuildTarget buildTarget, string buildPath)
        {
            string buildDir = Path.GetDirectoryName(buildPath);
            string targetClientDirectory = UnityTorrentClientProcessStartInfoProvider.TorrentClientDirectory;

            string clientPackage = ResolveClientPackage(buildTarget);
            string targetDir = Path.Combine(buildDir, targetClientDirectory);

            string expectedFilename = UnityTorrentClientProcessStartInfoProvider
                .ResolveTorrentClientFileName(CustomBuildScripts.BuildTargetToPlatformType(buildTarget));

            string expectedTorrentClientPath = Path.Combine(buildDir, expectedFilename);

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            using (var zip = new ZipFile(clientPackage))
            {
                zip.ExtractAll(targetDir, ExtractExistingFileAction.OverwriteSilently);
            }

            if (!File.Exists(expectedTorrentClientPath))
            {
                throw new BuildingException(string.Format("Failed to extract torrent-client, expected {0} to exist.", expectedTorrentClientPath));
            }
        }
    }
}