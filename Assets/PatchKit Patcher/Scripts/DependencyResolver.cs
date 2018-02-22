using Microsoft.Practices.Unity;
using PatchKit.Logging;
using PatchKit.Network;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders;

namespace PatchKit.Unity.Patcher
{
    public static class DependencyResolver
    {
        private static readonly IUnityContainer _container = new UnityContainer();

        static DependencyResolver()
        {
            RegisterLogger();

            _container
                .RegisterType<ITorrentClientProcessStartInfoProvider, UnityTorrentClientProcessStartInfoProvider>();
            _container.RegisterType<ITorrentClient, TorrentClient>();
            _container.RegisterType<IHttpClient, UnityHttpClient>();

            // We are overriding IHttpClient to DefaultHttpClient since it works better for downloaders
            _container.RegisterInstance<IBaseHttpDownloader>(new BaseHttpDownloader(new DefaultHttpClient(),
                _container.Resolve<ILogger>()));

            _container.RegisterType<IRsyncFilePatcher, RsyncFilePatcher>();
        }

        private static void RegisterLogger()
        {
            var logger = new DefaultLogger(new DefaultMessageSourceStackLocator());
            _container.RegisterInstance<ILogger>(logger);
            _container.RegisterInstance<IMessagesStream>(logger);
        }

        public static T Resolve<T>()
        {
            return _container.Resolve<T>();
        }
    }
}