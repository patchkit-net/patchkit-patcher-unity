using UnityEngine;
using UnityEngine.UI;
using UniRx;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.AppData.Local;

namespace PatchKit.Unity
{
    public class GameTitle : MonoBehaviour
    {
        public Text Text;

        private bool _hasBeenSet;

        private void Start()
        {
            var patcher = Patcher.Patcher.Instance;

            Assert.IsNotNull(patcher);
            Assert.IsNotNull(Text);

            patcher.Data
                .ObserveOnMainThread()
                .SkipWhile(data => string.IsNullOrEmpty(data.AppSecret))
                .Select(x => x.AppSecret)
                .First()
                .Subscribe(UseCachedText)
                .AddTo(this);

            patcher.AppInfo
                .ObserveOnMainThread()
                .Where(x => !string.IsNullOrEmpty(x.DisplayName))
                .Subscribe(SetAndCacheText)
                .AddTo(this);
        }

        private void UseCachedText(string appSecret)
        {
            if(_hasBeenSet)
            {
                return;
            }

            var cachedDisplayName = GetCache(appSecret)
                .GetValue("app-display-name", null);

            if (string.IsNullOrEmpty(cachedDisplayName))
            {
                return;
            }

            Text.text = cachedDisplayName;
        }

        private void SetAndCacheText(PatchKit.Api.Models.Main.App app)
        {
            string displayName = app.DisplayName;

            GetCache(app.Secret).SetValue("app-display-name", displayName);
            Text.text = displayName;

            _hasBeenSet = true;
        }

        private ICache GetCache(string appSecret)
        {
            return new UnityCache(appSecret);
        }
    }
}