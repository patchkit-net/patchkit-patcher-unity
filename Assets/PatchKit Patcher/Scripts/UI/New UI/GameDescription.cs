using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Debug;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class GameDescription : MonoBehaviour
    {
        private const string DescriptionCacheKey = "app-display-description";

        public Text Text;

        private bool _hasBeenSet;

        private void Start()
        {
            Patcher patcher = Patcher.Instance;

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
                .Where(a => !string.IsNullOrEmpty(a.Secret))
                .Subscribe(SetAndCacheText)
                .AddTo(this);
        }

        private void UseCachedText(string appSecret)
        {
            if (_hasBeenSet)
            {
                return;
            }

            string cachedDisplayDescription = GetCache(appSecret)
                .GetValue(DescriptionCacheKey, null);

            if (string.IsNullOrEmpty(cachedDisplayDescription))
            {
                return;
            }

            Text.text = cachedDisplayDescription;
        }

        private void SetAndCacheText(PatchKit.Api.Models.Main.App app)
        {
            string displayDescription = app.Description;

            GetCache(app.Secret).SetValue(DescriptionCacheKey, displayDescription);
            Text.text = displayDescription;

            _hasBeenSet = true;
        }

        private ICache GetCache(string appSecret)
        {
            return new UnityCache(appSecret);
        }
    }
}