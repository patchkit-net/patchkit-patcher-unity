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
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppData.FileSystem;
using CancellationToken = PatchKit.Unity.Patcher.Cancellation.CancellationToken;

public class Background : MonoBehaviour
{
    public struct PatcherBannerData
    {
        public string ImageUrl;
        public PatcherBannerImageDimensions Dimensions;
        public string ModificationDate;
    }

    public struct Data
    {
        public PatcherBannerData BannerData;
        public string BannerFilePath;
    }

    private ICache _cache = null;

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

    public Image NewImage;
    public Image OldImage;

    public Sprite DefaultBackground;

    public Animator MainAnimator;

    private PatchKit.Logging.ILogger _logger;

    private void Start()
    {
        _logger = PatcherLogManager.DefaultLogger;

        var patcher = Patcher.Instance;

        Assert.IsNotNull(patcher);
        Assert.IsNotNull(MainAnimator);
        Assert.IsNotNull(NewImage);
        Assert.IsNotNull(OldImage);

        patcher.Data
            .SkipWhile(data => string.IsNullOrEmpty(data.AppSecret))
            .First()
            .ObserveOnMainThread()
            .Subscribe(Initialize);
        //TODO: Dispose subscription
    }

    private void Initialize(PatcherData data)
    {
        _cache = new UnityCache(data.AppSecret);

        if (IsCachedBannerAvailable())
        {
            _logger.LogDebug(string.Format("A cached banner image is available at {0}", CachedBannerPath));
            LoadBannerImage(CachedBannerPath, NewImage);
            MainAnimator.SetTrigger(AnimationSwitchTrigger);
        }

        var patcher = Patcher.Instance;

        patcher.AppInfo
            .SkipWhile(info => info.Id == default(int))
            .Select(info => new Data
            {
                BannerData = new PatcherBannerData
                {
                    ImageUrl = info.PatcherBannerImage,
                    Dimensions = info.PatcherBannerImageDimensions,
                    ModificationDate = info.PatcherBannerImageUpdatedAt
                },
                BannerFilePath = Path.Combine(data.AppDataPath, BannerImageFilename)
            })
            .ObserveOnMainThread()
            .Subscribe(OnBannerDataUpdate);
        //TODO: Dispose subscription
    }

    private void OnBannerDataUpdate(Data data)
    {
        _logger.LogDebug("On patcher data update.");
        var bannerData = data.BannerData;

        if (IsLocalBannerMissing(data))
        {
            AquireRemoteBanner(data);
        }
        else if (IsNewBannerAvailable(data))
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

    private bool IsLocalBannerMissing(Data data)
    {
        return !string.IsNullOrEmpty(CachedBannerModificationDate) && !File.Exists(CachedBannerPath);
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
        return bannerData.ModificationDate == CachedBannerModificationDate;
    }

    private void ClearCachedBanner()
    {
        _logger.LogDebug(string.Format("Clearning the cached banner at {0}", CachedBannerPath));
        if (!File.Exists(CachedBannerPath))
        {
            _logger.LogError("The cached banner doesn't exist.");
            return;
        }

        FileOperations.Delete(CachedBannerPath, CancellationToken.Empty);
        CachedBannerPath = "";
    }

    private void AquireRemoteBanner(Data data)
    {
        _logger.LogDebug(string.Format("Aquiring the remote banner image from {0}", data.BannerData.ImageUrl));
        var coroutine = Threading.StartThreadCoroutine(() => {
            CancellationTokenSource source = new CancellationTokenSource();

            var downloader = new HttpDownloader(data.BannerFilePath, new string[]{data.BannerData.ImageUrl});

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

        }, (bool result) => {
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
