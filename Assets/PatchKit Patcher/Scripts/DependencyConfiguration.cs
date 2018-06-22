using Autofac;
using PatchKit.Apps.Updating;
using PatchKit.Apps.Updating.AppData.Local;
using PatchKit.Apps.Updating.AppData.Remote.Downloaders;
using PatchKit.Apps.Updating.Utilities;
using PatchKit.Core.IO;
using PatchKit.Logging;
using PatchKit_Patcher.Scripts;
using UnityEngine;
using ILogger = PatchKit.Logging.ILogger;

namespace PatchKit.Patching.Unity
{
    public class DependencyConfiguration : MonoBehaviour
    {
        private void Awake()
        {
            UnityEngine.Debug.Log("Dependency injection configuration.");

            DependencyResolver.ContainerBuilder.RegisterModule(new Core.Properties.AssemblyModule(true));
            DependencyResolver.ContainerBuilder.RegisterModule(new Logging.Properties.AssemblyModule());
            DependencyResolver.ContainerBuilder.RegisterModule(new Network.Properties.AssemblyModule(1024));
            DependencyResolver.ContainerBuilder.RegisterModule(new Api.Properties.AssemblyModule());
            DependencyResolver.ContainerBuilder.RegisterModule(new Apps.Properties.AssemblyModule());
            DependencyResolver.ContainerBuilder.RegisterModule(new Apps.Updating.Properties.AssemblyModule());

            DependencyResolver.ContainerBuilder.RegisterType<DefaultLogger>()
                .SingleInstance()
                .As<ILogger>()
                .As<IMessagesStream>();

            DependencyResolver.ContainerBuilder.RegisterType<PlatformResolver>().As<IPlatformResolver>();
            DependencyResolver.ContainerBuilder.RegisterType<TorrentClientFactory>().As<ITorrentClientFactory>();
            DependencyResolver.ContainerBuilder.RegisterType<UnityTorrentClientProcessStartInfoProvider>()
                .As<ITorrentClientProcessStartInfoProvider>();
            DependencyResolver.ContainerBuilder.RegisterType<UnityCache>().As<ICache>();

            //DependencyResolver.RegisterType<IHttpClient, UnityHttpClient>();

            DependencyResolver.ContainerBuilder.RegisterType<UnityDiskSpaceChecker>().As<IDiskSpaceChecker>();

            DependencyResolver.Build();
        }
    }
}