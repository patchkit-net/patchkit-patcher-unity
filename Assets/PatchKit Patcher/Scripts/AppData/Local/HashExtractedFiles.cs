namespace PatchKit.Unity.Patcher.AppData.Local
{
    public class HashExtractedFiles
    {
        public string Hash(string path)
        {
            return HashCalculator.ComputeMD5Hash(path);
        }
    }
}
