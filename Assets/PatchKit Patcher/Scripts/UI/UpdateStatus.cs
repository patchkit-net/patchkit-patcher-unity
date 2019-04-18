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
        Patcher.Instance.StateChanged += state =>
        {
            Assert.IsNotNull(value: state);
            Assert.IsNotNull(value: Text);

            if (state.Kind != PatcherStateKind.UpdatingApp)
            {
                Text.text = string.Empty;
                return;
            }

            Assert.IsNotNull(value: state.AppState);

            if (state.AppState.UpdateState.IsConnecting)
            {
                Text.text = string.Empty;
                return;
            }

            string installedBytesText =
                $"{state.AppState.UpdateState.InstalledBytes / 1024.0 / 1024.0:0.0} MB";

            string totalBytesText =
                $"{state.AppState.UpdateState.TotalBytes / 1024.0 / 1024.0:0.0} MB";


            Text.text = $"{installedBytesText} of {totalBytesText}";
        };
    }
}
}