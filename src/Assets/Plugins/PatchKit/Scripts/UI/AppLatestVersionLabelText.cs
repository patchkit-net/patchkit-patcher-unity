using System.Collections;
using PatchKit.Unity.Utilities;
using UnityEngine.UI;

namespace PatchKit.Unity.UI
{
    public class AppLatestVersionLabelText : AppCompontent
    {
        public Text Text;

        protected override IEnumerator LoadCoroutine()
        {
            yield return Threading.StartThreadCoroutine(() => ApiConnection.GetAppLatestAppVersion(AppSecret),
                response =>
                {
                    Text.text = response.Label;
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