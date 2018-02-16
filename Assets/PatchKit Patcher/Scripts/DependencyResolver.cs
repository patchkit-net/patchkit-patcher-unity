using Microsoft.Practices.Unity;
using PatchKit.Logging;

namespace PatchKit.Unity.Patcher
{
    public static class DependencyResolver
    {
        private static readonly IUnityContainer _container = new UnityContainer();

        static DependencyResolver()
        {
            RegisterLogger();
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