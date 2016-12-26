namespace PatchKit.Unity.Patcher
{
    internal class PatcherStrategyResolver : IPatcherStrategyResolver
    {
        public IPatcherStrategy Resolve(PatcherContext context)
        {
            if (context.Data.LocalData.IsInstalled())
            {
                if (GetDiffCost(context) < GetContentCost(context))
                {
                    return new PatcherDiffStrategy(context);
                }
            }

            return new PatcherContentStrategy(context);
        }

        private ulong GetContentCost(PatcherContext context)
        {
            int latestVersionId = context.Data.RemoteData.MetaData.GetLatestVersionId();

            var contentSummary = context.Data.RemoteData.MetaData.GetContentSummary(latestVersionId);

            return (ulong)contentSummary.Size;
        }

        private ulong GetDiffCost(PatcherContext context)
        {
            int latestVersionId = context.Data.RemoteData.MetaData.GetLatestVersionId();
            int currentLocalVersionId = context.Data.LocalData.GetInstalledVersion();

            ulong cost = 0;

            for (int i = currentLocalVersionId + 1; i <= latestVersionId; i++)
            {
                var diffSummary = context.Data.RemoteData.MetaData.GetDiffSummary(i);
                cost += (ulong)diffSummary.Size;
            }

            return cost;
        }
    }
}