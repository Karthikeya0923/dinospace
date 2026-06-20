namespace dinospace
{
    public partial class WhatsNewPage : ContentPage
    {
        public WhatsNewPage()
        {
            InitializeComponent();
            VersionLabel.Text = $"Current version: {AppInfo.VersionString}";
        }
    }
}