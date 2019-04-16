using UnityEngine;
using UnityEngine.UI;
using UniRx;
using PatchKit.Api.Models;
using System.IO;
using UnityEngine.Assertions;

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

    private const string CachedBannerPathKey = "cached-banner-path-key";
    private const string CachedBannerModificationDateKey = "cached-banner-modif-date-key";

    private const string BannerImageFilename = "banner";

    private const string AnimationLoadingParameter = "isLoading";
    private const string AnimationSwitchTrigger = "switch";

    public string CachedBannerPath
    {
        get
        {
            return AppPlayerPrefs.GetString(
                CachedBannerPathKey,
                Patcher.Instance.State.AppState.Secret);
        }
        private set
        {
            AppPlayerPrefs.SetString(
                CachedBannerPathKey,
                Patcher.Instance.State.AppState.Secret,
                value);
        }
    }

    public string CachedBannerModificationDate
    {
        get
        {
            return AppPlayerPrefs.GetString(
                CachedBannerModificationDateKey,
                Patcher.Instance.State.AppState.Secret);
        }

        private set
        {
            AppPlayerPrefs.SetString(
                CachedBannerModificationDateKey,
                Patcher.Instance.State.AppState.Secret,
                value);
        }
    }

    public Image NewImage;
    public Image OldImage;

    public Sprite DefaultBackground;

    public Animator MainAnimator;

    private bool _initialized;

    private void Start()
    {
        var patcher = Patcher.Instance;

        Assert.IsNotNull(patcher);
        Assert.IsNotNull(MainAnimator);
        Assert.IsNotNull(NewImage);
        Assert.IsNotNull(OldImage);

        if (IsCachedBannerAvailable())
        {
            Debug.Log($"A cached banner image is available at {CachedBannerPath}");
            LoadBannerImage(CachedBannerPath, OldImage);
        }

        Patcher.Instance.StateChanged += state => Initialize();
    }

    private void Initialize()
    {
        if (!Patcher.Instance.State.AppState.Info.HasValue || _initialized)
        {
            return;
        }

        var info = Patcher.Instance.State.AppState.Info.Value;

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
                Patcher.Instance.State.AppState.Path,
                BannerImageFilename)
        };

        OnBannerDataUpdate(data);
    }

    private void OnBannerDataUpdate(Data data)
    {
        Debug.Log("On patcher data update.");
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
            Debug.Log("Banner image has been removed.");
            ClearCachedBanner();
            CachedBannerModificationDate = bannerData.ModificationDate;

            SwitchToDefault();
        }
        else if (IsCachedBannerSameAsRemote(data.BannerData))
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
        Debug.Log($"Clearning the cached banner at {CachedBannerPath}");
        if (!File.Exists(CachedBannerPath))
        {
            Debug.LogError("The cached banner doesn't exist.");
            return;
        }

        File.Delete(CachedBannerPath);
        CachedBannerPath = "";
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

    private void LoadBannerImage(string filepath, Image target)
    {
        Texture2D texture = new Texture2D(0, 0);

        if (string.IsNullOrEmpty(filepath))
        {
            filepath = CachedBannerPath;

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

            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

            target.sprite = sprite;
        }
        else
        {
            Debug.LogWarning("The cached banner image doesn't exist.");
        }
    }
}
