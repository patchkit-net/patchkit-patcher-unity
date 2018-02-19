using UnityEngine;
using UnityEngine.UI;
using UniRx;
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

    private ICache _cache = new UnityCache();

    private const string CachedBannerUrlKey = "cached-banner-url-key";
    private const string CachedBannerFilePath = "banner";

    public Image targetImage;

    private ILogger _logger;

    private void Start()
    {
        _logger = (ILogger) PatcherLogManager.DefaultLogger;

        var patcher = Patcher.Instance;

        if (IsCachedBannerAvailable())
        {
            LoadCachedBanner();
        }

        Assert.IsNotNull(patcher);

        patcher.AppInfo
            .ObserveOnMainThread()
            .Skip(1)    // Skip the first update
            .Select(info => new PatcherBannerData{imageUrl = info.PatcherBannerImage, dimensions = info.PatcherBannerImageDimensions})
            .Subscribe(OnBannerDataUpdate);
    }

    private void OnBannerDataUpdate(PatcherBannerData bannerData)
    {
        if (IsCachedBannerAvailable() && IsCachedBannerSameAsRemote(bannerData.imageUrl))
        {
            return;
        }

        AquireRemoteBanner(bannerData);
    }

    private bool IsCachedBannerAvailable()
    {
        return _cache.GetValue(CachedBannerUrlKey) != null;
    }

    private bool IsCachedBannerSameAsRemote(string remoteBannerUrl)
    {
        var cachedBannerUrl = _cache.GetValue(CachedBannerUrlKey);

        return remoteBannerUrl == cachedBannerUrl;
    }

    private void AquireRemoteBanner(PatcherBannerData bannerData)
    {
        Threading.StartThreadCoroutine(() => {
            CancellationTokenSource source = new CancellationTokenSource();

            var downloader = new HttpDownloader(CachedBannerFilePath, new string[]{bannerData.imageUrl});

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
                LoadCachedBanner();
            }
        });

    }

    private void ActivateDefaultBanner()
    {

    }

    private void LoadCachedBanner()
    {
        Texture2D texture = new Texture2D(200, 200);

        if (File.Exists(CachedBannerFilePath))
        {
            var fileBytes = File.ReadAllBytes(CachedBannerFilePath);
            texture.LoadImage(fileBytes);

            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

            targetImage.sprite = sprite;
        }
    }
}
