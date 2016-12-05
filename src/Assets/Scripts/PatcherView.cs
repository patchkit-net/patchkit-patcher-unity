using PatchKit.Unity.Patcher.Views;

namespace PatchKit.Unity.Patcher
{
    public class PatcherView
    {
        public readonly IGeneralProgressView GeneralProgressView;

        public readonly IDownloadProgressView DownloadProgressView;

        public PatcherView(IGeneralProgressView generalProgressView, IDownloadProgressView downloadProgressView)
        {
            GeneralProgressView = generalProgressView;
            DownloadProgressView = downloadProgressView;
        }
    }
}