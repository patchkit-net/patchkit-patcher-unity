using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UniRx;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity
{

    public class PatchKitLogo : MonoBehaviour
    {
        public string patchKitWebsiteUrl;

        public Texture2D cursorTexture;

        public Vector2 cursorHotspot;

        public Image image;

        public Button button;

        private bool _isLogoVisible = false;

        private const float LogoVisibilityChangeDelay = 3; // in seconds

        private void Awake()
        {
            Assert.IsNotNull(button);
            Assert.IsNotNull(image);

            Assert.IsNotNull(cursorTexture);

            button.enabled = false;
            image.enabled = false;
        }

        private void Start()
        {
            var patcher = Patcher.Patcher.Instance;

            Assert.IsNotNull(patcher);

            patcher.AppInfo
                .ObserveOnMainThread()
                .Select(app => app.PatcherWhitelabel)
                .Subscribe(Resolve)
                .AddTo(this);

            StartCoroutine(ChangeVisibility());
        }

        private IEnumerator ChangeVisibility()
        {
            while (true)
            {
                yield return new WaitForSeconds(LogoVisibilityChangeDelay);
                image.enabled = _isLogoVisible;
                button.enabled = _isLogoVisible;
            }
        }

        private void Resolve(bool isWhitelabel)
        {
            _isLogoVisible = !isWhitelabel;
        }

        public void OnMouseEnter()
        {
            Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
        }

        public void OnMouseExit()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        public void GoToPatchKit()
        {
            Application.OpenURL(patchKitWebsiteUrl);
        }
    }
}