namespace PatchKit.Unity.Patcher.Licensing
{
    public interface ILicenseObtainer
    {
        bool ShowError { set; }

        ILicense Obtain();
    }
}
