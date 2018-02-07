using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Utilities;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace PatchKit.Unity.Patcher.UI
{
    public class DownloadStatus : MonoBehaviour
    {
        public Text Text;

        private void Start()
        {
            Patcher.Instance.State.ObserveOnMainThread().Subscribe(state =>
            {
                if (state != PatcherState.UpdatingApp)
                {
                    Text.text = string.Empty;
                }
            }).AddTo(this);

            var status = Patcher.Instance.UpdaterStatus
                .SelectSwitchOrNull(u => u.LatestActiveOperation)
                .Select(s => s as IReadOnlyDownloadStatus);

            var bytes = status.WhereNotNull().Select(s => (IObservable<long>) s.Bytes).Switch();
            var totalBytes = status.WhereNotNull().Select(s => (IObservable<long>) s.TotalBytes).Switch();

            var text = status.CombineLatest(bytes, totalBytes,
                (s, b, t) =>
                    s == null
                        ? string.Empty
                        : string.Format("{0:0.0} MB of {1:0.0} MB", b / 1024.0 / 1024.0, t / 1024.0 / 1024.0));

            text.ObserveOnMainThread().SubscribeToText(Text).AddTo(this);

            Text.text = string.Empty;
        }
    }
}