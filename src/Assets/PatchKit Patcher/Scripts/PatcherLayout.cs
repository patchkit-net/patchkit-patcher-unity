using PatchKit.Unity.API.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher
{
    [RequireComponent(typeof(Patcher))]
    public class PatcherLayout : MonoBehaviour
    {
        private Patcher _patcher;

        private PatcherController _patcherController;

        public int Width;

        public int Height;

        public Button CloseButton;

        public Button MinimizeButton;

        public Text Status;

        public Slider ProgressBar;

        public Text ProgressText;

        public Text DownloadStatus;

        public Slider DownloadProgressBar;

        public Text DownloadProgressText;

        public Button PlayButton;

        public RectTransform RetryPopup;

        public AppChangelogText ChangelogText;

        private void UpdateTitleBarButtons()
        {
            bool areActive = UnityEngine.Application.platform == RuntimePlatform.WindowsPlayer;

            CloseButton.gameObject.SetActive(areActive);
            MinimizeButton.gameObject.SetActive(areActive);
        }

        private void Awake()
        {
            _patcher = GetComponent<Patcher>();

            _patcherController = GetComponent<PatcherController>();
        }

        private void Start()
        {
            Screen.SetResolution(Width, Height, false);

            UpdateTitleBarButtons();

            ChangelogText.SecretKey = _patcherController.SecretKey;

            ChangelogText.Refresh();
        }

        private void UpdateProgress()
        {
            ProgressBar.value = _patcher.Status.Progress;

            ProgressText.text = _patcher.Status.Progress.ToString("0.0%");

            DownloadProgressBar.value = _patcher.Status.DownloadProgress;

            DownloadProgressText.text = _patcher.Status.DownloadProgress.ToString("0.0%");
        }

        private void UpdateStatus()
        {
            switch (_patcher.Status.State)
            {
                case PatcherState.None:
                    {
                        Status.text = string.Empty;
                        DownloadStatus.text = string.Empty;
                        break;
                    }
                case PatcherState.Patching:
                    {
                        Status.text = "Processing...";
                        DownloadStatus.text = _patcher.Status.IsDownloading
                            ? string.Format("Downloading {0} kB/s",
                                _patcher.Status.DownloadSpeed.ToString("0.0"))
                            : string.Empty;
                        break;
                    }
                case PatcherState.Succeed:
                    {
                        Status.text = "Ready!";
                        DownloadStatus.text = string.Empty;
                        break;
                    }
                case PatcherState.Cancelled:
                    {
                        Status.text = "Patching has been cancelled.";
                        DownloadStatus.text = string.Empty;
                        break;
                    }
                case PatcherState.NoInternetConnection:
                    {
                        Status.text = "Please check your internet connection.";
                        DownloadStatus.text = string.Empty;
                        break;
                    }
                case PatcherState.Failed:
                    {
                        Status.text = "An error has occured!";
                        DownloadStatus.text = string.Empty;
                        break;
                    }
            }
        }

        private void UpdatePlayButton()
        {
            PlayButton.interactable = _patcher.Status.State == PatcherState.Succeed;
        }

        private void UpdateRetryPopup()
        {
            bool isVisible;

            switch (_patcher.Status.State)
            {
                case PatcherState.Cancelled:
                case PatcherState.Failed:
                case PatcherState.NoInternetConnection:
                case PatcherState.None:
                    {
                        isVisible = true;
                        break;
                    }
                default:
                    {
                        isVisible = false;

                        break;
                    }
            }

            var retryPopupLocalPosition = RetryPopup.anchoredPosition;

            float retryPopupWidth = RetryPopup.rect.width;

            float transitionSpeed = Time.deltaTime * retryPopupWidth * 3.0f;

            if (isVisible)
            {
                retryPopupLocalPosition.x -= transitionSpeed;
            }
            else
            {
                retryPopupLocalPosition.x += transitionSpeed;
            }

            retryPopupLocalPosition.x = Mathf.Clamp(retryPopupLocalPosition.x, 0.0f, retryPopupWidth);

            RetryPopup.anchoredPosition = retryPopupLocalPosition;
        }

        private void Update()
        {
            UpdateStatus();

            UpdateProgress();

            UpdatePlayButton();

            UpdateRetryPopup();
        }
    }
}