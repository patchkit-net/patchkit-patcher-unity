using PatchKit.Api;
using PatchKit.Apps.Updating;
using PatchKit.Apps.Updating.AppData.Local;
using PatchKit.Apps.Updating.AppData.Remote.Downloaders;
using PatchKit.Apps.Updating.Utilities;
using PatchKit.Core.Collections.Immutable;
using PatchKit.Core.DependencyInjection;
using PatchKit.Network;
using UnityEngine;

namespace PatchKit.Patching.Unity
{
    public class DependencyConfiguration : MonoBehaviour
    {
        private void Awake()
        {
            UnityEngine.Debug.Log("Dependency injection configuration.");
            RegisterLogger();

            DependencyResolver.RegisterType<IPlatformResolver, PlatformResolver>();
            DependencyResolver.RegisterType<ITorrentClientFactory, TorrentClientFactory>();
            DependencyResolver
                .RegisterType<ITorrentClientProcessStartInfoProvider, UnityTorrentClientProcessStartInfoProvider>();
            //DependencyResolver.RegisterType<IHttpClient, UnityHttpClient>();
            DependencyResolver.RegisterType<ICache, UnityCache>();

            DependencyResolver.Build();
        }

        private static void RegisterLogger()
        {
            var logger = new Logging.DefaultLogger(new Logging.MessageSourceStackLocator());
            DependencyResolver.RegisterInstance<Logging.ILogger>(logger);
            DependencyResolver.RegisterInstance<Logging.IMessagesStream>(logger);
        }
    }
}