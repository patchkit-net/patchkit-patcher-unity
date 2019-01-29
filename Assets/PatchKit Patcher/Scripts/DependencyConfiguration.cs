using Autofac;
using PatchKit.Apps.Updating;
using PatchKit.Core.CSharp;
using PatchKit.Core.Utilities;
using PatchKit.Logging;
using PatchKit.Patching.Unity.Debug;
using UnityEngine;
using ILogger = PatchKit.Logging.ILogger;

namespace PatchKit.Patching.Unity
{
    public class DependencyConfiguration : MonoBehaviour
    {
        private void Awake()
        {
            UnityEngine.Debug.Log("Dependency injection configuration.");

            Configure();
        }

        private static object _logLock = new object();

        private static void Log(LogMessage logMessage)
        {
            lock (_logLock)
            {
                switch (logMessage.Type)
                {
                    case LogMessageType.Trace:
                        UnityEngine.Debug.Log(logMessage.Message);
                        break;
                    case LogMessageType.Info:
                        UnityEngine.Debug.Log(logMessage.Message);
                        break;
                    case LogMessageType.Warning:
                        UnityEngine.Debug.LogWarning(logMessage.Message);
                        break;
                    case LogMessageType.Error:
                        UnityEngine.Debug.LogError(logMessage.Message);
                        break;
                }

                if (logMessage.Exception != null)
                {
                    UnityEngine.Debug.LogException(logMessage.Exception);
                }
            }
        }

        public static void Configure()
        {
            var coreModule = new PatchKit.Core.Properties.AssemblyModule(_ => { });
            var networkModule = new PatchKit.Network.Properties.AssemblyModule(coreModule);
            var apiModule = new PatchKit.Api.Properties.AssemblyModule(coreModule, networkModule);
            var appsModule = new PatchKit.Apps.Properties.AssemblyModule(coreModule);
            var appsUpdatingModule =
                new PatchKit.Apps.Updating.Properties.AssemblyModule(coreModule, networkModule, appsModule);

            DependencyResolver.ContainerBuilder.RegisterModule(new PatchKit.Logging.Properties.AssemblyModule());
            DependencyResolver.ContainerBuilder.RegisterType<DefaultLogger>().As<ILogger>().As<IMessagesStream>()
                .SingleInstance();
            DependencyResolver.ContainerBuilder.RegisterModule(coreModule);
            DependencyResolver.ContainerBuilder.RegisterModule(networkModule);
            DependencyResolver.ContainerBuilder.RegisterModule(apiModule);
            DependencyResolver.ContainerBuilder.RegisterModule(appsModule);
            DependencyResolver.ContainerBuilder.RegisterModule(appsUpdatingModule);

            DependencyResolver.ContainerBuilder.RegisterType<PlatformResolver>().As<IPlatformResolver>();

            DependencyResolver.Build();

            //DependencyResolver.Resolve<IMessagesStream>()
             //   .Subscribe(new UnityMessageWriter(new SimpleMessageFormatter()));
        }
    }
}