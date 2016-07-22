using System;
using System.IO;
using System.Runtime.InteropServices;
using PatchKit.Async;

namespace PatchKit.Unity.Patcher
{
    internal class LibrsyncException : Exception
    {
        public LibrsyncException(int status) : base(string.Format("librsync failure - {0}", status))
        {
        }
    }

    internal class Librsync
    {
        [DllImport("rsync", EntryPoint = "rs_rdiff_patch")]
        private static extern int rs_rdiff_patch(string basisFile, string deltaFile, string newFile);

        public void Patch(string filePath, string patchFilePath, AsyncCancellationToken cancellationToken)
        {
            string outputFilePath = filePath + ".patched";

            int status = rs_rdiff_patch(filePath, patchFilePath, outputFilePath);

            if (status != 0)
            {
                throw new LibrsyncException(status);
            }

            File.Copy(outputFilePath, filePath, true);
            File.Delete(outputFilePath);
        }
    }
}
