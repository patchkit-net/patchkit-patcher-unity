using System.IO;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public class DownloadedResourceValidator
    {
        public void Validate(string downloadedResourcePath, RemoteResource resource)
        {
            if (!File.Exists(downloadedResourcePath))
            {
                throw new DownloadedResourceValidationException(string.Format("Downloaded resource doesn't exist <{0}>.", downloadedResourcePath));
            }

            var fileInfo = new FileInfo(downloadedResourcePath);

            if (fileInfo.Length != resource.Size)
            {
                throw new DownloadedResourceValidationException(string.Format("Downloaded resource size is not correct. Should be {0} but is {1}.",
                    resource.Size, fileInfo.Length));
            }

            var hashCode = HashCalculator.ComputeFileHash(downloadedResourcePath);

            if (!string.IsNullOrEmpty(resource.HashCode) && hashCode != resource.HashCode)
            {
                throw new DownloadedResourceValidationException(string.Format("Downloaded resource hash is not correct. Should be {0} but is {1}.",
                    resource.HashCode, hashCode));
            }
        }
    }
}