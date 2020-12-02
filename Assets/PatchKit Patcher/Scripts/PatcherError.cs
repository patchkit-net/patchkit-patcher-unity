using PatchKit.Unity.UI.Languages;

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
            return new PatcherError(PatcherLanguages.OpenTag + "please_check_your_internet_connection" +
                                    PatcherLanguages.CloseTag);
        }

        public static PatcherError NoPermissions()
        {
            return new PatcherError(PatcherLanguages.OpenTag +
                                    "please_check_write_permissions_in_applications_directory" +
                                    PatcherLanguages.CloseTag);
        }

        public static PatcherError NotEnoughDiskSpace(long additionalBytesRequired)
        {
            return new PatcherError(
                PatcherLanguages.OpenTag +
                "not_enough_disk_space_to_install_this_application_additional_[count]_gb_of_disk_space_is_required" +
                PatcherLanguages.CloseTag,
                additionalBytesRequired / (1024 * 1024 * 1024.0)
            );
        }

        public static PatcherError NonLauncherExecution()
        {
            return new PatcherError(PatcherLanguages.OpenTag + "patcher_has_to_be_started_using_the_launcher" +
                                    PatcherLanguages.CloseTag);
        }

        public static PatcherError CannotRepairDiskFilesException()
        {
            return new PatcherError(
                PatcherLanguages.OpenTag +
                "couldnt_validate_local_files_please_try_again_if_the_issue_remains_it_could_mean_a_disk_issue" +
                PatcherLanguages.CloseTag);
        }

        public static PatcherError Other()
        {
            return new PatcherError(PatcherLanguages.OpenTag +
                                    "unknown_error_please_try_again_if_the_issue_remains_please_contact_the_support" +
                                    PatcherLanguages.CloseTag);
        }

        public string ToString()
        {
            return string.Format(Message, Args);
        }
    }
}