using System.Collections;
using PatchKit.Unity.API.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.API.UI
{
    public class AppLatestVersionChangelogText : MonoBehaviour
    {
        public string SecretKey;

        public Text Text;

        private Coroutine _refreshCoroutine;

        public void Refresh()
        {
            if (_refreshCoroutine != null)
            {
                StopCoroutine(_refreshCoroutine);
            }

            _refreshCoroutine = StartCoroutine(RefreshCoroutine());
        }

        private void Start()
        {
            Refresh();
        }

        private IEnumerator RefreshCoroutine()
        {
            if (string.IsNullOrEmpty(SecretKey))
            {
                yield break;
            }

            var request = PatchKitUnity.API.BeginGetAppLatestVersion(SecretKey);

            yield return request.WaitCoroutine();

            var version = PatchKitUnity.API.EndGetAppLatestVersion(request);

            Text.text = version.Changelog;
        }

        private void Reset()
        {
            if (Text == null)
            {
                Text = GetComponent<Text>();
            }
        }
    }
}