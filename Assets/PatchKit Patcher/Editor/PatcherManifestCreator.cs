using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Callbacks;

namespace PatchKit.Unity.Editor
{
    public static class PatcherManifestCreator
    {
        private const int ManifestVersion = 2;

        [PostProcessBuild, UsedImplicitly]
        private static void PostProcessBuild(BuildTarget buildTarget, string buildPath)
        {
            var manifest = new Manifest();
            
            if (buildTarget == BuildTarget.StandaloneWindows || buildTarget == BuildTarget.StandaloneWindows64)
            {
                manifest = WindowsManifest(buildPath);
            }

            if (buildTarget == BuildTarget.StandaloneOSX)
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

        private static Manifest WindowsManifest(string buildPath)
        {
            return CommonManifest(buildPath);
        }

        private static Manifest LinuxManifest(string buildPath)
        {
            return CommonManifest(buildPath);
        }

        private static Manifest CommonManifest(string buildPath)
        {
            string targetFile = Path.GetFileName(buildPath);
            return new Manifest {
                ExeFileName = $"\"{{exedir}}/{targetFile}\"",
                ExeArguments = "--installdir \"{installdir}\" --secret \"{secret}\"",

                Version = ManifestVersion,
                Target = "{exedir}/" + targetFile,
                Arguments = new Manifest.Argument[] {
                    new Manifest.Argument { Value = new string[] {
                        "--installdir", "{installdir}"
                    }},
                    new Manifest.Argument { Value = new string[] {
                        "--lockfile", "{lockfile}"
                    }},
                    new Manifest.Argument { Value = new string[] {
                        "--secret", "{secret}"
                    }},
                }
            };
        }

        private static Manifest OsxManifest(string buildPath)
        {
            string targetFile = Path.GetFileName(buildPath);
            return new Manifest {
                ExeFileName = "open",
                ExeArguments =
                    $"\"{{exedir}}/{targetFile}\" --args --installdir \"{{installdir}}\" --secret \"{{secret}}\"",

                Version = ManifestVersion,
                Target = "open",
                Arguments = new Manifest.Argument[] {
                    new Manifest.Argument { Value = new string[] {
                        "{exedir}/" + targetFile
                    }},
                    new Manifest.Argument { Value = new string[] {
                        "--args"
                    }},
                    new Manifest.Argument { Value = new string[] {
                        "--installdir", "{installdir}"
                    }},
                    new Manifest.Argument { Value = new string[] {
                        "--lockfile", "{lockfile}"
                    }},
                    new Manifest.Argument { Value = new string[] {
                        "--secret", "{secret}"
                    }},
                }
            };
        }
    }
}