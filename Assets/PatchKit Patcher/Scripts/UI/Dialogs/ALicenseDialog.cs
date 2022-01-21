namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public abstract class ALicenseDialog : Dialog<ALicenseDialog>, ILicenseDialog
    {
        public abstract void SetKey(string key);
        public abstract LicenseDialogResult Display(LicenseDialogMessageType messageType);
    }
}
