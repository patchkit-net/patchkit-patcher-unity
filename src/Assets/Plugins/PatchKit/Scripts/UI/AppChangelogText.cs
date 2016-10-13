using System.Collections;
using System.Linq;
using Newtonsoft.Json.Linq;
using PatchKit.Api.Utilities;
using PatchKit.Unity.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.UI
{
    public class AppChangelogText : AppCompontent
    {
        [Multiline] public string Format = "<b>{label}</b>\n{changelog}\n\n";

        public Text Text;

        protected override IEnumerator LoadCoroutine()
        {
            yield return ApiConnection.GetCoroutine(string.Format("1/apps/{0}/versions", AppSecret), null, response =>
            {
                Text.text = string.Join("\n",
                    response.GetJson().Values<JObject>().OrderByDescending(version => version.Value<int>("id")).Select(version =>
                    {
                        string changelog = Format;

                        changelog = changelog.Replace("{label}", version.Value<string>("label"));
                        changelog = changelog.Replace("{changelog}", version.Value<string>("changelog"));
                        string publishDate = UnixTimeConvert.FromUnixTimeStamp(version.Value<int>("publish_date")).ToString("g");
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