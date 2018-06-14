using PatchKit.Apps.Updating;
using PatchKit.Apps.Updating.AppData.Local;
using PatchKit.Apps.Updating.AppData.Remote.Downloaders;
using PatchKit.Apps.Updating.Utilities;
using PatchKit.Core.Collections.Immutable;
using PatchKit.Core.DependencyInjection;
using PatchKit.Network;

namespace PatchKit.Patching.Unity
{
    public class DependencyConfiguration
    {
        public static void Execute()
        {
            UnityEngine.Debug.Log("Dependency injection configuration.");
            RegisterLogger();

            ProcessBindings(Apps.Updating.Properties.AssemblyModule.Container.Bindings);

            DependencyResolver.RegisterType<IPlatformResolver, PlatformResolver>();
            DependencyResolver.RegisterType<ITorrentClientProcessStartInfoProvider, UnityTorrentClientProcessStartInfoProvider>();
            DependencyResolver.RegisterType<IHttpClient, UnityHttpClient>();
            DependencyResolver.RegisterType<ICache, UnityCache>();
        }

        private static void ProcessBindings(ImmutableArray<Binding> bindings)
        {
            foreach (Binding binding in bindings)
            {
                DependencyResolver.RegisterType(binding.Type, binding.TargetType);
            }
        }

        private static void RegisterLogger()
        {
            var logger = new Logging.DefaultLogger(new Logging.DefaultMessageSourceStackLocator()); 
            DependencyResolver.RegisterInstance<Logging.ILogger>(logger); 
            DependencyResolver.RegisterInstance<Logging.IMessagesStream>(logger); 
        }
    }
}