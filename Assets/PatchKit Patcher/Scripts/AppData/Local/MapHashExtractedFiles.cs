using System.Collections.Generic;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public class MapHashExtractedFiles
    {
        private Dictionary<string, string> MapHash;
        
        public MapHashExtractedFiles()
        {
            MapHash = new Dictionary<string, string>();
        }
        
        public string Add(string path)
        {
            string nameHash = HashCalculator.ComputeMD5Hash(path);
            MapHash.Add(path, nameHash);
            return nameHash;
        }

        public bool TryGetHash(string path,out string nameHash)
        {
            if (MapHash.TryGetValue(path, out nameHash))
            {
                return true;
            }

            return false;
        }
    }
}
