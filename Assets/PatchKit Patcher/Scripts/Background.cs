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
    }

    public struct InitializationData
    {
        public PatcherBannerData bannerData;
        public string bannerFilePath;
    }

    private ICache _cache = new UnityCache();

    private const string CachedBannerUrlKey = "cached-banner-url-key";
    private const string CachedBannerPathKey = "cached-banner-path-key";
    private string _bannerFilePath;

    public Image targetImage;

    private PatchKit.Logging.ILogger _logger;

    void Start()
    {
        _logger = PatcherLogManager.DefaultLogger;

        _logger.LogDebug("Starting background image script.");

        var patcher = Patcher.Instance;

        if (IsCachedBannerAvailable())
        {
            _logger.LogDebug("A cached banner image is available.");
            LoadCachedBanner();
        }

        Assert.IsNotNull(patcher);

        var patcherData = patcher.Data
            // .Skip(1)
            .ObserveOnMainThread()
            .Select(data => data.AppDataPath);

        var appInfo = patcher.AppInfo
            // .Skip(1)
            .ObserveOnMainThread()
            .Select(info => new PatcherBannerData{imageUrl = "https://i.imgur.com/N95DUhU.jpg", dimensions = info.PatcherBannerImageDimensions});

        patcherData.CombineLatest(appInfo, (lhs, rhs) => new InitializationData{bannerData = rhs, bannerFilePath = lhs}).Subscribe(OnBannerDataUpdate);
    }

    private void OnBannerDataUpdate(InitializationData data)
    {
        _logger.LogDebug("On patcher data update.");
        var bannerData = data.bannerData;

        if (string.IsNullOrEmpty(data.bannerFilePath))
        {
            return;
        }

        _bannerFilePath = Path.Combine(data.bannerFilePath, "banner_image");

        if (IsCachedBannerAvailable() && IsCachedBannerSameAsRemote(bannerData.imageUrl))
        {
            _logger.LogDebug("The cached banner is the same as remote.");
            return;
        }

        if (string.IsNullOrEmpty(bannerData.imageUrl))
        {
            _logger.LogDebug("No banner is available.");
            return;
        }

        AquireRemoteBanner(bannerData);
    }

    private bool IsCachedBannerAvailable()
    {
        return !string.IsNullOrEmpty(_cache.GetValue(CachedBannerUrlKey));
    }

    private bool IsCachedBannerSameAsRemote(string remoteBannerUrl)
    {
        var cachedBannerUrl = _cache.GetValue(CachedBannerUrlKey);

        return remoteBannerUrl == cachedBannerUrl;
    }

    private void AquireRemoteBanner(PatcherBannerData bannerData)
    {
        var coroutine = Threading.StartThreadCoroutine(() => {
            CancellationTokenSource source = new CancellationTokenSource();

            var downloader = new HttpDownloader(_bannerFilePath, new string[]{bannerData.imageUrl});

            try
            {
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
                _cache.SetValue(CachedBannerUrlKey, bannerData.imageUrl);
                _cache.SetValue(CachedBannerPathKey, _bannerFilePath);
                LoadCachedBanner();
            }
        });

        StartCoroutine(coroutine);

    }

    private void ActivateDefaultBanner()
    {

    }

    private void LoadCachedBanner()
    {
        Texture2D texture = new Texture2D(200, 200);

        if (string.IsNullOrEmpty(_bannerFilePath))
        {
            _bannerFilePath = _cache.GetValue(CachedBannerPathKey);
        }

        if (File.Exists(_bannerFilePath))
        {
            var fileBytes = File.ReadAllBytes(_bannerFilePath);
            texture.LoadImage(fileBytes);

            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

            targetImage.sprite = sprite;
        }
    }
}
