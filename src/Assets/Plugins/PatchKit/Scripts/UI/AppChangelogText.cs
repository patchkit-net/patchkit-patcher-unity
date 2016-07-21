using System.Collections;
using System.Linq;
using PatchKit.Unity.Api;
using PatchKit.Unity.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.UI
{
    public class AppChangelogText : AppCompontent
    {
        private string _previousFormat;

        [Multiline]
        public string Format = "<b>{label}</b>\n{changelog}\n\n";

        public Text Text;

        protected override void Update()
        {
            if (Format != _previousFormat)
            {
                _previousFormat = Format;

                Refresh();
            }

            base.Update();
        }

        protected override IEnumerator RefreshCoroutine()
        {
            //TODO: Use ApiConnection.Instance.BeginGetAppChangelog() after it receives implementation
            var request = ApiConnectionInstance.Instance.BeginGetAppVersionList(AppSecret);

            yield return request.WaitCoroutine();

            var versionsList = ApiConnectionInstance.Instance.EndGetAppVersionList(request);

            Text.text = string.Join("\n",
                versionsList.OrderByDescending(version => version.Id).Select(version =>
                {
                    string changelog = Format;

                    changelog = changelog.Replace("{label}", version.Label);
                    changelog = changelog.Replace("{changelog}", version.Changelog);
                    string publishDate = TimeConvert.FromUnixTimeStamp(version.PublishDate).ToString("g");
                    changelog = changelog.Replace("{publishdate}", publishDate);

                    return changelog;
                }).ToArray());
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