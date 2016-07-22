using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    public class Status : MonoBehaviour
    {
        public Text Text;

        private void Update()
        {
            var status = PatcherApplication.Instance.Patcher.Status;

            if (status.State == PatcherState.None)
            {
                Text.text = string.Empty;
            }
            else if (status.State == PatcherState.Failed)
            {
                Text.text = "Patching has failed!";
            }
            else if (status.State == PatcherState.Cancelled)
            {
                Text.text = "Patching has been cancelled.";
            }
            else if (status.State == PatcherState.Patching)
            {
                Text.text = "Patching...";
            }
            else if (status.State == PatcherState.Succeed)
            {
                Text.text = "Ready!";
            }
        }
    }
}
