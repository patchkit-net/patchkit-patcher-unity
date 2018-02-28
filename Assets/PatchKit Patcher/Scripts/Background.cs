using UnityEngine;
using UnityEngine.UI;
using UniRx;
using PatchKit.Logging;
using PatchKit.Unity.Utilities;
using PatchKit.Unity.Patcher;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders;
using PatchKit.Api.Models.Main;
using System;
using System.IO;
using System.Collections;

public class Background : MonoBehaviour
{
    public struct PatcherBannerData
    {
        public string imageUrl;
        public PatcherBannerImageDimensions dimensions;
        public string modificationDate;
    }

    public struct Data
    {
        public PatcherBannerData bannerData;
        public string bannerFilePath;
    }

    private ICache _cache = new UnityCache();

    private const string CachedBannerUrlKey = "cached-banner-url-key";
    private const string CachedBannerPathKey = "cached-banner-path-key";
    private const string CachedBannerModificationDateKey = "cached-banner-modif-date-key";

    private const string BannerImageFilename = "banner";

    private const string AnimationLoadingParameter = "isLoading";
    private const string AnimationSwitchTrigger = "switch";

    public string CachedBannerPath
    {
        get 
        {
            return _cache.GetValue(CachedBannerPathKey);
        }
        private set
        {
            _cache.SetValue(CachedBannerPathKey, value);
        }
    }

    public string CachedBannerImageUrl
    {
        get
        {
            return _cache.GetValue(CachedBannerUrlKey);
        }

        private set 
        {
            _cache.SetValue(CachedBannerUrlKey, value);
        }
    }

    public string CachedBannerModificationDate
    {
        get
        {
            return _cache.GetValue(CachedBannerModificationDateKey);
        }

        private set
        {
            _cache.SetValue(CachedBannerModificationDateKey, value);
        }
    }

    public Image newImage;
    public Image oldImage;

    public Animator mainAnimator;

    private PatchKit.Logging.ILogger _logger;

    private void Start()
    {
        _logger = PatcherLogManager.DefaultLogger;

        var patcher = Patcher.Instance;

        if (IsCachedBannerAvailable())
        {
            _logger.LogDebug("A cached banner image is available.");
            LoadCachedBanner(CachedBannerPath, oldImage);
        }

        Assert.IsNotNull(patcher);
        Assert.IsNotNull(mainAnimator);
        Assert.IsNotNull(newImage);
        Assert.IsNotNull(oldImage);

        var patcherData = patcher.Data
            .Select(data => data.AppDataPath)
            .SkipWhile(val => string.IsNullOrEmpty(val))
            .Select(val => Path.Combine(val, BannerImageFilename));

        var appInfo = patcher.AppInfo
            .Select(info => new PatcherBannerData{
                imageUrl = info.PatcherBannerImage,
                dimensions = info.PatcherBannerImageDimensions,
                modificationDate = info.PatcherBannerImageUpdatedAt
                });

        patcherData
            .CombineLatest(appInfo, (lhs, rhs) => new Data{bannerData = rhs, bannerFilePath = lhs})
            .ObserveOnMainThread()
            .Subscribe(OnBannerDataUpdate);
    }

    private void OnBannerDataUpdate(Data data)
    {
        _logger.LogDebug("On patcher data update.");
        var bannerData = data.bannerData;

        if (string.IsNullOrEmpty(bannerData.imageUrl))
        {
            _logger.LogDebug("No banner is available.");
            return;
        }

        if (IsCachedBannerAvailable() && IsCachedBannerSameAsRemote(bannerData))
        {
            _logger.LogDebug("The cached banner is the same as remote.");
            return;
        }

        AquireRemoteBanner(data);
    }

    private bool IsCachedBannerAvailable()
    {
        return !string.IsNullOrEmpty(CachedBannerImageUrl);
    }

    private bool IsCachedBannerSameAsRemote(PatcherBannerData bannerData)
    {
        var cachedModificationDate = CachedBannerModificationDate;

        return bannerData.modificationDate == cachedModificationDate;
    }

    private void AquireRemoteBanner(Data data)
    {
        _logger.LogDebug(string.Format("Aquiring the remote banner image from {0}", data.bannerData.imageUrl));
        var coroutine = Threading.StartThreadCoroutine(() => {
            CancellationTokenSource source = new CancellationTokenSource();

            var downloader = new HttpDownloader(data.bannerFilePath, new string[]{data.bannerData.imageUrl});

            try
            {
                UnityDispatcher.Invoke(() => {
                    mainAnimator.SetBool(AnimationLoadingParameter, true);
                });

                downloader.Download(source.Token);
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }, (bool result) => {
            if (result)
            {
                CachedBannerImageUrl = data.bannerData.imageUrl;
                CachedBannerPath = data.bannerFilePath;
                CachedBannerModificationDate = data.bannerData.modificationDate;

                mainAnimator.SetBool(AnimationLoadingParameter, false);
                mainAnimator.SetTrigger(AnimationSwitchTrigger);

                LoadCachedBanner(data.bannerFilePath, newImage);
            }
        });

        StartCoroutine(coroutine);
    }

    private void LoadCachedBanner(string filepath, Image target)
    {
        Texture2D texture = new Texture2D(0, 0);

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
            _logger.LogDebug(string.Format("Loading the banner image from {0}", filepath));
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
