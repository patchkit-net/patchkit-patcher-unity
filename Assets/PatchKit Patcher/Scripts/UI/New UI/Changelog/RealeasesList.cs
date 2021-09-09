using System;
using System.Collections.Generic;
using System.Linq;
using PatchKit.Unity.Patcher.Debug;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class RealeasesList : MonoBehaviour
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(RealeasesList));

        public RealeasesElement RealeasesPrefab;
        public Transform ChangelogListTransform;
        public GameObject SpacingPrefab;
        private RealeasesElement _currentlySelected;

        public RealeasesElement CurrentlySelected
        {
            get { return _currentlySelected; }
        }

        private List<RealeasesElement> _realeasesElements = new List<RealeasesElement>();
        private float _changelogListPosition;
        private float _tolerance;
        private bool _start = true;
        private List<int> VersionOfElements;
        private float _spacingChangelogList;
        private float _endPosition = 0;
        private RectTransform _rectTransformRealeasesList;
        private float _spacingRealeasesList;
        private NewChangelogList _newChangelogList;



        public void AddButtons(int versionsCount, List<int> versionOfElements)
        {
            VersionOfElements = versionOfElements;

            for (int i = versionsCount; i > 0; i--)
            {
                AddButton(i);
            }
        }

        public void AddButton(int versionNumber)
        {
            var release = Instantiate(RealeasesPrefab, transform, false);
            release.VersionID = versionNumber;
            release.Text.SetText(versionNumber.ToString("#.#.#"));
            release.transform.SetAsLastSibling();
            release.Image.color = Color.clear;
            release.SetButton(_newChangelogList, this);

            _realeasesElements.Add(release);
        }

        private void Start()
        {
            _spacingChangelogList = ChangelogListTransform.GetComponent<VerticalLayoutGroup>().spacing;
            _rectTransformRealeasesList = (RectTransform) transform;
            _spacingRealeasesList = GetComponent<VerticalLayoutGroup>().spacing;
            _newChangelogList = ChangelogListTransform.GetComponent<NewChangelogList>();
        }

        private void LateUpdate()
        {
            if (_realeasesElements.Count > 0 && !_start && ChangelogListTransform.localPosition.y >= 0 &&
                Math.Abs(_changelogListPosition - ChangelogListTransform.localPosition.y) > _tolerance &&
                ChangelogListTransform.localPosition.y < _endPosition)
            {
                SelectRelease();
            }
        }

        public void PrepareReleases()
        {
            if (_realeasesElements.Count > 0 && _start)
            {
                _start = false;

                var rectTransforms = ChangelogListTransform.GetComponentsInChildren<ChangelogElement>()
                    .Select(s => s.GetComponent<RectTransform>()).ToArray();
                int k = -1;
                int delta = 0;
                float heightSpacing = 0;
                for (int i = 0; i < rectTransforms.Length; i++)
                {
                    if (VersionOfElements[i] != _realeasesElements.Count)
                    {
                        if (k < VersionOfElements[i])
                        {
                            k = VersionOfElements[i];
                            DebugLogger.Log(i + "Version: " + k);
                            _endPosition += heightSpacing;
                            _realeasesElements[k].StartPosition = _endPosition;

                            if (heightSpacing > 0)
                            {
                                
                                /*RectTransform rectTransform = (RectTransform) gameObjectSpacing.transform;
                                rectTransform.sizeDelta = new Vector2(20f, heightSpacing);*/
                                
                                GameObject spacing = Instantiate(SpacingPrefab, ChangelogListTransform, false);
                                spacing.transform.SetSiblingIndex(i + delta);
                                spacing.AddComponent<LayoutElement>().minHeight = heightSpacing;
                                delta++;
                            }

                            heightSpacing = 600;
                            DebugLogger.Log("TMP: " + _endPosition);
                        }

                        float heightOfElements = rectTransforms[i].rect.height + _spacingChangelogList;
                        heightSpacing -= heightOfElements;
                        if (heightSpacing < 40f)
                            heightSpacing = 40f;
                        _endPosition += heightOfElements;
                    }
                }

                SelectRelease();
            }
        }

        public void SelectRelease()
        {
            _changelogListPosition = ChangelogListTransform.localPosition.y;
            var select = _realeasesElements.LastOrDefault(element =>
                element.StartPosition <= ChangelogListTransform.localPosition.y) ?? _realeasesElements[0];

            int indexOfSelect = _realeasesElements.IndexOf(select);
            var next = indexOfSelect + 1 < _realeasesElements.Count
                ? _realeasesElements[_realeasesElements.IndexOf(select) + 1].StartPosition
                : _endPosition;

            _tolerance = next - select.StartPosition;
            Color color;
            ColorUtility.TryParseHtmlString("#57C962", out color);
            select.Image.color = color;

            DebugLogger.Log(ChangelogListTransform.localPosition.y + " Set release: " + select.VersionID + " " + select.StartPosition);
            if (_currentlySelected != null)
            {
                _currentlySelected.Image.color = Color.clear;
                DebugLogger.Log("Previous selected release: " + _currentlySelected.VersionID + " " + _currentlySelected.StartPosition);
            }

            _currentlySelected = select;

            RectTransform rectTransformRealeasesPrefab = (RectTransform) _currentlySelected.transform;
            Vector3 vector3 = transform.localPosition;
            vector3.y = (indexOfSelect) *
                        (_spacingRealeasesList +
                         rectTransformRealeasesPrefab.rect.height);
            _rectTransformRealeasesList.localPosition = vector3;
        }
    }
}