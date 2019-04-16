namespace Legacy.UI
{
public interface ILicenseDialog
{
    void SetKey(string key);

    LicenseDialogResult Display(LicenseDialogMessageType messageType);
}
}