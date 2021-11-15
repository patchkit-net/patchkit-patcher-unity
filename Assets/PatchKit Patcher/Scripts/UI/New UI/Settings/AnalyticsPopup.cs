using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class AnalyticsPopup : Analytics<AnalyticsPopup>
    {
        public override void Display()
        {
            base.Display(CancellationToken.Empty);
        }
    }
}