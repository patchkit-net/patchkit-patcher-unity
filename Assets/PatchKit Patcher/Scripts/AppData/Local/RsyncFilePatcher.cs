using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using PatchKit.Logging;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    /// <summary>
    /// The file patcher.
    /// </summary>
    public class RsyncFilePatcher : IRsyncFilePatcher
    {
#if UNITY_EDITOR_LINUX
        [DllImport("librsync", EntryPoint = "rs_rdiff_patch")]
#else
        [DllImport("rsync", EntryPoint = "rs_rdiff_patch")]
        #endif
        private static extern int rs_rdiff_patch(string basisFile, string deltaFile, string newFile);

        private readonly ILogger _logger;

        public RsyncFilePatcher([NotNull] ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _logger = logger;
        }

        public void Patch([NotNull] string filePath, [NotNull] string diffPath, [NotNull] string outputFilePath,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("Value cannot be null or empty.", "filePath");
            }

            if (string.IsNullOrEmpty(diffPath))
            {
                throw new ArgumentException("Value cannot be null or empty.", "diffPath");
            }

            if (string.IsNullOrEmpty(outputFilePath))
            {
                throw new ArgumentException("Value cannot be null or empty.", "outputFilePath");
            }

            try
            {
                _logger.LogDebug(string.Format("Patching {0}", filePath));

                _logger.LogTrace("diffPath = " + diffPath);
                _logger.LogTrace("outputFilePath = " + outputFilePath);

                int status = rs_rdiff_patch(filePath, diffPath, outputFilePath);

                // TODO: Convert status to more meaningful error messages.
                if (status != 0)
                {
                    throw new LibrsyncException(status);
                }

                _logger.LogDebug("File patched.");
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to patch file.", e);
                throw;
            }
        }
    }
}