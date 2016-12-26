using System;
using System.IO;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.Data.Local
{
    internal class TemporaryData : TemporaryDirectory, ITemporaryData
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(TemporaryData));

        public TemporaryData(string path) : base(path)
        {
            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(path, "path");

            Checks.ArgumentNotNullOrEmpty(path, "path");
        }

        public string GetUniquePath()
        {
            string path = string.Empty;

            for (int i = 0; i < 1000; i++)
            {
                path = System.IO.Path.Combine(Path, Guid.NewGuid().ToString("N"));
                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    break;
                }
            }

            return path;
        }
    }
}
