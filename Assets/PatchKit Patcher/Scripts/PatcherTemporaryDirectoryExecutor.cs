using System;
using System.IO;
using PatchKit.Core.IO;
using PatchKit.Apps.Updating.Debug;

namespace PatchKit.Patching.Unity
{
    public class UnityTemporaryDirectoryExecutor : ITemporaryDirectoryExecutor
    {
        TemporaryDirectory CreateTemporaryDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return new TemporaryDirectory(path);
        }
            
        public void Execute(Action<TemporaryDirectory> action)
        {
            using (var tempDir = CreateTemporaryDirectory())
            {
                try
                {
                    action(tempDir);
                }
                catch (Exception e)
                {
                    string envValue;
                    if (EnvironmentInfo.TryReadEnvironmentVariable(PatcherEnvironmentVariables.KeepTemporaryFilesOnError, out envValue))
                    {
                        if (envValue != "0")
                        {
                            tempDir.Keep = true;
                        }
                    }
                        
                    throw;
                }
            }
        }
    }
}