using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class RealeasesElement : MonoBehaviour
    {
        public TextMeshProUGUI Text;
        public Button Button;
        public Image Image;
        public float StartPosition;
        public int VersionID { get; set; }

        public void SetButton(NewChangelogList changelogList, RealeasesList realeasesList)
        {
            Button.onClick.AddListener(() =>
            {
                if (realeasesList.CurrentlySelected.VersionID != VersionID)
                {
                    Vector3 vector3 = changelogList.transform.localPosition;
                    vector3.y = StartPosition;
                    UnityEngine.Debug.Log(StartPosition + " version: " + Text.text);
                    changelogList.Scrolling(changelogList, vector3, realeasesList.SelectRelease);
                }
            });
        }
    }
}
