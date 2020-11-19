using PatchKit.Unity.UI.Languages;

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
            return new PatcherError(PatcherLanguages.GetTranslation("please_check_your_internet_connection"));
        }

        public static PatcherError NoPermissions() {
            return new PatcherError(PatcherLanguages.GetTranslation("please_check_write_permissions_in_applications_directory"));
        }

        public static PatcherError NotEnoughDiskSpace(long additionalBytesRequired) {
            return new PatcherError(PatcherLanguages.GetTranslation("not_enough_disk_space_to_install_this_application_additional_[count]_gb_of_disk_space_is_required"),
                additionalBytesRequired / (1024 * 1024 * 1024.0)
            );
        }

        public static PatcherError NonLauncherExecution() {
            return new PatcherError(PatcherLanguages.GetTranslation("patcher_has_to_be_started_using_the_launcher"));
        }

        public static PatcherError CannotRepairDiskFilesException()
        {
            return new PatcherError(PatcherLanguages.GetTranslation("couldnt_validate_local_files_please_try_again_if_the_issue_remains_it_could_mean_a_disk_issue"));
        }

        public static PatcherError Other() {
            return new PatcherError(PatcherLanguages.GetTranslation("unknown_error_please_try_again_if_the_issue_remains_please_contact_the_support"));
        }


        public string ToString() {
            return string.Format(_message, _args);
        }
    }
}