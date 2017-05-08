using System.IO;

namespace PatchKit.Unity.Utilities
{
    public class Files
    {
        public static void CreateParents(string path)
        {
            var dirName = Path.GetDirectoryName(path);
            if (dirName != null)
            {
                Directory.CreateDirectory(dirName);
            }
        }
    }
}