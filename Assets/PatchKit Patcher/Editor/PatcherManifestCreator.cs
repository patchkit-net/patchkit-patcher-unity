using System;
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
            Manifest manifest;

            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    manifest = WindowsManifest(buildPath);
                    break;
#if UNITY_2017_3_OR_NEWER
                case BuildTarget.StandaloneOSX:
#else
                case BuildTarget.StandaloneOSXIntel64:
#endif
                    manifest = OsxManifest(buildPath);
                    break;
                case BuildTarget.StandaloneLinux:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneLinuxUniversal:
                    manifest = LinuxManifest(buildPath);
                    break;
                default:
                    throw new NotSupportedException();
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
                "security_fix_944",
                "preselected_executable",
                "execution_arguments"
            };
        }

        private static Manifest LinuxManifest(string buildPath)
        {
            string patcherExe = Path.GetFileName(buildPath);
            string launchScript = UnixLaunchScriptCreator.LaunchScriptName;

            string launchScriptPath = "{exedir}/" + launchScript;

            return new Manifest {
                ExeFileName = "sh",
                ExeArguments = "\"" + launchScriptPath + "\" --exedir \"{exedir}\" --patcher-exe \"" + patcherExe + "\" --secret \"{secret}\" --installdir \"{installdir}\"",

                Version = ManifestVersion,
                Target = "sh",
                Capabilities = Capabilities(),
                Arguments = new Manifest.Argument[] {
                    CreateManifestAgument(launchScriptPath),
                    CreateManifestAgument("--exedir", "{exedir}"),
                    CreateManifestAgument("--secret", "{secret}"),
                    CreateManifestAgument("--installdir", "{installdir}"),
                    CreateManifestAgument("--network-status", "{network-status}"),
                    CreateManifestAgument("--patcher-exe", patcherExe),
                    CreateManifestAgument("--lockfile", "{lockfile}"),
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
