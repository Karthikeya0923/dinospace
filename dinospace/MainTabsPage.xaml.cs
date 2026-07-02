namespace dinospace
{
    public interface ITabView
    {
        void OnSelected();
    }

    public partial class MainTabsPage : ContentPage
    {
        private readonly string[] _names = { "Home", "Explore", "Saved", "Settings" };
        private readonly List<View> _tabs = new List<View>();
        private readonly List<Label> _tabLabels = new List<Label>();
        private bool _warmedUp = false;

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

            // Quietly pre-build the heavy pages (DinoPedia, SpacePedia, Ask AI)
            // while the home screen sits idle, so tapping them opens instantly.
            if (!_warmedUp)
            {
                _warmedUp = true;
                PageCache.Warmup(Dispatcher);
            }
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
                tap.Tapped += (s, e) => GoToTab(index);
                cell.GestureRecognizers.Add(tap);

                TabBar.Add(cell, i, 0);
            }
        }

        // Tab-bar tap: jump WITHOUT the animated multi-page scroll (that scroll was the jank),
        // then refresh the landed tab one frame later so the switch paints instantly first.
        private void GoToTab(int index)
        {
            if (index == Pager.Position) return;

            // Instant cut to the target page — no slide across intermediate pages.
            Pager.ScrollTo(index, position: Microsoft.Maui.Controls.ScrollToPosition.Center, animate: false);

            Highlight(index);

            Dispatcher.Dispatch(() =>
            {
                if (index >= 0 && index < _tabs.Count)
                    (_tabs[index] as ITabView)?.OnSelected();
            });
        }

        private void OnPositionChanged(object sender, PositionChangedEventArgs e)
        {
            int pos = e.CurrentPosition;
            Highlight(pos);
            // Swipe path: defer the rebuild a frame so the swipe settles before heavy work.
            Dispatcher.Dispatch(() =>
            {
                if (pos >= 0 && pos < _tabs.Count)
                    (_tabs[pos] as ITabView)?.OnSelected();
            });
        }

        private void Highlight(int index)
        {
            for (int i = 0; i < _tabLabels.Count; i++)
            {
                _tabLabels[i].TextColor = (i == index) ? Theme.Accent : Theme.TextHint;
                _tabLabels[i].FontAttributes = (i == index) ? FontAttributes.Bold : FontAttributes.None;
            }
        }

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
