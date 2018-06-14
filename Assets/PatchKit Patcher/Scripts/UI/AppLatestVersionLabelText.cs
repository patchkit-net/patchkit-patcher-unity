using System.Collections;
using UnityEngine.UI;

namespace PatchKit.Patching.Unity.UI
{
    public class AppLatestVersionLabelText : AppCompontent
    {
        public Text Text;

        protected override IEnumerator LoadCoroutine()
        {
            yield return UnityThreading.StartThreadCoroutine(() => ApiConnection.GetAppLatestAppVersion(AppSecret, null, CancellationToken),
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