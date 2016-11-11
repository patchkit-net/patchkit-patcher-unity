using System.Collections;
using System.Linq;
using PatchKit.Api;
using PatchKit.Unity.Api;
using PatchKit.Unity.UI;
using PatchKit.Unity.Utilities;
using UnityEngine.Assertions;

namespace PatchKit.Unity.Patcher.UI
{
    public class ChangelogList : RefreshableComponent
    {
        public ChangelogElement TitlePrefab;

        public ChangelogElement ChangePrefab;

        private void Start()
        {
            Refresh();
        }

        protected override IEnumerator RefreshCoroutine()
        {
            Assert.IsNotNull(PatcherApplication.Instance.Configuration.AppSecret);
            
            var request =
                ApiConnectionInstance.Instance.BeginGetAppVersionList(
                    PatcherApplication.Instance.Configuration.AppSecret);

            yield return request.WaitCoroutine();

            var versions = ApiConnectionInstance.Instance.EndGetAppVersionList(request);

            foreach (var version in versions.OrderByDescending(version => version.Id))
            {
                CreateVersionChangelog(version);
            }
        }

        private void CreateVersionChangelog(ApiConnection.AppVersion version)
        {
            CreateVersionTitle(version);
            CreateVersionChangeList(version);
        }

        private void CreateVersionTitle(ApiConnection.AppVersion version)
        {
            var title = Instantiate(TitlePrefab);
            title.Text.text = string.Format("Changelog {0}", version.Label);
            title.transform.SetParent(transform, false);
            title.transform.SetAsLastSibling();
        }

        private void CreateVersionChangeList(ApiConnection.AppVersion version)
        {
            var changeList = (version.Changelog ?? string.Empty).Split('\n');

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
