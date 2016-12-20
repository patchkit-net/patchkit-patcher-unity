namespace PatchKit.Unity.Patcher.Data.Remote
{
    public class RemoteResourceDownloader
    {
        private readonly string _destinationFilePath;
        private readonly RemoteResource _resource;

        public RemoteResourceDownloader(string destinationFilePath, RemoteResource resource)
        {
            _destinationFilePath = destinationFilePath;
            _resource = resource;
        }

        public void Download()
        {
        }
    }
}