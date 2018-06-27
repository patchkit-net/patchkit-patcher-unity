using PatchKit.Apps.Updating.Debug;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace PatchKit.Patching.Unity
{

    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public class PatchKitLogo : MonoBehaviour
    {
        private const string PatchKitWebsiteUrl = "https://patchkit.net/?source=patcher";

        public Texture2D CursorTexture;

        public Vector2 CursorHotspot;

        private Image _image;

        private Button _button;

        private void Start()
        {
            var patcher = Patcher.Instance;

            _button = GetComponent<Button>();
            _image = GetComponent<Image>();

            Assert.IsNotNull(CursorTexture);

            _button.enabled = false;
            _image.enabled = false;

            _button.onClick.AddListener(GoToPatchKit);

            Assert.IsNotNull(patcher);

            patcher.AppInfo
                .ObserveOnMainThread()
                .Where(app => app.Id != default(int))
                .Select(app => app.PatcherWhitelabel)
                .Subscribe(Resolve)
                .AddTo(this);
        }

        private void ChangeVisibility(bool isLogoVisible)
        {
            _image.enabled = isLogoVisible;
            _button.enabled = isLogoVisible;
        }

        private void Resolve(bool isWhitelabel)
        {
            ChangeVisibility(!isWhitelabel);
        }

        public void OnMouseEnter()
        {
            Cursor.SetCursor(CursorTexture, CursorHotspot, CursorMode.Auto);
        }

        public void OnMouseExit()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        public void GoToPatchKit()
        {
            Application.OpenURL(PatchKitWebsiteUrl);
        }
    }
}