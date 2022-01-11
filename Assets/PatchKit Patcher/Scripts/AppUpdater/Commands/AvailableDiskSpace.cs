using System.IO;
using System.Runtime.InteropServices;
using PatchKit.Unity.Patcher.Debug;
using UniRx;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class AvailableDiskSpace
    {
        private ReactiveProperty<long> _freeDiskSpace = new ReactiveProperty<long>();

        public IReadOnlyReactiveProperty<long> FreeDiskSpace
        {
            get { return _freeDiskSpace; }
        }

        private static AvailableDiskSpace _instance;

        public static AvailableDiskSpace Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AvailableDiskSpace();
                }

                return _instance;
            }
        }

#if UNITY_STANDALONE_WIN
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetDiskFreeSpaceEx(string directoryName,
            out ulong freeBytes,
            out ulong totalBytes,
            out ulong totalFreeBytes);

#elif UNITY_STANDALONE_OSX
        [DllImport("getdiskspaceosx", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool getAvailableDiskSpace(string t_path, out long freeBytes);

#elif UNITY_STANDALONE_LINUX
        [DllImport("libgetdiskspace", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool getAvailableDiskSpace(string t_path, out long freeBytes);

#else
#error Unsupported platform
#endif

        public long GetAvailableDiskSpace(string localDirectoryPath)
        {
            Checks.ArgumentNotNull(localDirectoryPath, "localDirectoryPath");

#if UNITY_STANDALONE_WIN
            ulong freeBytes, totalBytes, totalFreeBytes;
            GetDiskFreeSpaceEx(new FileInfo(localDirectoryPath).Directory.FullName, out freeBytes, out totalBytes,
                out totalFreeBytes);

            _freeDiskSpace.Value = (long) freeBytes;
            return _freeDiskSpace.Value;
#else
            long freeBytes = 0;
            getAvailableDiskSpace(dir.Directory.FullName, out freeBytes);

            _freeDiskSpace.Value = (long) freeBytes;
            return _freeDiskSpace.Value;
#endif
        }
    }
}