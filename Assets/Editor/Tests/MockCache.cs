using System.Collections.Generic;
using PatchKit.Apps.Updating.AppData.Local;

class MockCache : ICache
{
    public readonly Dictionary<string, string> Dictionary = new Dictionary<string, string>();

    public void SetValue(string key, string value)
    {
        Dictionary[key] = value;
    }

    public string GetValue(string key, string defaultValue = null)
    {
        if (Dictionary.ContainsKey(key))
        {
            return Dictionary[key];
        }
        return defaultValue;
    }

    public void DeleteKey(string key)
    {
        Dictionary.Remove(key);
    }

    public bool HasKey(string key)
    {
        return Dictionary.ContainsKey(key);
    }
}
