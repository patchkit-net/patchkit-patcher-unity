using System;
using System.Collections;
using System.Linq;
using Newtonsoft.Json;
using PatchKit.Api.Models.Main;
using PatchKit.Api.Utilities;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.UI;
using PatchKit.Unity.UI.Languages;
using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class NewChangelogList : UIApiComponent
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(NewChangelogList));
        
        private int _versionNumber;
        
        public ChangelogElementHeader TitlePrefabDark;
        public ChangelogElementHeader TitlePrefabBright;
        public NewChangelogElement ChangePrefab;
        public LastRelease LastRelease;

        protected override IEnumerator LoadCoroutine()
        {
            while (!Patcher.Instance.Data.HasValue || Patcher.Instance.Data.Value.AppSecret == null)
            {
                yield return null;
            }

            string appSecret = Patcher.Instance.Data.Value.AppSecret;

            LoadChangelogFromCache(appSecret);

            yield return
                Threading.StartThreadCoroutine(() =>
                        MainApiConnection.GetAppChangelog(
                            Patcher.Instance.Data.Value.AppSecret,
                            CancellationToken.Empty),
                    changelog => CreateAndCacheChangelog(appSecret, changelog.versions));
        }
        
        private void LoadChangelogFromCache(string appSecret)
        {
            try
            {
                string cacheValue = new UnityCache(appSecret).GetValue("app-changelog", null);

                if (cacheValue == null)
                {
                    return;
                }

                ChangelogEntry[] versions = JsonConvert.DeserializeObject<Changelog>(cacheValue).versions;

                CreateChangelog(versions);
            }
            catch (Exception)
            {
                // ignore
            }
        }

        private void CreateAndCacheChangelog(string appSecret, ChangelogEntry[] versions)
        {
            try
            {
                string cacheValue = JsonConvert.SerializeObject(versions);

                new UnityCache(appSecret).SetValue("app-changelog", cacheValue);
            }
            catch (Exception e)
            {
                DebugLogger.Log(e.ToString());
            }

            CreateChangelog(versions);
        }

        private void DestroyOldChangelog()
        {
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
        }

        private void CreateChangelog(ChangelogEntry[] versions)
        {
            DestroyOldChangelog();
            _versionNumber = versions.Length;

            CreateLastRelease(versions[0]);
            foreach (ChangelogEntry version in versions)
            {
                CreateVersionChangelog(version);
                _versionNumber--;
            }
        }

        private void CreateLastRelease(ChangelogEntry changelogEntry)
        {
            LastRelease.Label.SetText(changelogEntry.VersionLabel);
            string publishDate = UnixTimeConvert.FromUnixTimeStamp(changelogEntry.PublishTime)
                .ToString("dd <>MMM</>, yyyy").ToLower()
                .Replace("<>", PatcherLanguages.OpenTag)
                .Replace("</>", PatcherLanguages.CloseTag);
            LastRelease.PublishDate.SetText(publishDate);
            
            CreateVersionChangeList(changelogEntry.Changes, LastRelease.ChangeList);
            LastRelease.gameObject.SetActive(false);
        }

        private void CreateVersionChangelog(ChangelogEntry changelogEntry)
        {
            Transform body = CreateVersionTitleWithPublishData(changelogEntry.VersionLabel, changelogEntry.PublishTime,
                !String.IsNullOrEmpty(changelogEntry.Changes));
            CreateVersionChangeList(changelogEntry.Changes, body);
            body.gameObject.SetActive(false);
        }

        private Transform CreateVersionTitleWithPublishData(string label, long publishTime, bool areChanges)
        {
            ChangelogElementHeader title = Instantiate(_versionNumber % 2 == 1 ? TitlePrefabDark : TitlePrefabBright,
                transform, false);
            title.Title.SetText(string.Format("{0}", label));
            string publishDate = UnixTimeConvert.FromUnixTimeStamp(publishTime)
                .ToString("dd <>MMM</>, yyyy").ToLower()
                .Replace("<>", PatcherLanguages.OpenTag)
                .Replace("</>", PatcherLanguages.CloseTag);
            title.PublishDate.SetText(string.Format("{0}", publishDate));
            title.ArrowButton.SetActive(areChanges);
            title.transform.SetAsLastSibling();
            
            return title.Body;
        }

        private void CreateVersionChangeList(string changelog, Transform parent)
        {
            string[] changeList = (changelog ?? string.Empty).Split('\n');
            
            foreach (string change in changeList.Where(s => !string.IsNullOrEmpty(s)))
            {
                string formattedChange = change.TrimStart(' ', '-', '*');
                CreateVersionChange(formattedChange, parent);
            }
        }

        private void CreateVersionChange(string changeText, Transform parent)
        {
            NewChangelogElement change = Instantiate(ChangePrefab, parent, false);
            change.Text.SetText(changeText);
            change.transform.SetAsLastSibling();
        }
    }
}