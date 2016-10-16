using PatchKit.Unity.Patcher.Zip;

namespace PatchKit.Unity.Patcher.Statistics
{
    public delegate void CustomProgressHandler<T>(T progress) where T : ICustomProgress;
}