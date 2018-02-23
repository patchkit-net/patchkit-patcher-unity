using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Patching.Unity.UI
{
    public class WarningText : MonoBehaviour
    {
        public Text Text;

        private void Start()
        {
            Patcher.Instance.Warning.ObserveOnMainThread().Subscribe(warning =>
            {
                Text.text = warning;
            }).AddTo(this);
        }
    }
}