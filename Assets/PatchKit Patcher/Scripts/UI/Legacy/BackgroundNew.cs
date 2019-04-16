//using System;
//using System.IO;
//using JetBrains.Annotations;
//using PatchKit.Api.Models;
//using UnityEngine;
//using UnityEngine.Assertions;
//using UnityEngine.UI;
//
//namespace UI.Legacy
//{
//public class Background : MonoBehaviour
//{
//    public struct PatcherBannerData
//    {
//        public string ImageUrl;
//        public PatcherBannerImageDimensions Dimensions;
//        public string ModificationDate;
//    }
//
//    public struct Data
//    {
//        public PatcherBannerData BannerData;
//        public string BannerFilePath;
//    }
//
//    private const string CachedBannerFileName = "banner";
//
//    private const string CachedBannerFilePathKey = "cached-banner-path-key";
//
//    private const string CachedBannerModificationDateKey =
//        "cached-banner-modif-date-key";
//
//    private const string AnimationLoadingParameter = "isLoading";
//    private const string AnimationSwitchTrigger = "switch";
//
//    private string CachedBannerPath
//    {
//        get
//        {
//            return AppPlayerPrefs.GetString(
//                key: CachedBannerFilePathKey,
//                appSecret: Patcher.Instance.State.AppState.Secret);
//        }
//        set
//        {
//            AppPlayerPrefs.SetString(
//                key: CachedBannerFilePathKey,
//                appSecret: Patcher.Instance.State.AppState.Secret,
//                value: value);
//        }
//    }
//
//    private string CachedBannerModificationDate
//    {
//        get
//        {
//            return AppPlayerPrefs.GetString(
//                key: CachedBannerModificationDateKey,
//                appSecret: Patcher.Instance.State.AppState.Secret);
//        }
//        set
//        {
//            AppPlayerPrefs.SetString(
//                key: CachedBannerModificationDateKey,
//                appSecret: Patcher.Instance.State.AppState.Secret,
//                value: value);
//        }
//    }
//
//    public Animator MainAnimator;
//
//    public Image NewImage;
//    public Image OldImage;
//    public Sprite DefaultBackground;
//
//    private void Awake()
//    {
//        var banner = GetCachedBannerSprite();
//
//        Assert.IsNotNull(value: DefaultBackground);
//        Assert.IsNotNull(value: MainAnimator);
//        Assert.IsNotNull(value: NewImage);
//        Assert.IsNotNull(value: OldImage);
//
//        if (banner == null)
//        {
//            banner = DefaultBackground;
//        }
//
//        TransitionToNewBanner(sprite: banner);
//    }
//
//    private void Initialize(PatcherData data)
//    {
//        patcher.AppInfo.SkipWhile(info => info.Id == default(int))
//            .Select(
//                info => new Data
//                {
//                    BannerData = new PatcherBannerData
//                    {
//                        ImageUrl = info.PatcherBannerImage,
//                        Dimensions = info.PatcherBannerImageDimensions,
//                        ModificationDate = info.PatcherBannerImageUpdatedAt
//                    },
//                    BannerFilePath = Path.Combine(
//                        path1: data.AppDataPath,
//                        path2: CachedBannerFileName)
//                })
//            .ObserveOnMainThread()
//            .Subscribe(OnBannerDataUpdate);
//        //TODO: Dispose subscription
//    }
//
//    private void OnBannerDataUpdate(App app)
//    {
//        if (app.)
//
//            var bannerData = data.BannerData;
//
//        if (IsLocalBannerMissing(data: data))
//        {
//            AcquireRemoteBanner(data: data);
//        }
//        else if (IsNewBannerAvailable(data: data))
//        {
//            AcquireRemoteBanner(data: data);
//        }
//        else if (HasBannerBeenRemoved(data: data))
//        {
//            Debug.Log(message: "Banner image has been removed.");
//            ClearCachedBanner();
//            CachedBannerModificationDate = bannerData.ModificationDate;
//
//            SwitchToDefault();
//        }
//        else if (IsCachedBannerSameAsRemote(bannerData: data.BannerData))
//        {
//            Debug.Log(message: "Nothing has changed.");
//        }
//        else
//        {
//            Debug.Log(message: "Banner has never been set.");
//            SwitchToDefault();
//        }
//    }
//
//    private bool IsLocalBannerMissing(Data data)
//    {
//        return !string.IsNullOrEmpty(value: CachedBannerModificationDate) &&
//            !File.Exists(path: CachedBannerPath);
//    }
//
//    private bool IsNewBannerAvailable(Data data)
//    {
//        return !string.IsNullOrEmpty(value: data.BannerData.ImageUrl) &&
//            !IsCachedBannerSameAsRemote(bannerData: data.BannerData);
//    }
//
//    private bool HasBannerBeenRemoved(Data data)
//    {
//        return string.IsNullOrEmpty(value: data.BannerData.ImageUrl) &&
//            !string.IsNullOrEmpty(value: data.BannerData.ModificationDate) &&
//            IsCachedBannerAvailable();
//    }
//
//    private bool IsCachedBannerSameAsRemote(PatcherBannerData bannerData)
//    {
//        return bannerData.ModificationDate == CachedBannerModificationDate;
//    }
//
//    private void ClearCachedBanner()
//    {
//        if (!File.Exists(path: CachedBannerPath))
//        {
//            Debug.LogError(message: "The cached banner doesn't exist.");
//            return;
//        }
//
//        File.Delete(path: CachedBannerPath);
//        CachedBannerPath = "";
//    }
//
//    private void AcquireRemoteBanner(Data data)
//    {
//        //TODO:
//        /*
//        _logger.LogDebug(string.Format("Aquiring the remote banner image from {0}", data.BannerData.ImageUrl));
//        var coroutine = Threading.StartThreadCoroutine(() => {
//            CancellationTokenSource source = new CancellationTokenSource();
//
//            var downloader = new HttpDownloader(data.BannerFilePath, new string[]{data.BannerData.ImageUrl});
//
//            try
//            {
//                UnityDispatcher.Invoke(() => {
//                    MainAnimator.SetBool(AnimationLoadingParameter, true);
//                });
//
//                downloader.Download(source.Token);
//                return true;
//            }
//            catch (Exception)
//            {
//                return false;
//            }
//
//        }, (bool result) => {
//            if (result)
//            {
//                CachedBannerPath = data.BannerFilePath;
//                CachedBannerModificationDate = data.BannerData.ModificationDate;
//
//                MainAnimator.SetBool(AnimationLoadingParameter, false);
//                MainAnimator.SetTrigger(AnimationSwitchTrigger);
//
//                LoadBannerImage(data.BannerFilePath, NewImage);
//            }
//        });
//
//        StartCoroutine(coroutine);*/
//    }
//
//    private void TransitionToNewBanner([NotNull] Sprite sprite)
//    {
//        Assert.IsNotNull(value: OldImage);
//        Assert.IsNotNull(value: NewImage);
//        Assert.IsNotNull(value: MainAnimator);
//
//        OldImage.sprite = NewImage.sprite;
//        NewImage.sprite = sprite;
//
//        MainAnimator.SetTrigger(name: AnimationSwitchTrigger);
//    }
//
//    private Sprite GetCachedBannerSprite()
//    {
//        return string.IsNullOrEmpty(value: CachedBannerPath)
//            ? null
//            : GetBannerSprite(path: CachedBannerPath);
//    }
//
//    private Sprite GetBannerSprite([NotNull] string path)
//    {
//        if (!File.Exists(path: path))
//        {
//            return null;
//        }
//
//        try
//        {
//            var bytes = File.ReadAllBytes(path: path);
//
//            var texture = new Texture2D(
//                width: 0,
//                height: 0);
//
//            if (!texture.LoadImage(data: bytes))
//            {
//                return null;
//            }
//
//            return Sprite.Create(
//                texture: texture,
//                rect: new Rect(
//                    x: 0,
//                    y: 0,
//                    width: texture.width,
//                    height: texture.height),
//                pivot: Vector2.zero);
//        }
//        catch (Exception e)
//        {
//            Debug.LogException(exception: e);
//            return null;
//        }
//    }
//}
//}