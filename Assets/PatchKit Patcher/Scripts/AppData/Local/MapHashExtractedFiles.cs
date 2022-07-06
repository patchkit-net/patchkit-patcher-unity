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
        
        public string GetNameHash(string path)
        {
            string nameHash;
            if (TryGetHash(path, out nameHash))
            {
                return nameHash;
            }
            
            nameHash = HashCalculator.ComputeMD5Hash(path);
            MapHash.Add(path, nameHash);
            return nameHash;
        }

        private bool TryGetHash(string path, out string nameHash)
        {
            return MapHash.TryGetValue(path, out nameHash);
        }
    }
}
