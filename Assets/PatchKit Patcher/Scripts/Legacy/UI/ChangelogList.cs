using System.Linq;
using JetBrains.Annotations;
using PatchKit.Api.Models;
using UnityEngine;
using UnityEngine.Assertions;

namespace Legacy.UI
{
public class ChangelogList : MonoBehaviour
{
    public ChangelogElement TitlePrefab;
    public ChangelogElement ChangePrefab;

    private bool _created;

    private void Awake()
    {
        Patcher.Instance.OnStateChanged += state =>
        {
            if (state.App?.Versions != null)
            {
                Create(versions: state.App.Value.Versions);
            }
        };
    }

    private void Create([NotNull] AppVersion[] versions)
    {
        if (_created)
        {
            return;
        }

        _created = true;

        foreach (var version in versions.OrderByDescending(
            keySelector: version => version.Id))
        {
            CreateVersionChangelog(version: version);
        }
    }

    private void CreateVersionChangelog(AppVersion version)
    {
        CreateVersionTitle(label: version.Label);
        CreateVersionChangeList(changelog: version.Changelog);
    }

    private void CreateVersionTitle(string label)
    {
        var title = Instantiate(original: TitlePrefab);
        Assert.IsNotNull(value: title);
        Assert.IsNotNull(value: title.Text);
        Assert.IsNotNull(value: title.transform);

        title.Text.text = $"Changelog {label}";
        title.transform.SetParent(
            parent: transform,
            worldPositionStays: false);
        title.transform.SetAsLastSibling();
    }

    private void CreateVersionChangeList(string changelog)
    {
        var changeList = (changelog ?? string.Empty).Split('\n');

        foreach (string change in changeList.Where(
            predicate: s => !string.IsNullOrEmpty(value: s)))
        {
            string formattedChange = change.TrimStart(
                ' ',
                '-',
                '*');
            CreateVersionChange(changeText: formattedChange);
        }
    }

    private void CreateVersionChange(string changeText)
    {
        var change = Instantiate(original: ChangePrefab);
        Assert.IsNotNull(value: change);
        Assert.IsNotNull(value: change.Text);
        Assert.IsNotNull(value: change.transform);

        change.Text.text = changeText;
        change.transform.SetParent(
            parent: transform,
            worldPositionStays: false);
        change.transform.SetAsLastSibling();
    }
}
}