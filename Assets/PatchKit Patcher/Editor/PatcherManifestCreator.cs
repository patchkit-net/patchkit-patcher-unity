using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Callbacks;

namespace PatchKit.Unity.Editor
{
    public static class PatcherManifestCreator
    {
        private const int ManifestVersion = 3;

        [PostProcessBuild, UsedImplicitly]
        private static void PostProcessBuild(BuildTarget buildTarget, string buildPath)
        {
            Manifest manifest = new Manifest();

            if (buildTarget == BuildTarget.StandaloneWindows || buildTarget == BuildTarget.StandaloneWindows64)
            {
                manifest = WindowsManifest(buildPath);
            }

            if (buildTarget == BuildTarget.StandaloneOSXUniversal ||
                buildTarget == BuildTarget.StandaloneOSXIntel ||
                buildTarget == BuildTarget.StandaloneOSXIntel64)
            {
                manifest = OsxManifest(buildPath);
            }

            if (buildTarget == BuildTarget.StandaloneLinux ||
                buildTarget == BuildTarget.StandaloneLinux64 ||
                buildTarget == BuildTarget.StandaloneLinuxUniversal)
            {
                manifest = LinuxManifest(buildPath);
            }

            string manifestPath = Path.Combine(Path.GetDirectoryName(buildPath), "patcher.manifest");
            string manifestContent = JsonConvert.SerializeObject(manifest, Formatting.Indented);

            if (File.Exists(manifestPath))
            {
                File.Delete(manifestPath);
            }

            File.WriteAllText(manifestPath, manifestContent);
        }

        private static Manifest.Argument CreateManifestAgument(params string[] args)
        {
            return new Manifest.Argument { Value = args };
        }

        private static Manifest LinuxManifest(string buildPath)
        {
            string patcherExe = Path.GetFileName(buildPath);
            string launchScript = UnixLaunchScriptCreator.LaunchScriptName;

            string launchScriptPath = "{exedir}/" + launchScript;

            return new Manifest {
                ExeFileName = "sh",
                ExeArguments = "\"" + launchScriptPath + "\" \"{exedir}\" \"" + patcherExe + "\" \"{secret}\" \"{installdir}\"",

                Version = ManifestVersion,
                Target = "sh",
                Arguments = new Manifest.Argument[] {
                    CreateManifestAgument(launchScriptPath),
                    CreateManifestAgument("--exedir={exedir}"),
                    CreateManifestAgument("--secret={secret}"),
                    CreateManifestAgument("--installdir={installdir}"),
                    CreateManifestAgument("--network-status={network-status}"),
                    CreateManifestAgument("--patcher-exe=" + patcherExe),
                    CreateManifestAgument("--lockfile={lockfile}"),
                }
            };
        }

        private static Manifest WindowsManifest(string buildPath)
        {
            string targetFile = Path.GetFileName(buildPath);
            return new Manifest {
                ExeFileName = string.Format("\"{{exedir}}/{0}\"", targetFile),
                ExeArguments = "--installdir \"{installdir}\" --secret \"{secret}\"",

                Version = ManifestVersion,
                Target = "{exedir}/" + targetFile,
                Arguments = new Manifest.Argument[] {
                    CreateManifestAgument("--installdir", "{installdir}"),
                    CreateManifestAgument("--lockfile", "{lockfile}"),
                    CreateManifestAgument("--secret", "{secret}"),
                    CreateManifestAgument("--{network-status}"),
                }
            };
        }

        private static Manifest OsxManifest(string buildPath)
        {
            string targetFile = Path.GetFileName(buildPath);
            return new Manifest {
                ExeFileName = "open",
                ExeArguments = string.Format("\"{{exedir}}/{0}\" --args --installdir \"{{installdir}}\" --secret \"{{secret}}\"", targetFile),

                Version = ManifestVersion,
                Target = "open",
                Arguments = new Manifest.Argument[] {
                    CreateManifestAgument("{exedir}/" + targetFile),
                    CreateManifestAgument("--args"),
                    CreateManifestAgument("--installdir", "{installdir}"),
                    CreateManifestAgument("--lockfile", "{lockfile}"),
                    CreateManifestAgument("--secret", "{secret}"),
                    CreateManifestAgument("--{network-status}"),
                }
            };
        }
    }
}