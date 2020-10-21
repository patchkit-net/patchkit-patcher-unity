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
            return new PatcherError(PatcherLanguages.GetTraduction("no_internet_connection"));
        }

        public static PatcherError NoPermissions() {
            return new PatcherError(PatcherLanguages.GetTraduction("no_permissions"));
        }

        public static PatcherError NotEnoughDiskSpace(long additionalBytesRequired) {
            return new PatcherError(PatcherLanguages.GetTraduction("not_enough_disk_space"),
                additionalBytesRequired / (1024 * 1024 * 1024.0)
            );
        }

        public static PatcherError NonLauncherExecution() {
            return new PatcherError(PatcherLanguages.GetTraduction("non_launcher_execution"));
        }

        public static PatcherError CannotRepairDiskFilesException()
        {
            return new PatcherError(PatcherLanguages.GetTraduction("other_error_repiar"));
        }

        public static PatcherError Other() {
            return new PatcherError(PatcherLanguages.GetTraduction("other_error"));
        }


        public string ToString() {
            return string.Format(_message, _args);
        }
    }
}