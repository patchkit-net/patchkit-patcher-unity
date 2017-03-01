namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public interface ILicenseDialog
    {
        LicenseDialogResult Display(LicenseDialogMessageType messageType);
    }
}