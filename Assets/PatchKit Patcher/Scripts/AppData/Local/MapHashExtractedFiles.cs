using System.Collections.Generic;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public class MapHashExtractedFiles
    {
        private static Dictionary<string, string> MapHash = new Dictionary<string, string>();

        public static void Clear()
        {
            MapHash.Clear();
        }
        
        public static string Add(string path)
        {
            string nameHash = HashCalculator.ComputeMD5Hash(path);
            MapHash.Add(path, nameHash);
            return nameHash;
        }

        public static bool TryGetHash(string path,out string nameHash)
        {
            if (MapHash.TryGetValue(path, out nameHash))
            {
                return true;
            }

            return false;
        }
    }
}
