using System.Collections;
using UnityEngine.UI;

namespace PatchKit.Patching.Unity.UI
{
    public class AppLatestVersionChangelogText : AppCompontent
    {
        public Text Text;

        protected override IEnumerator LoadCoroutine()
        {
            yield return UnityThreading.StartThreadCoroutine(() => ApiConnection.GetAppLatestAppVersion(AppSecret, null, CancellationToken),
                response =>
                {
                    Text.text = response.Changelog;
                });
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