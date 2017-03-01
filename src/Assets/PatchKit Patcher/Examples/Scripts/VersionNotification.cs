using UnityEngine;
using System.Collections;
using PatchKit.Api;
using PatchKit.Unity.Utilities;
using UnityEngine.UI;

namespace PatchKit.Unity.Examples
{
    public class VersionNotification : MonoBehaviour
    {
        private MainApiConnection _apiConnection;

        protected MainApiConnection ApiConnection
        {
            get { return _apiConnection ?? (_apiConnection = new MainApiConnection(Settings.GetMainApiConnectionSettings())); }
        }

        private bool _newVersion = false;

        private IEnumerator _checkForNewVersionCoroutine;

        public Image Background;

        public Text Text;

        public string AppSecret;

        public int CurrentVersionId;

        public float CheckInterval = 15.0f;

        IEnumerator CheckForNewVersionCoroutine()
        {
            while (!_newVersion)
            {
                yield return Threading.StartThreadCoroutine(() => ApiConnection.GetAppLatestAppVersion(AppSecret),
                    response =>
                    {
                        if (CurrentVersionId < response.Id)
                        {
                            Background.CrossFadeAlpha(1.0f, 1.0f, true);
                            Text.CrossFadeAlpha(1.0f, 1.0f, true);
                            Text.text = "<b>A new version is available!</b>\n\n" +
                                        "<b>" + response.Label + "</b>\n" +
                                        response.Changelog;
                        }
                    });

                yield return new WaitForSeconds(CheckInterval);
            }
        }

        void Awake()
        {
            Background.CrossFadeAlpha(0.0f, 0.0f, true);
            Text.CrossFadeAlpha(0.0f, 0.0f, true);
        }

        void OnEnable()
        {
            _checkForNewVersionCoroutine = CheckForNewVersionCoroutine();

            StartCoroutine(_checkForNewVersionCoroutine);
        }

        void OnDisable()
        {
            if (_checkForNewVersionCoroutine != null)
            {
                StopCoroutine(_checkForNewVersionCoroutine);
            }
        }
    }
}
