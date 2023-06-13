using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Debug;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class SettingsList : MonoBehaviour
    {
        public Toggle ToggleAnalytics;

        private bool _hasBeenSet;
        private string _appSecret;

        private void Awake()
        {
            Patcher patcher = Patcher.Instance;

            Assert.IsNotNull(patcher);

            patcher.Data
                .ObserveOnMainThread()
                .Select(x => x.AppSecret)
                .SkipWhile(string.IsNullOrEmpty)
                .First()
                .Subscribe(UseCachedAnalytics)
                .AddTo(this);
        }

        public bool SetAnalytics
        {
            set
            {
                PatcherStatistics.SetPermitStatistics(value);
                ToggleAnalytics.isOn = value;
                GetCache(_appSecret).SetInt(CollectUsageDate.CachePatchKitAnalytics,
                    value ? (int) CollectUsageDate.Analytics.On : (int) CollectUsageDate.Analytics.Off);
            }
        }

        private void UseCachedAnalytics(string appSecret)
        {
            if (_hasBeenSet)
            {
                return;
            }

            _appSecret = appSecret;

            if (GetCache(_appSecret).GetInt(CollectUsageDate.CachePatchKitAnalytics) ==
                (int) CollectUsageDate.Analytics.On)
            {
                SetAnalytics = true;
            }

            _hasBeenSet = true;
        }

        private ICache GetCache(string appSecret)
        {
            return new UnityCache(appSecret);
        }
    }
}