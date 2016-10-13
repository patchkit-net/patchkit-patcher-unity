using System.Collections;
using PatchKit.Unity.Utilities;
using UnityEngine.UI;

namespace PatchKit.Unity.UI
{
    public class AppLatestVersionChangelogText : AppCompontent
    {
        public Text Text;

        protected override IEnumerator LoadCoroutine()
        {
            yield return ApiConnection.GetCoroutine(string.Format("1/apps/{0}/versions/latest", AppSecret), null,
                response =>
                {
                    Text.text = response.GetJson().Value<string>("changelog");
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