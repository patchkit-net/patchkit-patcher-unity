using PatchKit.Api;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class UnityWebRequestFactory : IHttpWebRequestFactory
    {
        public IHttpWebRequest Create(string url)
        {
            return new WrapRequest(url);
        }
    }
}