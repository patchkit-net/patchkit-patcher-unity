using PatchKit.Unity.UI.Languages;
using PatchKit.Unity.Utilities;

namespace PatchKit.Unity.Patcher
{
    public class PatcherError
    {
        public string Message;
        public object[] Args;

        public PatcherError(string message, params object[] args)
        {
            Message = message;
            Args = args;
        }


        public static PatcherError NoInternetConnection()
        {
            return new PatcherError(LanguageHelper.Tag("please_check_your_internet_connection"));
        }

        public static PatcherError NoPermissions()
        {
            return new PatcherError(LanguageHelper.Tag("please_check_write_permissions_in_applications_directory"));
        }

        public static PatcherError NotEnoughDiskSpace(long additionalBytesRequired)
        {
            return new PatcherError(LanguageHelper.Tag(
                    "not_enough_disk_space_to_install_this_application_additional_[count]_gb_of_disk_space_is_required"),
                additionalBytesRequired / (1024 * 1024 * 1024.0)
            );
        }

        public static PatcherError NonLauncherExecution()
        {
            return new PatcherError(LanguageHelper.Tag("patcher_has_to_be_started_using_the_launcher"));
        }

        public static PatcherError CannotRepairDiskFilesException()
        {
            return new PatcherError(LanguageHelper.Tag(
                "couldnt_validate_local_files_please_try_again_if_the_issue_remains_it_could_mean_a_disk_issue"));
        }

        public static PatcherError Other()
        {
            return new PatcherError(LanguageHelper.Tag(
                "unknown_error_please_try_again_if_the_issue_remains_please_contact_the_support"));
        }
        
        public static PatcherError FilePathTooLong()
        {
            return new PatcherError("Launcher is unable to install this application at this location. Please move your files higher in the directory structure.");
        }

        public string ToString() {
            return string.Format(Message, Args);
        }
    }
}