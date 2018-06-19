using UnityEngine;
using UnityEngine.UI;
using PatchKit.Logging;
using PatchKit.Api.Models;
using System;
using System.IO;
using System.Threading;
using PatchKit.Apps.Updating;
using PatchKit.Apps.Updating.AppData.Local;
using PatchKit.Apps.Updating.AppData.Remote.Downloaders;
using PatchKit.Apps.Updating.Debug;
using PatchKit.Patching.Unity;
using UniRx;
using ILogger = PatchKit.Logging.ILogger;

public class Background : MonoBehaviour
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

    private readonly ICache _cache = new UnityCache();

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

    public Animator MainAnimator;

    private ILogger _logger;

    private void Start()
    {
        _logger = DependencyResolver.Resolve<ILogger>();

        var patcher = Patcher.Instance;

        if (IsCachedBannerAvailable())
        {
            _logger.LogDebug(string.Format("A cached banner image is available at {0}", CachedBannerPath));
            LoadBannerImage(CachedBannerPath, OldImage);
        }

        Assert.IsNotNull(patcher);
        Assert.IsNotNull(MainAnimator);
        Assert.IsNotNull(NewImage);
        Assert.IsNotNull(OldImage);

        var patcherData = patcher.Data
            .Select(data => data.AppDataPath)
            .SkipWhile(string.IsNullOrEmpty)
            .Select(val => Path.Combine(val, BannerImageFilename));

        var appInfo = patcher.AppInfo
            .Select(info => new PatcherBannerData{
                ImageUrl = info.PatcherBannerImage,
                Dimensions = info.PatcherBannerImageDimensions,
                ModificationDate = info.PatcherBannerImageUpdatedAt
                });

        patcherData
            .CombineLatest(appInfo, (lhs, rhs) => new Data{BannerData = rhs, BannerFilePath = lhs})
            .ObserveOnMainThread()
            .Subscribe(OnBannerDataUpdate);
    }

    private void OnBannerDataUpdate(Data data)
    {
        _logger.LogDebug("On patcher data update.");
        var bannerData = data.BannerData;

        if (string.IsNullOrEmpty(bannerData.ImageUrl))
        {
            _logger.LogDebug("No banner is available.");

            MainAnimator.SetTrigger(AnimationSwitchTrigger);
            
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
        return !string.IsNullOrEmpty(CachedBannerPath) && File.Exists(CachedBannerPath);
    }

    private bool IsCachedBannerSameAsRemote(PatcherBannerData bannerData)
    {
        var cachedModificationDate = CachedBannerModificationDate;

        return bannerData.ModificationDate == cachedModificationDate;
    }

    private void AquireRemoteBanner(Data data)
    {
        _logger.LogDebug(string.Format("Aquiring the remote banner image from {0}", data.BannerData.ImageUrl));
        var coroutine = UnityThreading.StartThreadCoroutine(() => {
            CancellationTokenSource source = new CancellationTokenSource();

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
    }

    private void LoadBannerImage(string filepath, Image target)
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
