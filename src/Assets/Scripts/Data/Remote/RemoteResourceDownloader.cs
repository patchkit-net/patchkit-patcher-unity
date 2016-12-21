namespace PatchKit.Unity.Patcher.Data.Remote
{
    public class RemoteResourceDownloader
    {
        private readonly string _destinationFilePath;

        private readonly RemoteResource _resource;

        public event DownloadProgressChangedHandler ProgressChanged;

        public RemoteResourceDownloader(string destinationFilePath, RemoteResource resource)
        {
            _destinationFilePath = destinationFilePath;
            _resource = resource;
        }

        public void Download()
        {
        }

        protected virtual void OnProgressChanged(long downloadedbytes, long totalbytes)
        {
            var handler = ProgressChanged;
            if (handler != null) handler(downloadedbytes, totalbytes);
        }
    }
}