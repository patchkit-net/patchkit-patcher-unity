using System.Collections;
using PatchKit.Unity.Api;
using PatchKit.Unity.Utilities;
using UnityEngine.UI;

namespace PatchKit.Unity.UI
{
    public class AppLatestVersionChangelogText : AppCompontent
    {
        public Text Text;

        protected override IEnumerator RefreshCoroutine()
        {
            var request = ApiConnectionInstance.Instance.BeginGetAppLatestAppVersion(AppSecret);

            yield return request.WaitCoroutine();

            var version = ApiConnectionInstance.Instance.EndGetAppLatestAppVersion(request);

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