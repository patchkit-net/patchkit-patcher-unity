using PatchKit.Apps.Updating.Debug;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Patching.Unity.UI
{
    public class GameTitle : MonoBehaviour
    {
        public Text Text;

        private void Start()
        {
            var patcher = Patcher.Instance;

            Assert.IsNotNull(patcher);
            Assert.IsNotNull(Text);

            patcher.AppInfo
                .ObserveOnMainThread()
                .Select(app => app.DisplayName)
                .Where(s => !string.IsNullOrEmpty(s))
                .SubscribeToText(Text)
                .AddTo(this);
        }
    }
}