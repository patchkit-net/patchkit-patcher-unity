using System.IO;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public class TemporaryData : TemporaryDirectory, ITemporaryData
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(TemporaryData));

        public TemporaryData(string path) : base(path)
        {
            Checks.ArgumentNotNullOrEmpty(path, "path");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(path, "path");
        }

        public string GetUniquePath()
        {
            string path = string.Empty;

            for (int i = 0; i < 1000; i++)
            {
                // Use first function if second would cause problems.
                //path = System.IO.Path.Combine(Path, Guid.NewGuid().ToString("N"));
                path = System.IO.Path.Combine(Path, System.IO.Path.GetRandomFileName());
                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    break;
                }
            }

            return path;
        }
    }
}