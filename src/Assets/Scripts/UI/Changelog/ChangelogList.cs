using System.Collections;
using System.Linq;
using Newtonsoft.Json.Linq;
using PatchKit.Unity.UI;
using PatchKit.Unity.Utilities;

namespace PatchKit.Unity.Patcher.UI
{
    public class ChangelogList : UIApiComponent
    {
        public ChangelogElement TitlePrefab;

        public ChangelogElement ChangePrefab;

        protected override IEnumerator LoadCoroutine()
        {
            yield return
                ApiConnection.GetCoroutine(
                    string.Format("1/apps/{0}/versions", PatcherApplication.Instance.Configuration.AppSecret), null,
                    response =>
                    {
                        var versions = response.GetJson();
                        foreach (var version in versions.OrderByDescending(version => version.Value<int>("id")))
                        {
                            if (version is JObject)
                            {
                                CreateVersionChangelog(version as JObject);
                            }
                        }
                    });
        }

        private void CreateVersionChangelog(JObject version)
        {
            CreateVersionTitle(version.Value<string>("label"));
            CreateVersionChangeList(version.Value<string>("changelog"));
        }

        private void CreateVersionTitle(string label)
        {
            var title = Instantiate(TitlePrefab);
            title.Text.text = string.Format("Changelog {0}", label);
            title.transform.SetParent(transform, false);
            title.transform.SetAsLastSibling();
        }

        private void CreateVersionChangeList(string changelog)
        {
            var changeList = (changelog ?? string.Empty).Split('\n');

            foreach (var change in changeList.Where(s => !string.IsNullOrEmpty(s)))
            {
                string formattedChange = change.TrimStart(' ', '-', '*');
                CreateVersionChange(formattedChange);
            }
        }

        private void CreateVersionChange(string changeText)
        {
            var change = Instantiate(ChangePrefab);
            change.Text.text = changeText;
            change.transform.SetParent(transform, false);
            change.transform.SetAsLastSibling();
        }
    }
}