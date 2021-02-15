using System.IO;

namespace PatchKit.Unity.Editor
{
    public class PatcherAssetModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        public static string[] OnWillSaveAssets(string[] paths)
        {
            bool isSavingScene = false;

            foreach (string path in paths)
            {
                if (string.Equals(Path.GetExtension(path), ".unity"))
                {
                    isSavingScene = true;
                    break;
                }
            }

            if (isSavingScene)
            {
                CustomizationSupport.SetActivePatcherComponentsAll(true);
            }

            return paths;
        }
    }
}