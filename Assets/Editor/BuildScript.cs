﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace PatchKit.Unity.Editor
{
    public class BuildScript
    {
        public static void BuildWindows32Release()
        {
            Build(BuildTarget.StandaloneWindows, false);
        }

        public static void BuildWindows32Development()
        {
            Build(BuildTarget.StandaloneWindows, !false);
        }

        public static void BuildWindows64Release()
        {
            Build(BuildTarget.StandaloneWindows64, false);
        }

        public static void BuildWindows64Development()
        {
            Build(BuildTarget.StandaloneWindows64, !false);
        }

        public static void BuildOSX64Release()
        {
            Build(BuildTargetOsx.Get(), false);
        }

        public static void BuildOSX64Development()
        {
            Build(BuildTargetOsx.Get(), !false);
        }

        public static void BuildLinux32Release()
        {
            Build(BuildTarget.StandaloneLinux, false);
        }

        public static void BuildLinux32Development()
        {
            Build(BuildTarget.StandaloneLinux, !false);
        }

        public static void BuildLinux64Release()
        {
            Build(BuildTarget.StandaloneLinux64, false);
        }

        public static void BuildLinux64Development()
        {
            Build(BuildTarget.StandaloneLinux64, !false);
        }

        public static void Build(BuildTarget target, bool development)
        {
            BuildPipeline.BuildPlayer(GetScenes(), Environment.GetCommandLineArgs().Last(), target,
                development ? BuildOptions.Development : BuildOptions.None);
        }

        private static string[] GetScenes()
        {
            var result = new List<string>();
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                var scene = EditorBuildSettings.scenes[i];
                if (scene.enabled)
                {
                    result.Add(scene.path);
                }
            }

            return result.ToArray();
        }
    }
}