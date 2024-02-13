﻿using System.Runtime.InteropServices;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    /// <summary>
    /// The file patcher.
    /// TODO: Create an interface and cleanup this class.
    /// </summary>
    public class FilePatcher
    {
        // TODO: Use rsync as executable.
        #if UNITY_EDITOR_LINUX
        [DllImport("librsync", EntryPoint = "rs_rdiff_patch")]
        #else
        [DllImport("rsync", EntryPoint = "rs_rdiff_patch")]
        #endif
        private static extern int rs_rdiff_patch(string basisFile, string deltaFile, string newFile);

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(FilePatcher));

        private readonly string _filePath;
        private readonly string _diffPath;
        private readonly string _outputFilePath;

        private bool _patchHasBeenCalled;

        public FilePatcher(string filePath, string diffPath, string outputFilePath)
        {
            Checks.ArgumentFileExists(filePath, "filePath");
            Checks.ArgumentFileExists(diffPath, "diffPath");
            Checks.ArgumentParentDirectoryExists(outputFilePath, "outputFilePath");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(filePath, "filePath");
            DebugLogger.LogVariable(diffPath, "diffPath");
            DebugLogger.LogVariable(outputFilePath, "outputFilePath");

            _filePath = filePath;
            _diffPath = diffPath;
            _outputFilePath = outputFilePath;
        }

        public void Patch()
        {
            Assert.MethodCalledOnlyOnce(ref _patchHasBeenCalled, "Patch");

            DebugLogger.Log("Patching.");

            int status = rs_rdiff_patch(Paths.Fix(_filePath), Paths.Fix(_diffPath), Paths.Fix(_outputFilePath));

            if (status != 0)
            {
                throw new LibrsyncException(status);
            }
        }
    }
}