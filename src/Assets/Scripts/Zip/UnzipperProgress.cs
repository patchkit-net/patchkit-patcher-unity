using JetBrains.Annotations;

namespace PatchKit.Unity.Patcher.Zip
{
    public struct UnzipperProgress : ICustomProgress
    {
        [CanBeNull]
        public string FileName { get; set; }

        public double Progress { get; set; }
    }
}