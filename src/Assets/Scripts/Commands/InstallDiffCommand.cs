using System.IO;
using System.Linq;
using PatchKit.Api.Models;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Data.Local;
using PatchKit.Unity.Patcher.Diff;
using UnityEngine.Assertions;

namespace PatchKit.Unity.Patcher.Commands
{
    internal class InstallDiffCommand : IInstallDiffCommand
    {
        private readonly string _packagePath;
        private readonly int _versionId;
        private readonly PatcherContext _context;

        public InstallDiffCommand(string packagePath, int versionId, PatcherContext context)
        {
            _packagePath = packagePath;
            _versionId = versionId;
            _context = context;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            var summary = _context.Data.RemoteData.MetaData.GetDiffSummary(_versionId);

            using (var packageDir = new TemporaryDirectory(_context.Data.LocalData.TemporaryData.GetUniquePath()))
            {
                var unarchiver = new Unarchiver(_packagePath, packageDir.Path);
                unarchiver.Unarchive(cancellationToken);

                ProcessAddedFiles(summary, packageDir.Path, cancellationToken);
                ProcessRemovedFiles(summary, cancellationToken);
                ProcessModifiedFiles(summary, packageDir.Path, cancellationToken);
            }
        }

        private void ProcessRemovedFiles(AppDiffSummary summary, CancellationToken cancellationToken)
        {
            var removedFiles = summary.RemovedFiles.Where(s => !s.EndsWith("/"));
            var removedDirectories = summary.RemovedFiles.Where(s => s.EndsWith("/"));

            foreach (var fileName in removedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _context.Data.LocalData.DeleteFile(fileName);
                _context.Data.LocalData.MetaData.RemoveFile(fileName);
            }

            foreach (var dirName in removedDirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_context.Data.LocalData.IsDirectoryEmpty(dirName))
                {
                    _context.Data.LocalData.DeleteDirectory(dirName);
                }
            }
        }

        private void ProcessAddedFiles(AppDiffSummary summary, string packageDirPath, CancellationToken cancellationToken)
        {
            foreach (var fileName in summary.AddedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (fileName.EndsWith("/"))
                {
                    _context.Data.LocalData.CreateDirectory(fileName);
                }
                else
                {
                    string sourceFilePath = Path.Combine(packageDirPath, fileName);

                    if (!File.Exists(sourceFilePath))
                    {
                        throw new InstallerException(string.Format("Cannot find file {0} in content package.", fileName));
                    }

                    _context.Data.LocalData.CreateOrUpdateFile(fileName, sourceFilePath);
                    _context.Data.LocalData.MetaData.AddOrUpdateFile(fileName, _versionId);
                }
            }
        }

        private void ProcessModifiedFiles(AppDiffSummary summary, string packageDirPath, CancellationToken cancellationToken)
        {
            foreach (var fileName in summary.ModifiedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!fileName.EndsWith("/"))
                {
                    PatchFile(fileName, packageDirPath);
                }
            }
        }

        private void PatchFile(string fileName, string packageDirPath)
        {
            if (!_context.Data.LocalData.FileExists(fileName) || !_context.Data.LocalData.MetaData.FileExists(fileName))
            {
                throw new InstallerException(string.Format("Couldn't patch file {0} - file doesn't exists.", fileName));
            }

            Assert.AreEqual(_versionId - 1, _context.Data.LocalData.MetaData.GetFileVersion(fileName),
                string.Format("Wrong file version {0}", fileName));

            string newFile = _context.Data.LocalData.TemporaryData.GetUniquePath();

            try
            {
                var filePatcher = new FilePatcher(_context.Data.LocalData.GetFilePath(fileName),
                    Path.Combine(packageDirPath, fileName), newFile);
                filePatcher.Patch();

                _context.Data.LocalData.CreateOrUpdateFile(fileName, newFile);
                _context.Data.LocalData.MetaData.AddOrUpdateFile(fileName, _versionId);
            }
            finally
            {
                if (File.Exists(newFile))
                {
                    File.Delete(newFile);
                }
            }
        }
    }
}