using Ionic.Zip;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class AnalyticsBanner : Analytics<AnalyticsBanner>
    {
        public override void Display()
        {
            DisplayWithoutWait();
        }
        
        public void OnDisplay()
        {
            OnDisplayWithoutWait();
        }
    }
}