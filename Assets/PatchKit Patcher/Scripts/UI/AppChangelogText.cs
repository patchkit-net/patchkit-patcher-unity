using System.Collections;
using System.Linq;
using PatchKit.Api.Utilities;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.UI.Languages;
using PatchKit.Unity.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.UI
{
    public class AppChangelogText : AppCompontent
    {
        public Text Text;

        protected override IEnumerator LoadCoroutine()
        {
            yield return Threading.StartThreadCoroutine(() => MainApiConnection.GetAppChangelog(AppSecret, CancellationToken.Empty), response =>
            {
                Text.text = string.Join("\n",
                    response.versions.Select(version =>
                    {
                        string changelog = string.Format("<b>{label}</b>  {0} {publishdate}\n{changelog}\n\n",PatcherLanguages.GetTraduction("published_at"));

                        changelog = changelog.Replace("{label}", version.VersionLabel);
                        changelog = changelog.Replace("{changelog}", version.Changes);
                        string publishDate = UnixTimeConvert.FromUnixTimeStamp(version.PublishTime).ToString("g");
                        changelog = changelog.Replace("{publishdate}", publishDate);

                        return changelog;
                    }).ToArray());
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