namespace PatchKit.Unity.Patcher
{
    public class PatcherContext
    {
        public readonly PatcherData Data;

        public readonly PatcherConfiguration Configuration;

        public readonly PatcherView View;

        public PatcherContext(PatcherData data, PatcherConfiguration configuration, PatcherView view)
        {
            Data = data;
            Configuration = configuration;
            View = view;
        }
    }
}