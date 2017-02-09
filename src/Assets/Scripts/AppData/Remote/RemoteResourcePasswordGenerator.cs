using System;
using System.Text;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class RemoteResourcePasswordGenerator
    {
        public string Generate(string appSecret, int versionId)
        {
            string hash = appSecret + versionId;
            byte[] hashBytes = Encoding.UTF8.GetBytes(hash);
            return '\x08'.ToString() + '\x07' + '\x18' + '\x24' + Convert.ToBase64String(hashBytes);
        }
    }
}