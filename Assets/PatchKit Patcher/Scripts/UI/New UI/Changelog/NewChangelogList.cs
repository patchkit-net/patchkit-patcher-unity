﻿using System;
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

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class NewChangelogList : UIApiComponent
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(NewChangelogList));

        public ChangelogElement TitlePrefab;

        public ChangelogElement ChangePrefab;

        public RealeasesList RealeasesList;

        protected override IEnumerator LoadCoroutine()
        {
            while (!Patcher.Instance.Data.HasValue || Patcher.Instance.Data.Value.AppSecret == null)
            {
                yield return null;
            }

            var appSecret = Patcher.Instance.Data.Value.AppSecret;

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
                var cacheValue = new UnityCache(appSecret).GetValue("app-changelog", null);

                if (cacheValue == null)
                {
                    return;
                }

                var versions = JsonConvert.DeserializeObject<Changelog>(cacheValue).versions;

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
                var cacheValue = JsonConvert.SerializeObject(versions);

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
            
            RealeasesList.AddButtons(versions);
            foreach (ChangelogEntry version in versions)
            {
                CreateVersionChangelog(version);
            }
        }

        private void CreateVersionChangelog(ChangelogEntry changelogEntry)
        {
            CreateVersionTitleWithPublishData(changelogEntry.VersionLabel, changelogEntry.PublishTime);
            CreateVersionChangeList(changelogEntry.Changes);
        }

        private void CreateVersionTitleWithPublishData(string label, long publishTime)
        {
            var title = Instantiate(TitlePrefab);
            title.Texts[1].SetText(string.Format("Changelog {0}", label));
            string publishDate = UnixTimeConvert.FromUnixTimeStamp(publishTime)
                .ToString("g", CurrentCultureInfo.GetCurrentCultureInfo());
            title.Texts[0].SetText(string.Format("{0} {1}",
                PatcherLanguages.OpenTag + "published_at" + PatcherLanguages.CloseTag, publishDate));
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
            change.Texts[0].SetText(changeText);
            change.transform.SetParent(transform, false);
            change.transform.SetAsLastSibling();
        }
    }
}