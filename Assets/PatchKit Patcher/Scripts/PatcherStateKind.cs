public enum PatcherStateKind
{
    Initializing,
    Idle,
    AskingForLicenseKey,
    UpdatingApp,
    StartingApp,
    DisplayingNoLauncherError,
    DisplayingMultipleInstancesError,
    DisplyingOutOfDiskSpaceError,
    DisplayingInternalError,
    DisplayingUnauthorizedAccessError,
    Quitting
}