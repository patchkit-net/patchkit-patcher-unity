using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Callbacks;

namespace PatchKit.Unity.Editor
{
    public static class PatcherManifestCreator
    {
        private const int ManifestVersion = 4;

        private static void SaveTestManifest(Manifest manifest)
        {
            string targetLocation = EditorUtility.SaveFilePanel("Choose test manifest location", "", "patcher.manifest", "test");

            File.WriteAllText(targetLocation, JsonConvert.SerializeObject(manifest, Formatting.Indented));
        }

        [MenuItem("Tools/PatchKit Patcher Internal/Manifest/Windows")]
        private static void CreateTestManifestWindows()
        {
            SaveTestManifest(WindowsManifest("BUILD_PATH"));
        }

        [MenuItem("Tools/PatchKit Patcher Internal/Manifest/Linux")]
        private static void CreateTestManifestLinux()
        {
            SaveTestManifest(LinuxManifest("BUILD_PATH"));
        }

        [MenuItem("Tools/PatchKit Patcher Internal/Manifest/Osx")]
        private static void CreateTestManifestOsx()
        {
            SaveTestManifest(OsxManifest("BUILD_PATH"));
        }

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

            File.WriteAllText(manifestPath, manifestContent);
        }

        private static Manifest.Argument CreateManifestAgument(params string[] args)
        {
            return new Manifest.Argument { Value = args };
        }

        private static string[] Capabilities()
        {
            return new []{
                "pack1_compression_lzma2",
            };
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
                Capabilities = Capabilities(),
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
                Capabilities = Capabilities(),
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
                Capabilities = Capabilities(),
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
