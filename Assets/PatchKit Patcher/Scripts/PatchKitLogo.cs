using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UniRx;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity
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
            var patcher = Patcher.Patcher.Instance;

            _button = GetComponent<Button>();
            _image = GetComponent<Image>();

            Assert.IsNotNull(CursorTexture);

            _button.enabled = false;
            _image.enabled = false;

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