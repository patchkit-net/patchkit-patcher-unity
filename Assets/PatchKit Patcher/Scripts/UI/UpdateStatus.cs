using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UI
{
public class UpdateStatus : MonoBehaviour
{
    public Text Text;

    private void Awake()
    {
        Patcher.Instance.OnStateChanged += state =>
        {
            Assert.IsNotNull(value: Text);

            if (state.App == null ||
                state.App.Value.UpdateTask == null)
            {
                Text.text = string.Empty;
                return;
            }

            if (state.App.Value.UpdateTask.Value.IsConnecting)
            {
                Text.text = string.Empty;
                return;
            }

            string installedBytesText =
                $"{state.App.Value.UpdateTask.Value.InstalledBytes / 1024.0 / 1024.0:0.0} MB";

            string totalBytesText =
                $"{state.App.Value.UpdateTask.Value.TotalBytes / 1024.0 / 1024.0:0.0} MB";


            Text.text = $"{installedBytesText} of {totalBytesText}";
        };
    }
}
}