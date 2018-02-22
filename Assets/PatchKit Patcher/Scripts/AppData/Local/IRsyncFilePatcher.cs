using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public interface IRsyncFilePatcher
    {
        void Patch(string filePath, string diffPath, string outputFilePath, CancellationToken cancellationToken);
    }
}