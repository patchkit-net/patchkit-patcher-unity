using System.IO;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class CheckDiskSpaceCommand : ICheckDiskSpace
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(CheckDiskSpaceCommand));

        private readonly AppContentSummary _contentSummary;
        private readonly string _localDirectoryPath;

        public CheckDiskSpaceCommand(AppContentSummary contentSummary, string localDirectoryPath)
        {
            _contentSummary = contentSummary;
            _localDirectoryPath = localDirectoryPath;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            int reserve = 1024 * 1024 * 20;
            long requiredDiskSpace = (_contentSummary.Size + _contentSummary.UncompressedSize + reserve);

            var dir = new FileInfo(_localDirectoryPath);
            var drive = new DriveInfo(dir.Directory.Root.FullName);

            if (drive.AvailableFreeSpace >= requiredDiskSpace)
            {
                DebugLogger.Log("Available free space " + drive.AvailableFreeSpace + " >= required disk space " +
                                requiredDiskSpace);
            }
            else
            {
                throw new NotEnoughtDiskSpaceException("There's no enough disk space to install this application. " +
                                                       "Available free space " + drive.AvailableFreeSpace +
                                                       " < required disk space " + requiredDiskSpace);
            }

            throw new NotEnoughtDiskSpaceException("There's no enough disk space to install this application. " +
                                                   "Available free space " + drive.AvailableFreeSpace +
                                                   " < required disk space " + requiredDiskSpace);
        }

        public void Prepare(IStatusMonitor statusMonitor)
        {
            // do nothing
        }
    }
}