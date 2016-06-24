using System;
using System.IO;
using System.Runtime.InteropServices;
using PatchKit.API.Async;

namespace PatchKit.Unity.Patcher.Utilities
{
    internal class LibrsyncException : Exception
    {
        public LibrsyncException(int status) : base(string.Format("rdiff failure - {0}", status))
        {
        }
    }

    internal class Librsync
    {
        [DllImport("rsync", EntryPoint = "rs_file_open")]
        private static extern IntPtr rs_file_open(string filename, string mode);

        [DllImport("rsync", EntryPoint = "rs_file_close")]
        private static extern int rs_file_close(IntPtr file);

        [DllImport("rsync", EntryPoint = "rs_patch_file")]
        private static extern int rs_patch_file(IntPtr basisFile, IntPtr deltaFile, IntPtr newFile, IntPtr stats);

        public void Patch(string filePath, string patchFilePath, AsyncCancellationToken cancellationToken)
        {
            string outputFilePath = filePath + ".patched";

            var basisFile = rs_file_open(filePath, "rb");
            var deltaFile = rs_file_open(patchFilePath, "rb");
            var newFile = rs_file_open(outputFilePath, "wb");

            try
            {
                int status = rs_patch_file(basisFile, deltaFile, newFile, IntPtr.Zero);

                if (status != 0)
                {
                    throw new LibrsyncException(status);
                }
            }
            finally
            {
                rs_file_close(basisFile);
                rs_file_close(deltaFile);
                rs_file_close(newFile);
            }

            File.Copy(outputFilePath, filePath, true);
            File.Delete(outputFilePath);
        }
    }
}
