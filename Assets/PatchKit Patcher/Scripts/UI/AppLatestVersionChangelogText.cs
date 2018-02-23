using System.Collections;
using UnityEngine.UI;

namespace PatchKit.Patching.Unity.UI
{
    public class AppLatestVersionChangelogText : AppCompontent
    {
        public Text Text;

        protected override IEnumerator LoadCoroutine()
        {
            yield return UnityThreading.StartThreadCoroutine(() => MainApiConnection.GetAppLatestAppVersion(AppSecret),
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