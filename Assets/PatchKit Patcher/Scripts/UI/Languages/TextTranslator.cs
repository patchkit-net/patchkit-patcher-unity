using UnityEngine;
using UnityEngine.UI;

//text component is required in this gameobject
[RequireComponent (typeof(Text))]
public class TextTranslator : MonoBehaviour
{
    [Tooltip ("enter one of the keys that you specify in your (txt) file for all languages.\n\n# for example: [HOME=home]\n# the key here is [HOME]")]
    [Header ("Enter your word key here.")]
    public string Key;

    void Start ()
    {
        GetComponent <Text> ().text = PatcherLanguages.GetTraduction (Key);
    }
    
    public void ChangeText(string key)
    {
        Key = key;
        GetComponent <Text> ().text = PatcherLanguages.GetTraduction (key);
    }
}