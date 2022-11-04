using System;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.UI.NewUI;
using PatchKit.Unity.Utilities;

namespace PatchKit.Unity.Patcher
{
    public class CollectUsageDate
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(CollectUsageDate));

        private CollectUsageData _collectUsageData;
        private readonly Api.Models.Main.App _appInfo;
        
        public const string CachePatchKitAnalytics = "patchkit-analytics";

        enum CollectUsageData
        {
            none,
            ask_popup,
            ask_banner
        }

        public enum Analytics
        {
            Off,
            ON,
            FirstStart
        }

        public CollectUsageDate(Api.Models.Main.App appInfo)
        {
            _appInfo = appInfo;
        }

        public void Execute()
        {
            DebugLogger.Log("Collect usage data...");

            if (AnalyticsPopup.Instance == null)
            {
                DebugLogger.Log("Launcher does not support collecting usage data.");
                return;
            }
            
            if (String.IsNullOrEmpty(_appInfo.CollectUsageData))
            {
                DebugLogger.Log("Application is not using collect usage data.");
                return;
            }

            _collectUsageData = (CollectUsageData) Enum.Parse(typeof(CollectUsageData), _appInfo.CollectUsageData, true);

            if (!IsNone())
            {
                int analytics = (int) Analytics.FirstStart;

                UnityDispatcher.Invoke(() => analytics = GetCache(Patcher.Instance.AppSecret).GetInt(CachePatchKitAnalytics, (int) Analytics.FirstStart)).WaitOne();
                
                if (analytics == (int) Analytics.FirstStart)
                {
                    if (IsPopup())
                    {
                        AnalyticsPopup.Instance.Display();
                    }
                    else
                    {
                        AnalyticsBanner.Instance.Display();
                    }
                }
            }
        }

        public bool IsNone()
        {
            return _collectUsageData == CollectUsageData.none;
        }

        public bool IsPopup()
        {
            return _collectUsageData == CollectUsageData.ask_popup;
        }

        public bool IsBanner()
        {
            return _collectUsageData == CollectUsageData.ask_banner;
        }
        
        private ICache GetCache(string appSecret)
        {
            return new UnityCache(appSecret);
        }
    }
}