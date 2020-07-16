﻿using System;
using System.Collections;
using System.Linq;
using Newtonsoft.Json;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Cancellation;
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

        private void CreateAndCacheChangelog(string appSecret, Versions[] versions)
        {
            try
            {
                var cacheValue = JsonConvert.SerializeObject(versions);

                new UnityCache(appSecret).SetValue("app-changelog", cacheValue);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e.ToString());
            }

            CreateChangelog(versions);
        }

        private void DestroyOldChangelog()
        {
            while(transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }

        }

        private void CreateChangelog(Versions[] versions)
        {
            DestroyOldChangelog();

            foreach (Versions version in versions)
            {
                CreateVersionChangelog(version);
            }
        }

        private void CreateVersionChangelog(Versions version)
        {
            CreateVersionTitle(version.VersionLabel);
            CreateVersionChangeList(version.Changes);
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