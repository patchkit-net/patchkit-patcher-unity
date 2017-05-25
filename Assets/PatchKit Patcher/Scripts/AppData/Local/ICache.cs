namespace PatchKit.Unity.Patcher.AppData.Local
{
    public interface ICache
    {
        void SetValue(string key, string value);
        string GetValue(string key, string defaultValue = null);
    }
}
