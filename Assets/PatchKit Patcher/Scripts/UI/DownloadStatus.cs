using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UI
{
public class DownloadStatus : MonoBehaviour
{
    public Text Text;

    private void Awake()
    {
        Patcher.Instance.StateChanged += state =>
        {
            Assert.IsNotNull(value: state);
            Assert.IsNotNull(value: Text);

            if (state.Kind != PatcherStateKind.UpdatingApp ||
                state.UpdateAppState.IsConnecting)
            {
                Text.text = string.Empty;
                return;
            }

            string installedBytesText =
                $"{state.UpdateAppState.InstalledBytes / 1024.0 / 1024.0:0.0} MB";

            string totalBytesText =
                $"{state.UpdateAppState.TotalBytes / 1024.0 / 1024.0:0.0} MB";


            Text.text = $"{installedBytesText} of {totalBytesText}";
        };
    }
}
}