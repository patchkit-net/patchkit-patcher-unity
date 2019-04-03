using System;
using System.Collections;
using System.Threading;
using PatchKit.Unity.Utilities;
using Text = UnityEngine.UI.Text;
using Timeout = PatchKit.Core.Timeout;

namespace PatchKit.Unity.UI
{
    public class AppLatestVersionChangelogText : AppCompontent
    {
        public Text Text;

        protected override IEnumerator LoadCoroutine()
        {
            yield return Threading.StartThreadCoroutine(() => MainApiConnection.GetAppLatestAppVersion(AppSecret, new Timeout(TimeSpan.FromSeconds(10)), CancellationToken.None),
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