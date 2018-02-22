using System;
using JetBrains.Annotations;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public class TorrentClientFactory : ITorrentClientFactory
    {
        private readonly Func<ITorrentClient> _createFunc;

        public TorrentClientFactory([NotNull] Func<ITorrentClient> createFunc)
        {
            if (createFunc == null)
            {
                throw new ArgumentNullException("createFunc");
            }

            _createFunc = createFunc;
        }

        public ITorrentClient Create()
        {
            return _createFunc();
        }
    }
}