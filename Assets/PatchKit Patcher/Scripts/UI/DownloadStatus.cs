using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.UI.Languages;
using PatchKit.Unity.Utilities;
using UnityEngine;
using UniRx;

namespace PatchKit.Unity.Patcher.UI
{
    public class DownloadStatus : MonoBehaviour
    {
        private ITextTranslator _textTranslator;

        private void Start()
        {
            _textTranslator = GetComponent<ITextTranslator>();
            if (_textTranslator == null)
                _textTranslator = gameObject.AddComponent<TextTranslator>();

            var downloadStatus = Patcher.Instance.UpdaterStatus
                .SelectSwitchOrNull(u => u.LatestActiveOperation)
                .Select(s => s as IReadOnlyDownloadStatus);

            var text = downloadStatus.SelectSwitchOrDefault(status =>
            {
                return status.Bytes.CombineLatest(status.TotalBytes,
                    (bytes, totalBytes) => string.Format("{0:0.0} MB of {1:0.0} MB", bytes / 1024.0 / 1024.0,
                        totalBytes / 1024.0 / 1024.0));
            }, string.Empty);

            text.ObserveOnMainThread().Subscribe(t => _textTranslator.SetText(t)).AddTo(this);
        }
    }
}