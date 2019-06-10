using System.IO;
using PatchKit.Api.Models;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using JetBrains.Annotations;

namespace Legacy.UI
{
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

    private const string CachedBannerPathKey = "cached-banner-path-key";

    private const string CachedBannerModificationDateKey =
        "cached-banner-modif-date-key";

    private const string BannerImageFilename = "banner";

    private const string AnimationLoadingParameter = "isLoading";
    private const string AnimationSwitchTrigger = "switch";

    private string GetCachedBannerPath(
        [NotNull] string appSecret)
    {
        return AppPlayerPrefs.GetString(
            appSecret: appSecret,
            key: CachedBannerPathKey);
    }

    private void SetCachedBannerPath(
        [NotNull] string appSecret,
        string value)
    {
        AppPlayerPrefs.SetString(
            appSecret: appSecret,
            key: CachedBannerPathKey,
            value: value);
    }

    private string GetCachedBannerModificationDate(
        [NotNull] string appSecret)
    {
        return AppPlayerPrefs.GetString(
            appSecret: appSecret,
            key: CachedBannerModificationDateKey);
    }
    
    private void SetCachedBannerModificationDate(
        [NotNull] string appSecret,
        string value)
    {
        AppPlayerPrefs.SetString(
            appSecret: appSecret,
            key: CachedBannerModificationDateKey,
            value: value);
    }

    public Image NewImage;
    public Image OldImage;

    public Sprite DefaultBackground;

    public Animator MainAnimator;

    private bool _initialized;

    private void Start()
    {
        Assert.IsNotNull(MainAnimator);
        Assert.IsNotNull(NewImage);
        Assert.IsNotNull(OldImage);

        Patcher.Instance.OnStateChanged += state =>
        {
            if (state.App != null)
            {
                Initialize(state.App.Value);
            }
        };
    }

    private void Initialize(AppState app)
    {
        if (!app.Info.HasValue || _initialized)
        {
            return;
        }

        if (IsCachedBannerAvailable(app.Secret))
        {
            var path = GetCachedBannerPath(appSecret: app.Secret);
            Debug.Log(
                $"A cached banner image is available at {path}");
            LoadBannerImage(
                app.Secret,
                path,
                OldImage);
        }

        var info = app.Info.Value;

        _initialized = true;

        var data = new Data
        {
            BannerData = new PatcherBannerData
            {
                ImageUrl = info.PatcherBannerImage,
                Dimensions = info.PatcherBannerImageDimensions,
                ModificationDate = info.PatcherBannerImageUpdatedAt
            },
            BannerFilePath = Path.Combine(
                app.Path,
                BannerImageFilename)
        };

        OnBannerDataUpdate(app.Secret, data);
    }

    private void OnBannerDataUpdate(
        [NotNull] string appSecret,
        Data data)
    {
        Debug.Log("On patcher data update.");
        var bannerData = data.BannerData;

        if (IsLocalBannerMissing(appSecret, data))
        {
            AquireRemoteBanner(data);
        }
        else if (IsNewBannerAvailable(appSecret, data))
        {
            AquireRemoteBanner(data);
        }
        else if (HasBannerBeenRemoved(appSecret, data))
        {
            Debug.Log("Banner image has been removed.");
            ClearCachedBanner(appSecret);
            SetCachedBannerModificationDate(
                appSecret: appSecret,
                value: bannerData.ModificationDate);

            SwitchToDefault();
        }
        else if (IsCachedBannerSameAsRemote(appSecret, data.BannerData))
        {
            Debug.Log("Nothing has changed.");
        }
        else
        {
            Debug.Log("Banner has never been set.");
            SwitchToDefault();
        }
    }

    private void SwitchToDefault()
    {
        Debug.Log("Switching to default background");
        NewImage.sprite = DefaultBackground;
        MainAnimator.SetTrigger(AnimationSwitchTrigger);
    }

    private bool IsLocalBannerMissing(
        [NotNull] string appSecret,
        Data data)
    {
        return !string.IsNullOrEmpty(GetCachedBannerModificationDate(appSecret: appSecret)) &&
            !File.Exists(GetCachedBannerPath(appSecret: appSecret));
    }

    private bool IsNewBannerAvailable(
        [NotNull] string appSecret,
        Data data)
    {
        return !string.IsNullOrEmpty(data.BannerData.ImageUrl) &&
            !IsCachedBannerSameAsRemote(appSecret, data.BannerData);
    }

    private bool HasBannerBeenRemoved(
        [NotNull] string appSecret,
        Data data)
    {
        return string.IsNullOrEmpty(data.BannerData.ImageUrl) &&
            !string.IsNullOrEmpty(data.BannerData.ModificationDate) &&
            IsCachedBannerAvailable(appSecret);
    }

    private bool IsCachedBannerAvailable(
        [NotNull] string appSecret)
    {
        var path = GetCachedBannerPath(appSecret: appSecret);
        return !string.IsNullOrEmpty(path) &&
            File.Exists(path);
    }

    private bool IsCachedBannerSameAsRemote(
        [NotNull] string appSecret,
        PatcherBannerData bannerData)
    {
        var cachedDate = GetCachedBannerModificationDate(appSecret: appSecret);
        return bannerData.ModificationDate == cachedDate;
    }

    private void ClearCachedBanner([NotNull] string appSecret)
    {
        var path = GetCachedBannerPath(appSecret: appSecret);
        Debug.Log($"Clearning the cached banner at {path}");
        if (!File.Exists(path))
        {
            Debug.LogError("The cached banner doesn't exist.");
            return;
        }

        File.Delete(path);
        SetCachedBannerPath(appSecret: appSecret, value: "");
    }

    private void AquireRemoteBanner(Data data)
    {
        //TODO:
        /*
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

        StartCoroutine(coroutine);*/
    }

    private void LoadBannerImage(
        [NotNull] string appSecret,
        string filepath,
        Image target)
    {
        Texture2D texture = new Texture2D(
            0,
            0);

        if (string.IsNullOrEmpty(filepath))
        {
            filepath = GetCachedBannerPath(appSecret: appSecret);

            if (string.IsNullOrEmpty(filepath))
            {
                Debug.LogWarning("Banner file path was null or empty.");
                return;
            }
        }

        if (File.Exists(filepath))
        {
            Debug.Log($"Loading the banner image from {filepath}");
            var fileBytes = File.ReadAllBytes(filepath);

            if (!texture.LoadImage(fileBytes))
            {
                Debug.LogError("Failed to load the banner image.");
                return;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(
                    0,
                    0,
                    texture.width,
                    texture.height),
                Vector2.zero);

            target.sprite = sprite;
        }
        else
        {
            Debug.LogWarning("The cached banner image doesn't exist.");
        }
    }
}
}