using System;
using System.Runtime.InteropServices;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    // TODO: Use rsync as executable.
    public class FilePatcher
    {
        [DllImport("rsync", EntryPoint = "rs_rdiff_patch")]
        private static extern int rs_rdiff_patch(string basisFile, string deltaFile, string newFile);

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(FilePatcher));

        private readonly string _filePath;
        private readonly string _diffPath;
        private readonly string _outputFilePath;

        private bool _started;

        public FilePatcher(string filePath, string diffPath, string outputFilePath)
        {
            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(filePath, "filePath");
            DebugLogger.LogVariable(diffPath, "diffPath");
            DebugLogger.LogVariable(outputFilePath, "outputFilePath");

            Checks.ArgumentFileExists(filePath, "filePath");
            Checks.ArgumentFileExists(diffPath, "diffPath");
            Checks.ArgumentDirectoryOfFileExists(outputFilePath, "outputFilePath");

            _filePath = filePath;
            _diffPath = diffPath;
            _outputFilePath = outputFilePath;
        }

        public void Patch()
        {
            DebugLogger.Log("Starting file patching.");

            if (_started)
            {
                throw new InvalidOperationException("Cannot start the same FilePatcher twice.");
            }
            _started = true;

            int status = rs_rdiff_patch(_filePath, _diffPath, _outputFilePath);

            if (status != 0)
            {
                throw new LibrsyncException(status);
            }
        }
    }
}