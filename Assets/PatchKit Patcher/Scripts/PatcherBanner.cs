using System.IO;
using PatchKit.Api.Models;
using PatchKit.Apps.Updating;
using PatchKit.Logging;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using ILogger = PatchKit.Logging.ILogger;

namespace PatchKit.Patching.Unity
{
    public class PatcherBanner : MonoBehaviour
    {
        private struct PatcherBannerData
        {
            public string ImageUrl;
            public PatcherBannerImageDimensions Dimensions;
            public string ModificationDate;
        }

        private struct Data
        {
            public PatcherBannerData BannerData;
            public string BannerFilePath;
        }

        private UnityCache _cache;

        private const string CachedBannerPathKey = "cached-banner-path-key";
        private const string CachedBannerModificationDateKey = "cached-banner-modif-date-key";

        private const string BannerImageFilename = "banner";

        private const string AnimationLoadingParameter = "isLoading";
        private const string AnimationSwitchTrigger = "switch";

        private string CachedBannerPath
        {
            get 
            {
                return _cache.GetValue(CachedBannerPathKey);
            }
            set
            {
                _cache.SetValue(CachedBannerPathKey, value);
            }
        }

        private string CachedBannerModificationDate
        {
            get
            {
                return _cache.GetValue(CachedBannerModificationDateKey);
            }

            set
            {
                _cache.SetValue(CachedBannerModificationDateKey, value);
            }
        }

        public Image NewImage;
        public Image OldImage;

        public Sprite DefaultBackground;

        public Animator MainAnimator;

        private ILogger _logger;

        private void Start()
        {
            _logger = DependencyResolver.Resolve<ILogger>();

            var patcher = Patcher.Instance;

            patcher.Data
                .SkipWhile(data => string.IsNullOrEmpty(data.AppSecret))
                .First()
                .ObserveOnMainThread()
                .Subscribe(Initialize);
        }

        private void Initialize(PatcherData data)
        {
            _cache = new UnityCache();
    
            if (IsCachedBannerAvailable())
            {
                _logger.LogDebug($"A cached banner image is available at {CachedBannerPath}");
                LoadBannerImage(CachedBannerPath, OldImage);
            }
    
            var patcher = Patcher.Instance;
    
            var appInfo = patcher.AppInfo
                .SkipWhile(info => info.Id == default(int))
                .Select(info => new Data{ 
                    BannerData = new PatcherBannerData{
                        ImageUrl = info.PatcherBannerImage,
                        Dimensions = info.PatcherBannerImageDimensions,
                        ModificationDate = info.PatcherBannerImageUpdatedAt
                    },
                    BannerFilePath = Path.Combine(data.AppDataPath, BannerImageFilename) 
                })
                .ObserveOnMainThread()
                .Subscribe(OnBannerDataUpdate);
        }

        private void OnBannerDataUpdate(Data data)
        {
            _logger.LogDebug("On patcher data update.");
            var bannerData = data.BannerData;

            if (IsNewBannerAvailable(data))
            {
                AquireRemoteBanner(data);
            }
            else if (HasBannerBeenRemoved(data))
            {
                _logger.LogDebug("Banner image has been removed.");
                ClearCachedBanner();
                CachedBannerModificationDate = bannerData.ModificationDate;

                SwitchToDefault();
            }
            else if (IsCachedBannerSameAsRemote(data.BannerData))
            {
                _logger.LogDebug("Nothing has changed.");
            }
            else
            {
                _logger.LogDebug("Banner has never been set.");
                SwitchToDefault();
            }
        }

        private void SwitchToDefault()
        {
            _logger.LogDebug("Switching to default background");
            NewImage.sprite = DefaultBackground;
            MainAnimator.SetTrigger(AnimationSwitchTrigger);
        }

        private bool IsNewBannerAvailable(Data data)
        {
            return !string.IsNullOrEmpty(data.BannerData.ImageUrl) 
                   && !IsCachedBannerSameAsRemote(data.BannerData);
        }

        private bool HasBannerBeenRemoved(Data data)
        {
            return string.IsNullOrEmpty(data.BannerData.ImageUrl) 
                   && !string.IsNullOrEmpty(data.BannerData.ModificationDate)
                   && IsCachedBannerAvailable();
        }

        private bool IsCachedBannerAvailable()
        {
            return !string.IsNullOrEmpty(CachedBannerPath) 
                   && File.Exists(CachedBannerPath);
        }

        private bool IsCachedBannerSameAsRemote(PatcherBannerData bannerData)
        {
            var cachedModificationDate = CachedBannerModificationDate;

            return bannerData.ModificationDate == cachedModificationDate;
        }

        private void ClearCachedBanner()
        {
            _logger.LogDebug($"Clearning the cached banner at {CachedBannerPath}");
            if (!File.Exists(CachedBannerPath))
            {
                _logger.LogError("The cached banner doesn't exist.");
                return;
            }

            File.Delete(CachedBannerPath);
            CachedBannerPath = "";
        }

        private void AquireRemoteBanner(Data data)
        {
            /*_logger.LogDebug($"Aquiring the remote banner image from {data.BannerData.ImageUrl}");
            var coroutine = UnityThreading.StartThreadCoroutine(() => {
                var source = new CancellationTokenSource();

                var downloader = new HttpDownloader(data.BannerFilePath, new[]{data.BannerData.ImageUrl});

                try
                {
                    UnityDispatcher.Invoke(() => {
                        MainAnimator.SetBool(AnimationLoadingParameter, true);
                    });

                    downloader.Download(source.Token);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }

            }, result => {
                if (result)
                {
                    CachedBannerPath = data.BannerFilePath;
                    CachedBannerModificationDate = data.BannerData.ModificationDate;

                    MainAnimator.SetBool(AnimationLoadingParameter, false);
                    MainAnimator.SetTrigger(AnimationSwitchTrigger);

                    LoadBannerImage(data.BannerFilePath, NewImage);
                }
            });

            StartCoroutine(coroutine);
            */
        }

        private void LoadBannerImage(string filepath, Image target)
        {
            var texture = new Texture2D(0, 0);

            if (string.IsNullOrEmpty(filepath))
            {
                filepath = CachedBannerPath;

                if (string.IsNullOrEmpty(filepath))
                {
                    _logger.LogWarning("Banner file path was null or empty.");
                    return;
                }
            }

            if (File.Exists(filepath))
            {
                _logger.LogDebug($"Loading the banner image from {filepath}");
                var fileBytes = File.ReadAllBytes(filepath);
            
                if (!texture.LoadImage(fileBytes))
                {
                    _logger.LogError("Failed to load the banner image.");
                    return;
                }

                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

                target.sprite = sprite;
            }
            else
            {
                _logger.LogWarning("The cached banner image doesn't exist.");
            }
        }
    }
}
