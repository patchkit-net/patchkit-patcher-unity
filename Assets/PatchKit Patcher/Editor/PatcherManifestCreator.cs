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

        private static Manifest LinuxManifest(string buildPath)
        {
            string patcherExe = Path.GetFileName(buildPath);
            string launchScript = UnixLaunchScriptCreator.LaunchScriptName;

            return new Manifest {
                ExeFileName = string.Format("\"{{exedir}}/{0}\"", patcherExe),
                ExeArguments = "--installdir \"{installdir}\" --secret \"{secret}\"",

                Version = ManifestVersion,
                Target = "sh",
                Arguments = new Manifest.Argument[] {
                    new Manifest.Argument { Value = new string[] {
                        "{exedir}/" + launchScript
                    }},
                    new Manifest.Argument { Value = new string[] {
                        "{exedir}",
                        patcherExe,
                        "{secret}",
                        "{installdir}"
                    }},
                    new Manifest.Argument { Value = new string[] {
                        "{lockfile}"
                    }}
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
                ExeArguments = string.Format("\"{{exedir}}/{0}\" --args --installdir \"{{installdir}}\" --secret \"{{secret}}\"", targetFile),

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