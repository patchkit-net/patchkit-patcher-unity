using System.IO;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using PatchKit.Core.Assertions;
using PatchKit.Core.IO;

namespace PatchKit.Patching.Unity
{
    public class UnityDiskSpaceChecker : IDiskSpaceChecker
    {
        
#region CONDITIONALLY_COMPILED
#if UNITY_STANDALONE_WIN
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)] 
        [return: MarshalAs(UnmanagedType.Bool)] 
        private static extern bool GetDiskFreeSpaceEx(string directoryName, 
            out ulong freeBytes, 
            out ulong totalBytes, 
            out ulong totalFreeBytes); 
    
        private long CalculateAvailableDiskSpace([NotNull] string path)
        {
            ulong freeBytes, totalBytes, totalFreeBytes;
            GetDiskFreeSpaceEx(path, out freeBytes, out totalBytes, out totalFreeBytes);

            return (long) freeBytes;
        }
        
#elif UNITY_STANDALONE_OSX 
        [DllImport("getdiskspaceosx", SetLastError = true, CharSet = CharSet.Auto)] 
        [return: MarshalAs(UnmanagedType.Bool)] 
        private static extern bool getAvailableDiskSpace(string path, out long freeBytes); 
    
        private long CalculateAvailableDiskSpace([NotNull] string path)
        {
            long freeBytes = 0;
            getAvailableDiskSpace(path, out freeBytes);

            return freeBytes;
        }
        
#elif UNITY_STANDALONE_LINUX
        [DllImport("getdiskspace", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool getAvailableDiskSpace(string path, out long freeBytes);
        
        private long CalculateAvailableDiskSpace([NotNull] string path)
        {
            long freeBytes = 0;
            getAvailableDiskSpace(path, out freeBytes);

            return freeBytes;
        }
#endif
#endregion
 
        public long AvailableDiskSpace(FileSystemPath path)
        {
            string dirPath = Path.GetDirectoryName(path.Value);
            
            dirPath.ShouldNotBeNull();
            
            return CalculateAvailableDiskSpace(dirPath);
        }
    }
}