namespace PatchKit.Unity.UI
{
    public abstract class AppCompontent : RefreshableComponent
    {
        private string _previousAppSecret;

        public string AppSecret;

        protected override void Update()
        {
            if (_previousAppSecret != AppSecret)
            {
                _previousAppSecret = AppSecret;

                Refresh();
            }

            base.Update();
        }
    }
}
