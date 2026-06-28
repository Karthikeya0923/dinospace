namespace dinospace
{
    // Implemented by each tab view so the host can tell it "you're now visible"
    // (ContentViews have no OnAppearing).
    public interface ITabView
    {
        void OnSelected();
    }

    public partial class MainTabsPage : ContentPage
    {
        private readonly string[] _names = { "Home", "Explore", "Saved", "Settings" };
        private readonly List<View> _tabs = new List<View>();
        private readonly List<Label> _tabLabels = new List<Label>();

        public MainTabsPage()
        {
            InitializeComponent();

            _tabs.Add(new MainPage());
            _tabs.Add(new ExplorePage());
            _tabs.Add(new SavedPage());
            _tabs.Add(new SettingsPage());
            Pager.ItemsSource = _tabs;

            BuildTabBar();
            Highlight(0);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            int pos = Pager.Position;
            if (pos >= 0 && pos < _tabs.Count)
                (_tabs[pos] as ITabView)?.OnSelected();
        }

        private void BuildTabBar()
        {
            for (int i = 0; i < _names.Length; i++)
            {
                int index = i;
                var label = new Label
                {
                    Text = _names[i],
                    FontSize = 12,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    TextColor = Theme.TextHint
                };
                _tabLabels.Add(label);

                var cell = new Grid { Children = { label } };
                var tap = new TapGestureRecognizer();
                tap.Tapped += (s, e) => Pager.Position = index;
                cell.GestureRecognizers.Add(tap);

                TabBar.Add(cell, i, 0);
            }
        }

        private void OnPositionChanged(object sender, PositionChangedEventArgs e)
        {
            int pos = e.CurrentPosition;
            Highlight(pos);
            if (pos >= 0 && pos < _tabs.Count)
                (_tabs[pos] as ITabView)?.OnSelected();
        }

        private void Highlight(int index)
        {
            for (int i = 0; i < _tabLabels.Count; i++)
            {
                _tabLabels[i].TextColor = (i == index) ? Theme.Accent : Theme.TextHint;
                _tabLabels[i].FontAttributes = (i == index) ? FontAttributes.Bold : FontAttributes.None;
            }
        }

        // ===== Android: suppress the system back-gesture on the screen edges =====
        // so swiping at the very edge changes tabs instead of exiting the app.

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
#if ANDROID
            if (Width > 0 && Height > 0)
                ApplyEdgeExclusion(Width, Height);
#endif
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
#if ANDROID
            if (width > 0 && height > 0)
                ApplyEdgeExclusion(width, height);
#endif
        }

#if ANDROID
        private void ApplyEdgeExclusion(double dipWidth, double dipHeight)
        {
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Q) return;
            if (Handler?.PlatformView is not Android.Views.View native) return;

            float density = native.Context?.Resources?.DisplayMetrics?.Density ?? 2.75f;
            int w = (int)(dipWidth * density);
            int h = (int)(dipHeight * density);
            if (w <= 0 || h <= 0) return;

            int strip = (int)(36 * density);
            native.SystemGestureExclusionRects = new System.Collections.Generic.List<Android.Graphics.Rect>
            {
                new Android.Graphics.Rect(0, 0, strip, h),
                new Android.Graphics.Rect(w - strip, 0, w, h),
            };
        }
#endif
    }
}