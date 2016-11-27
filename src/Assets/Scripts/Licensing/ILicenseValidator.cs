namespace PatchKit.Unity.Patcher.Licensing
{
    public interface ILicenseValidator
    {
        /// <summary>
        /// Validates the specified license. Returns license validation code.
        /// If license is not valid, <c>null</c> is returned.
        /// </summary>
        /// <param name="license">The license to validate.</param>
        string Validate(ILicense license);
    }
}
