using System.Collections.Generic;
using PatchKit.Api.Models.Main;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public struct RemoteResource
    {
        public string[] TorrentUrls;

        public ResourceUrl[] ResourceUrls;

        public long Size;

        public string HashCode;

        public ChunksData ChunksData;

        public override string ToString()
        {
            return "urls: {" + string.Join(", ", GetUrls()) + "}\n" +
                   "torrent urls: {" + string.Join(", ", TorrentUrls) + "}\n" +
                   "size: " + Size + "\n" +
                   "hashcode: " + HashCode;
        }

        public bool HasMetaUrls()
        {
            return ResourceUrls != null && ResourceUrls.Length > 0 && !string.IsNullOrEmpty(ResourceUrls[0].MetaUrl);
        }

        public string[] GetMetaUrls()
        {
            var urls = new List<string>();
            
            foreach (ResourceUrl resourceUrl in ResourceUrls)
            {
                if (!string.IsNullOrEmpty(resourceUrl.MetaUrl))
                {
                    urls.Add(resourceUrl.MetaUrl);
                }
            }

            return urls.ToArray();
        }

        public string[] GetUrls()
        {
            var urls = new List<string>();
                        
            foreach (ResourceUrl resourceUrl in ResourceUrls)
            {
                if (!string.IsNullOrEmpty(resourceUrl.Url))
                {
                    urls.Add(resourceUrl.Url);
                }
            }

            return urls.ToArray();
        }
    }
}