namespace PatchKit.Unity.Patcher
{
    public class PatcherError
    {
        private string _message;
        private object[] _args;

        public PatcherError(string message, params object[] args) {
            _message = message;
            _args = args;
        }


        public static PatcherError NoInternetConnection() {
            return new PatcherError("Please check your internet connection.");
        }

        public static PatcherError NoPermissions() {
            return new PatcherError("Please check write permissions in application's directory.");
        }

        public static PatcherError NotEnoughDiskSpace(long additionalBytesRequired) {
            return new PatcherError(
                "Not enough disk space to install this application. Additional {0:0.00} GB of disk space is required.",
                additionalBytesRequired / (1024 * 1024 * 1024.0)
            );
        }

        public static PatcherError NonLauncherExecution() {
            return new PatcherError("Patcher has to be started using the launcher.");
        }

        public static PatcherError CannotRepairDiskFilesException()
        {
            return new PatcherError("Couldn't validate local files, please try again. If the issue remains, it could mean a disk issue.");
        }

        public static PatcherError Other() {
            return new PatcherError("Unknown error, please try again. If the issue remains, please contact the support.");
        }


        public string ToString() {
            return string.Format(_message, _args);
        }
    }
}