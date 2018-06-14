using System;
using System.Collections;
using System.Linq;
using PatchKit.Api.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Patching.Unity.UI
{
    public class AppChangelogText : AppCompontent
    {
        [Multiline] public string Format = "<b>{label}</b>\n{changelog}\n\n";

        public Text Text;

        protected override IEnumerator LoadCoroutine()
        {
            yield return UnityThreading.StartThreadCoroutine(() => ApiConnection.GetAppVersionList(AppSecret, null, null, CancellationToken), response =>
            {
                Text.text = string.Join("\n",
                    response.OrderByDescending(version => version.Id).Select(version =>
                    {
                        string changelog = Format;

                        changelog = changelog.Replace("{label}", version.Label);
                        changelog = changelog.Replace("{changelog}", version.Changelog);
                        string publishDate = UnixTimeConvert.FromUnixTimeStamp(version.PublishDate).ToString("g");
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