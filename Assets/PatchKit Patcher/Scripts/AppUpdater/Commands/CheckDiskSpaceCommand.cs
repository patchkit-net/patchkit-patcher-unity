using System.IO;
using System.Runtime.InteropServices;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class CheckDiskSpaceCommand : ICheckDiskSpace
    {
        private const int Reserve = 1024 * 1024 * 20; // 20 megabytes of reserve

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(CheckDiskSpaceCommand));

        private readonly AppContentSummary? _contentSummary;
        private readonly AppDiffSummary? _diffSummary;
        private readonly string _localDirectoryPath;
        private readonly long _bigestFileSize;

        public CheckDiskSpaceCommand(AppContentSummary contentSummary, string localDirectoryPath)
        {
            Checks.ArgumentNotNull(localDirectoryPath, "localDirectoryPath");

            _contentSummary = contentSummary;
            _localDirectoryPath = localDirectoryPath;
        }

        public CheckDiskSpaceCommand(AppDiffSummary diffSummary, string localDirectoryPath, long bigestFileSize)
        {
            Checks.ArgumentNotNull(localDirectoryPath, "localDirectoryPath");

            _diffSummary = diffSummary;
            _localDirectoryPath = localDirectoryPath;
            _bigestFileSize = bigestFileSize;
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

        // NOTE: libgetdiskspace doesn't work on Linux (Manjaro x86_64) in Editor (2017 LTS), has to be changed to getdiskspace
        [DllImport("libgetdiskspace", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool getAvailableDiskSpace(string t_path, out long freeBytes);

#else
#error Unsupported platform
#endif

        public void Execute(CancellationToken cancellationToken)
        {
            long availableDiskSpace = -1;
            long requiredDiskSpace = GetRequiredDiskSpace();

            var dir = new FileInfo(_localDirectoryPath);

#if UNITY_STANDALONE_WIN
            ulong freeBytes, totalBytes, totalFreeBytes;
            GetDiskFreeSpaceEx(dir.Directory.FullName, out freeBytes, out totalBytes, out totalFreeBytes);

            availableDiskSpace = (long) freeBytes;

#else

            long freeBytes = 0;
            getAvailableDiskSpace(dir.Directory.FullName, out freeBytes);

            availableDiskSpace = freeBytes;

#endif

            if (availableDiskSpace >= requiredDiskSpace)
            {
                DebugLogger.Log("Available free space " + availableDiskSpace + " >= required disk space " +
                                requiredDiskSpace);
            }
            else
            {
                throw new NotEnoughtDiskSpaceException("There's no enough disk space to install/update this application. " +
                                                       "Available free space " + availableDiskSpace +
                                                       " < required disk space " + requiredDiskSpace);
            }
        }

        private long GetRequiredDiskSpace()
        {
            if (UseContentSummary())
            {
                long requiredDiskSpaceForContent = GetRequiredDiskSpaceForContent();
                return requiredDiskSpaceForContent;
            }

            long requiredDiskSpaceForDiff = GetRequiredDiskSpaceForDiff();
            return requiredDiskSpaceForDiff;
        }

        private bool UseContentSummary()
        {
            return _contentSummary != null;
        }

        private long GetRequiredDiskSpaceForContent()
        {
            long uncompressedSize = _contentSummary.Value.UncompressedSize;
            if (uncompressedSize == 0) // before 2.4
            {
                // estimate the size
                uncompressedSize = (long) (_contentSummary.Value.Size * 1.4);
            }

            long requiredDiskSpace = _contentSummary.Value.Size + uncompressedSize + Reserve;
            return requiredDiskSpace;
        }

        private long GetRequiredDiskSpaceForDiff()
        {
            long uncompressedSize = _diffSummary.Value.UncompressedSize;
            if (uncompressedSize == 0) // before 2.4
            {
                // estimate the size
                uncompressedSize = (long) (_diffSummary.Value.Size * 1.4);
            }

            long requiredDiskSpace = _diffSummary.Value.Size + uncompressedSize + _bigestFileSize + Reserve;
            return requiredDiskSpace;
        }

        public void Prepare(UpdaterStatus status, CancellationToken cancellationToken)
        {
            // do nothing
        }
    }
}