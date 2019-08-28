using UnityEngine;

namespace PatchKit.Unity.Patcher.UI
{
    public class LoadingIndicatorAnimation : MonoBehaviour
    {
        private void Update()
        {
            transform.localEulerAngles += new Vector3(0f, 0f, Time.deltaTime * 180f);
        }
    }
}