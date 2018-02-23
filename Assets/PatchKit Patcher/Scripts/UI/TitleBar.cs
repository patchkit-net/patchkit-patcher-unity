using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Patching.Unity.UI
{
    public class TitleBar : MonoBehaviour
    {
        public Button CloseButton;

        public Button MinimizeButton;

        private void Update()
        {
            bool isEnabled = Application.platform == RuntimePlatform.WindowsPlayer;

            CloseButton.gameObject.SetActive(isEnabled);
            MinimizeButton.gameObject.SetActive(isEnabled);
        }
    }
}