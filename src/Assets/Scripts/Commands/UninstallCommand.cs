using System.IO;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.Commands
{
    internal class UninstallCommand : IUninstallCommand
    {
        private readonly PatcherContext _context;

        public UninstallCommand(PatcherContext context)
        {
            _context = context;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            foreach (var fileName in _context.Data.LocalData.MetaData.GetFileNames())
            {
                _context.Data.LocalData.DeleteFile(fileName);
                string directoryName = Path.GetDirectoryName(fileName);
                if (_context.Data.LocalData.IsDirectoryEmpty(directoryName))
                {
                    _context.Data.LocalData.DeleteDirectory(directoryName);
                }

                _context.Data.LocalData.MetaData.RemoveFile(fileName);
            }
        }
    }
}