namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    /// <summary>
    /// Geolocates current machine.
    /// </summary>
    public interface IGeolocateCommand : IAppUpdaterCommand
    {
        /// <summary>
        /// ISO 2 Country code
        /// </summary>
        string CountryCode { get; }
        
        bool HasCountryCode { get; }
    }
}