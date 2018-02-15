using UnityEngine;
using UnityEngine.UI;
using UniRx;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity
{
    public class GameTitle : MonoBehaviour
    {
        [SerializeField]
        private Text targetText;

        void Start()
        {
            var patcher = Patcher.Patcher.Instance;

            Assert.IsNotNull(patcher);

            patcher.AppInfo
                .ObserveOnMainThread()
                .Subscribe(OnInfoUpdate)
                .AddTo(this);
        }

        private void OnInfoUpdate(Api.Models.Main.App appInfo)
        {
            if (!string.IsNullOrEmpty(appInfo.DisplayName))
            {
                targetText.text = appInfo.DisplayName;
            }
        }
    }
}