using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PatchKit.Unity.Patcher.Diff
{
    internal class Librsync
    {
        [DllImport("rsync", EntryPoint = "rs_rdiff_patch")]
        private static extern int rs_rdiff_patch(string basisFile, string deltaFile, string newFile);

        public void Patch(string filePath, string diffFilePath)
        {
            Debug.Log(string.Format("Patching {0} with diff {1}", filePath, diffFilePath));

            string outputFilePath = filePath + ".patched";

            try
            {
                int status = rs_rdiff_patch(filePath, diffFilePath, outputFilePath);

                if (status != 0)
                {
                    throw new LibrsyncException(status);
                }

                File.Copy(outputFilePath, filePath, true);
            }
            finally
            {
                File.Delete(outputFilePath);
            }
        }
    }
}