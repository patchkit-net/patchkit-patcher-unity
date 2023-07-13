using PatchKit.Unity.Patcher.AppUpdater.Commands;
using PatchKit.Unity.UI.Languages;
using UniRx;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class DiscFreeSpace : MonoBehaviour
    {
        private ReactiveProperty<long> _freeDiskSpace = new ReactiveProperty<long>();

        public TextTranslator sizeTextTranslator;

        void Start()
        {
            _freeDiskSpace.Value = AvailableDiskSpace.Instance.GetAvailableDiskSpace(Patcher.Instance.Data.Value.AppDataPath);

            IObservable<string> text = _freeDiskSpace.Select(
                freeDiscSpace => string.Format("{0:0.0}MB", freeDiscSpace / 1024.0 / 1024.0));

            text.ObserveOnMainThread().Subscribe(t => sizeTextTranslator.SetText(t)).AddTo(this);
        }
    }
}