using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class OpenURL : MonoBehaviour
    {
        public string URL;
        
        public void OpenWebpage()
        {
            Application.OpenURL(URL);
        }
    }
}
