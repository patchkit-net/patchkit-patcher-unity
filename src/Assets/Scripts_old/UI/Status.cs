using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    public class Status : MonoBehaviour
    {
        public Text Text;

        private void Start()
        {
            PatcherApplication.Instance.Patcher.OnStateChanged += state =>
            {
                if (state == PatcherState.None)
                {
                    Text.text = string.Empty;
                }
                else if (state == PatcherState.Error)
                {
                    Text.text = "Patching has failed!";
                }
                else if (state == PatcherState.Cancelled)
                {
                    Text.text = "Patching has been cancelled.";
                }
                else if (state == PatcherState.UnauthorizedAccess)
                {
                    Text.text = "Unauthorized access.";
                }
                else if (state == PatcherState.Processing)
                {
                    Text.text = "Initializing...";
                }
                else if (state == PatcherState.Processing)
                {
                    Text.text = "Patching...";
                }
                else if (state == PatcherState.Success)
                {
                    Text.text = "Ready!";
                }
            };
        }
    }
}