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
        private static readonly IUnityContainer Container = new UnityContainer();

        static DependencyResolver()
        {
            RegisterLogger();

            Container
                .RegisterType<ITorrentClientProcessStartInfoProvider, UnityTorrentClientProcessStartInfoProvider>();

            Container.RegisterType<ITorrentClient, TorrentClient>();

            Container.RegisterInstance<ITorrentClientFactory>(
                new TorrentClientFactory(() => Container.Resolve<ITorrentClient>()));

            Container.RegisterType<IHttpClient, UnityHttpClient>();

            // We are overriding IHttpClient to DefaultHttpClient since it works better for downloaders
            Container.RegisterInstance<IBaseHttpDownloader>(new BaseHttpDownloader(new DefaultHttpClient(),
                Container.Resolve<ILogger>()));

            Container.RegisterType<IRsyncFilePatcher, RsyncFilePatcher>();
        }

        private static void RegisterLogger()
        {
            var logger = new DefaultLogger(new DefaultMessageSourceStackLocator());
            Container.RegisterInstance<ILogger>(logger);
            Container.RegisterInstance<IMessagesStream>(logger);
        }

        public static T Resolve<T>()
        {
            return Container.Resolve<T>();
        }
    }
}