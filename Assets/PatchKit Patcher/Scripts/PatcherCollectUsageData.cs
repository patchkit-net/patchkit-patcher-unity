﻿using System;
using System.ComponentModel;
using UniRx;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.AppData.Local;
    
namespace PatchKit.Unity.Patcher
{
    public class PatcherCollectUsageData
    {
        private const string CollectUsageDataCacheKey = "collect-usageD--data";
        
        private CollectUsageData _collectUsageData;
        
        enum CollectUsageData
        {
            none,
            ask_popup,
            ask_banner
        }

        public PatcherCollectUsageData()
        {
            var patcher = Patcher.Instance;

            Assert.IsNotNull(patcher);

            patcher.AppInfo
                .ObserveOnMainThread()
                .Where(x => !string.IsNullOrEmpty(x.CollectUsageData))
                .Subscribe(SetAndCachCollectUsageData);
        }
        
        private void SetAndCachCollectUsageData(PatchKit.Api.Models.Main.App app)
        {
            string collectUsageData = app.CollectUsageData;

            if (!String.IsNullOrEmpty(collectUsageData))
            {
                GetCache(app.Secret).SetValue(CollectUsageDataCacheKey, collectUsageData);
            }
            else
            {
                collectUsageData = GetCache(app.Secret).GetValue(CollectUsageDataCacheKey, "none");
            }
            
            _collectUsageData = (CollectUsageData) Enum.Parse(typeof(CollectUsageData), collectUsageData, true);
        }

        private ICache GetCache(string appSecret)
        {
            return new UnityCache(appSecret);
        }
        
        public bool IsNone()
        {
            return _collectUsageData == CollectUsageData.none;
        }

        public bool IsPopup()
        {
            return _collectUsageData == CollectUsageData.ask_banner;
        }
        
        public bool IsBanner()
        {
            return _collectUsageData == CollectUsageData.ask_banner;
        }
    }
}