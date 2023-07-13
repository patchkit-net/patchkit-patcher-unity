namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public abstract class BaseLicenseDialog : Dialog<BaseLicenseDialog>, ILicenseDialog
    {
        public abstract void SetKey(string key);
        public abstract LicenseDialogResult Display(LicenseDialogMessageType messageType);
    }
}
