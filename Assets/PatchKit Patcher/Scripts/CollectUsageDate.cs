using System;
using System.ComponentModel;
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

        private enum CollectUsageData
        {
            [Description("none")]
            None,
            [Description("ask_popup")]
            AskPopup,
            [Description("ask_banner")]
            AskBanner
        }

        public enum Analytics
        {
            Off,
            On,
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

            _collectUsageData = EnumExtension.GetEnumValueFromDescription<CollectUsageData>(_appInfo.CollectUsageData);

            if (IsNone())
            {
                DebugLogger.Log("Application has usage data collection set to none.");
                return;
            }
            
            int analytics = (int) Analytics.FirstStart;

            UnityDispatcher.Invoke(() => analytics = GetCache(Patcher.Instance.AppSecret).GetInt(CachePatchKitAnalytics, (int) Analytics.FirstStart)).WaitOne();

            if (analytics != (int) Analytics.FirstStart)
            {
                return;
            }
            
            if (IsPopup())
            {
                DebugLogger.Log("Application has usage data collection set to popup.");
                AnalyticsPopup.Instance.Display();
            }
            else
            {
                DebugLogger.Log("Application has usage data collection set to banner.");
                AnalyticsBanner.Instance.Display();
            }
        }

        public bool IsNone()
        {
            return _collectUsageData == CollectUsageData.None;
        }

        public bool IsPopup()
        {
            return _collectUsageData == CollectUsageData.AskPopup;
        }

        public bool IsBanner()
        {
            return _collectUsageData == CollectUsageData.AskBanner;
        }
        
        private ICache GetCache(string appSecret)
        {
            return new UnityCache(appSecret);
        }
    }
}