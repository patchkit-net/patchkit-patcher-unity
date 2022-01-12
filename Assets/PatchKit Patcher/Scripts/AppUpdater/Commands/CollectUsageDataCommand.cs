using System;
using JetBrains.Annotations;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.UI.NewUI;
using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class CollectUsageDataCommand : BaseAppUpdaterCommand, ICollectUsageDataCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(CollectUsageDataCommand));

        private CollectUsageData _collectUsageData;
        [NotNull] private readonly IRemoteMetaData _remoteMetaData;
        private OperationStatus _status;

        enum CollectUsageData
        {
            none,
            ask_popup,
            ask_banner
        }

        public CollectUsageDataCommand([NotNull] IRemoteMetaData remoteMetaData)
        {
            if (remoteMetaData == null) throw new ArgumentNullException("remoteMetaData");

            _remoteMetaData = remoteMetaData;
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Collect usage data...");

            base.Execute(cancellationToken);

            var appInfo = _remoteMetaData.GetAppInfo(true, cancellationToken);

            if (String.IsNullOrEmpty(appInfo.CollectUsageData))
            {
                DebugLogger.Log("Application is not using collect usage data.");
                return;
            }

            _collectUsageData = (CollectUsageData) Enum.Parse(typeof(CollectUsageData), appInfo.CollectUsageData, true);

            if (!IsNone())
            {
                int analytics = 2;

                UnityDispatcher.Invoke(() => analytics = PlayerPrefs.GetInt("analytics", 2)).WaitOne();
                
                if (analytics == 2)
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

        public override void Prepare(UpdaterStatus status, CancellationToken cancellationToken)
        {
            base.Prepare(status, cancellationToken);
            
            if (status == null) throw new ArgumentNullException("status");
            _status = new OperationStatus
            {
                Weight = {Value = 0.00001},
                Description = {Value = "Collect usage data..."}
            };
            status.RegisterOperation(_status);
        }
    }
}