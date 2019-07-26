using UnityEngine;
using UnityEngine.UI;
using UniRx;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.AppData.Local;

namespace PatchKit.Unity
{
    public class GameTitle : MonoBehaviour
    {
        private const string TitleCacheKey = "app-display-name";

        public Text Text;

        private bool _hasBeenSet;

        private void Start()
        {
            var patcher = Patcher.Patcher.Instance;

            Assert.IsNotNull(patcher);
            Assert.IsNotNull(Text);

            patcher.Data
                .ObserveOnMainThread()
                .Select(x => x.AppSecret)
                .SkipWhile(string.IsNullOrEmpty)
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
            if (_hasBeenSet)
            {
                return;
            }

            var cachedDisplayName = GetCache(appSecret)
                .GetValue(TitleCacheKey, null);

            if (string.IsNullOrEmpty(cachedDisplayName))
            {
                return;
            }

            Text.text = cachedDisplayName;
        }

        private void SetAndCacheText(PatchKit.Api.Models.Main.App app)
        {
            string displayName = app.DisplayName;

            GetCache(app.Secret).SetValue(TitleCacheKey, displayName);
            Text.text = displayName;

            _hasBeenSet = true;
        }

        private ICache GetCache(string appSecret)
        {
            return new UnityCache(appSecret);
        }
    }
}