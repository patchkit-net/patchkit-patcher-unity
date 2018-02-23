using System.Collections;
using UnityEngine.UI;

namespace PatchKit.Patching.Unity.UI
{
    public class AppLatestVersionLabelText : AppCompontent
    {
        public Text Text;

        protected override IEnumerator LoadCoroutine()
        {
            yield return UnityThreading.StartThreadCoroutine(() => MainApiConnection.GetAppLatestAppVersion(AppSecret),
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