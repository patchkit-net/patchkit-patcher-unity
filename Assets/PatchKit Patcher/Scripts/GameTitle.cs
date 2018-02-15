using UnityEngine;
using UnityEngine.UI;
using UniRx;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity
{
    public class GameTitle : MonoBehaviour
    {
        public Text Text;

        private void Start()
        {
            var patcher = Patcher.Patcher.Instance;

            Assert.IsNotNull(patcher);

            patcher.AppInfo
                .ObserveOnMainThread()
                .Select(app => app.DisplayName)
                .Where(s => !string.IsNullOrEmpty(s))
                .SubscribeToText(Text)
                .AddTo(this);
        }
    }
}