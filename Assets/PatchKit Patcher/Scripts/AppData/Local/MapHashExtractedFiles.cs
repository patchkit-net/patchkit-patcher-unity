using System.Collections.Generic;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public class MapHashExtractedFiles
    {
        private volatile Dictionary<string, string> _mapHash;
        
        public MapHashExtractedFiles()
        {
            _mapHash = new Dictionary<string, string>();
        }
        
        public string Add(string path)
        {
            string nameHash = HashCalculator.ComputeMD5Hash(path);
            lock (_mapHash)
            {
                _mapHash.Add(path, nameHash);
            }

            return nameHash;
        }

        public bool TryGetHash(string path,out string nameHash)
        {
            lock (_mapHash)
            {
                if (_mapHash.TryGetValue(path, out nameHash))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
