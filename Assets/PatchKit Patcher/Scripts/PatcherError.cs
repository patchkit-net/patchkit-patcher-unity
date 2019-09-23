namespace PatchKit.Unity.Patcher
{
    public class PatcherError
    {
        private string _code;
        private string _message;
        private object[] _args;

        public PatcherError(string code, string message, params object[] args) {
            _code = code;
            _message = message;
            _args = args;
        }

        public string GetCode()
        {
            return _code;
        }


        public static PatcherError NoInternetConnection() {
            return new PatcherError(
                "no_internet_connection",
                "Please check your internet connection.");
        }

        public static PatcherError NoPermissions() {
            return new PatcherError(
                "no_permissions",
                "Please check write permissions in application's directory.");
        }

        public static PatcherError NotEnoughDiskSpace(long additionalBytesRequired) {
            return new PatcherError(
                "not_enough_disk_space",
                "Not enough disk space to install this application. Additional {0:0.00} GB of disk space is required.",
                additionalBytesRequired / (1024 * 1024 * 1024.0)
            );
        }

        public static PatcherError NonLauncherExecution() {
            return new PatcherError(
                "non_launcher_execution",
                "Patcher has to be started using the launcher.");
        }

        public static PatcherError Other() {
            return new PatcherError(
                "other",
                "Unknown error, please try again. If the issue remains, please contact the support.");
        }


        public string ToString() {
            return string.Format(_message, _args);
        }
    }
}