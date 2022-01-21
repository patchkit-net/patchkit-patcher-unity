#if UNITY_2018
using System.Collections.Generic;
using PatchKit.Unity.Patcher.AppData.Local;

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

    public void SetInt(string key, int value)
    {
        Dictionary[key] = value.ToString();    
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        if (Dictionary.ContainsKey(key))
        {
            return int.Parse(Dictionary[key]);
        }
        return defaultValue;
    }
}
#endif