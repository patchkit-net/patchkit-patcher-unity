namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public abstract class ALicenseDialog : Dialog<ALicenseDialog>, ILicenseDialog
    {
        public virtual void SetKey(string key)
        {
            throw new System.NotImplementedException();
        }

        public virtual LicenseDialogResult Display(LicenseDialogMessageType messageType)
        {
            throw new System.NotImplementedException();
        }
    }
}
