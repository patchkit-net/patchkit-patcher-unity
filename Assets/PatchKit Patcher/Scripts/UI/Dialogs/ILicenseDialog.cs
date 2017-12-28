namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public interface ILicenseDialog
    {
        void SetKey(string key);
        
        LicenseDialogResult Display(LicenseDialogMessageType messageType);
    }
}