using System;
using System.Collections.Generic;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The app's home surface: four tabs (Home / Search / Saved / Settings)
    // with an iOS-style icon tab bar, hosted in one page.
    //
    // Tab swiping is a real finger-tracking pager: MainActivity feeds raw
    // touches into HandlePan*, the current tab translates with the finger
    // while the neighbour slides in beside it, and release snaps to the
    // nearest tab with a smooth ease-out.
    public class RootPage : ContentPage
    {
        private readonly List<(string label, string icon, View view)> _tabs = new();
        private readonly List<Microsoft.Maui.Controls.Shapes.Path> _navIcons = new();
        private readonly List<Label> _navLabels = new();
        private Grid _content = null!;
        private int _current = -1;
        private double Width01 => Math.Max(1, _content.Width);

        // pan state
        private bool _panning;
        private int _panNeighbor = -1;

        public static RootPage? Current { get; private set; }
        public void SwitchTab(int index) => GoToTab(index);

        // Keeps the same tab across a theme rebuild.
        public static int LastTab;
        // Set by ThemesPage: it re-pushes itself after a rebuild and dissolves
        // the freeze-frame itself, so RootPage should leave the cover alone.
        public static bool HoldThemeCoverOnce;
        private Grid _rootGrid = null!;

        // The bottom system-bar (navigation/gesture) inset in DIPs, reported
        // from MainActivity. The tab bar pads its bottom by this so it fills the
        // area right down to the bottom edge of the screen — no gap.
        private static double _bottomInset;
        private VerticalStackLayout? _navBar;

        // Push the tab bar down into the navigation-bar area.
        public static void SetBottomInset(double bottom)
        {
            _bottomInset = bottom;
            Current?.ApplyBottomInset();
        }

        private void ApplyBottomInset()
        {
            if (_navBar != null)
                _navBar.Padding = new Thickness(0, 0, 0, _bottomInset);
        }

        public RootPage()
        {
            Current = this;
            var home = new HomeView(GoToTab);
            var search = new SearchView();
            var saved = new SavedView();
            var settings = new SettingsView();

            _tabs.Add(("Home", Ui.IconHome, home));
            _tabs.Add(("Search", Ui.IconSearch, search));
            _tabs.Add(("Saved", Ui.IconSaved, saved));
            _tabs.Add(("Settings", Ui.IconSettings, settings));

            Build();
            GoToTab(LastTab >= 0 && LastTab < _tabs.Count ? LastTab : 0);
        }

        private void Build()
        {
            _content = new Grid();
            foreach (var (_, _, view) in _tabs)
            {
                view.IsVisible = false;
                _content.Add(view);
            }

            var nav = BuildNav();

            var root = new Grid { BackgroundColor = Theme.Bg, RowSpacing = 0 };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            // Wallpaper themes draw their art behind the tabs too, under the
            // same readability wash every page uses.
            if (Theme.Wallpaper is string wp)
            {
                var art = new Image { Source = wp, Aspect = Aspect.AspectFill, InputTransparent = true };
                root.Add(art, 0, 0);
                Grid.SetRowSpan(art, 2);
                var dim = new BoxView { Color = Theme.WallpaperDim, InputTransparent = true };
                root.Add(dim, 0, 0);
                Grid.SetRowSpan(dim, 2);
            }
            root.Add(_content, 0, 0);
            root.Add(nav, 0, 1);
            _rootGrid = root;
            Content = root;
        }

        // The active-tab bubble backings (Playful only). Native leaves them
        // transparent so the bar stays flat and editorial.
        private readonly List<Border> _navCells = new();

        // ----- tab bar -----
        // Native: a flat white bar with a thin top rule, small icons + labels.
        // Playful: a chunkier bar with big icons and a rounded accent bubble
        // that sits behind whichever tab is selected.
        private View BuildNav()
        {
            bool playful = AppLayout.BubbleTabs;
            double iconSize = playful ? 28 : 26;

            var grid = new Grid { Padding = new Thickness(playful ? 10 : 6, playful ? 10 : 8, playful ? 10 : 6, playful ? 8 : 6), ColumnSpacing = playful ? 4 : 0 };
            for (int i = 0; i < _tabs.Count; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            _navCells.Clear();
            for (int i = 0; i < _tabs.Count; i++)
            {
                int index = i;
                var icon = Ui.Icon(_tabs[i].icon, iconSize, Theme.TextHint);
                var label = new Label
                {
                    Text = _tabs[i].label,
                    FontFamily = playful ? Ui.Display : Ui.Fonts,
                    FontSize = playful ? 12 : 11.5,
                    TextColor = Theme.TextHint,
                    HorizontalTextAlignment = TextAlignment.Center
                };
                _navIcons.Add(icon);
                _navLabels.Add(label);

                var cell = new VerticalStackLayout { Spacing = playful ? 2 : 3, Padding = new Thickness(0, playful ? 8 : 4) };
                cell.Add(icon);
                cell.Add(label);

                // In Playful each tab sits inside a rounded bubble that lights up
                // when active; in Native the bubble is invisible.
                var bubble = new Border
                {
                    Content = cell,
                    BackgroundColor = Colors.Transparent,
                    Stroke = Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = 18 }
                };
                Ui.OnTap(bubble, (_, _) => GoToTab(index));
                Ui.Describe(bubble, _tabs[i].label);
                _navCells.Add(bubble);
                grid.Add(bubble, i, 0);
            }

            _navBar = new VerticalStackLayout
            {
                Spacing = 0,
                BackgroundColor = Theme.BgRaised,
                Padding = new Thickness(0, 0, 0, _bottomInset),
            };
            if (!playful)
                _navBar.Children.Add(new BoxView { HeightRequest = 1, Color = Theme.Hairline });
            _navBar.Children.Add(grid);
            return _navBar;
        }

        private void GoToTab(int index)
        {
            if (index < 0 || index >= _tabs.Count || index == _current) return;
            EndPanVisuals();

            if (_current >= 0) _tabs[_current].view.IsVisible = false;
            _current = index;
            LastTab = index;
            var v = _tabs[index].view;
            v.TranslationX = 0;
            v.IsVisible = true;

            SyncNav();
            (v as ITabView)?.OnSelected();
        }

        private void SyncNav()
        {
            bool playful = AppLayout.BubbleTabs;
            for (int i = 0; i < _tabs.Count; i++)
            {
                bool on = i == _current;
                Ui.SetIcon(_navIcons[i], on && i == 2 ? Ui.IconSavedFill : _tabs[i].icon, on ? Theme.Accent : Theme.TextHint);
                _navLabels[i].TextColor = on ? Theme.Accent : Theme.TextHint;
                _navLabels[i].FontAttributes = on ? FontAttributes.Bold : FontAttributes.None;
                if (i < _navCells.Count)
                    _navCells[i].BackgroundColor = (playful && on) ? Theme.AccentSoft : Colors.Transparent;
            }
        }

        // ----- finger-tracking pager (fed by MainActivity) -----

        // True while a horizontal drag is captured; MainActivity uses this to
        // keep routing the gesture here.
        public bool CanPan()
        {
            try { return Shell.Current?.Navigation?.NavigationStack?.Count <= 1; }
            catch { return false; }
        }

        public void HandlePanStart()
        {
            if (!CanPan() || _current < 0) return;
            _panning = true;
            _panNeighbor = -1;
        }

        public void HandlePanMove(double dxPixels, double density)
        {
            if (!_panning || _current < 0) return;
            double dx = dxPixels / Math.Max(0.5, density);

            // Rubber-band at the ends.
            int dir = dx < 0 ? +1 : -1;
            int neighbor = _current + dir;
            if (neighbor < 0 || neighbor >= _tabs.Count) dx *= 0.25;

            var cur = _tabs[_current].view;
            cur.TranslationX = dx;

            // Show/position the neighbour sliding in from the side.
            if (neighbor >= 0 && neighbor < _tabs.Count)
            {
                if (_panNeighbor != neighbor)
                {
                    if (_panNeighbor >= 0 && _panNeighbor != _current)
                        _tabs[_panNeighbor].view.IsVisible = false;
                    _panNeighbor = neighbor;
                    var nv = _tabs[neighbor].view;
                    nv.IsVisible = true;
                    (nv as ITabView)?.OnSelected();
                }
                _tabs[neighbor].view.TranslationX = dir > 0 ? Width01 + dx : -Width01 + dx;
            }
            else if (_panNeighbor >= 0)
            {
                _tabs[_panNeighbor].view.IsVisible = false;
                _panNeighbor = -1;
            }
        }

        public async void HandlePanEnd(double dxPixels, double velocityPxPerMs, double density)
        {
            if (!_panning || _current < 0) return;
            _panning = false;

            double dx = dxPixels / Math.Max(0.5, density);
            double w = Width01;
            int dir = dx < 0 ? +1 : -1;
            int neighbor = _current + dir;
            bool neighborValid = neighbor >= 0 && neighbor < _tabs.Count;

            // Commit if dragged past 1/3 of the screen or flicked fast.
            bool commit = neighborValid && (Math.Abs(dx) > w / 3.0 || Math.Abs(velocityPxPerMs) / Math.Max(0.5, density) > 0.55);

            var cur = _tabs[_current].view;
            if (commit)
            {
                var nv = _tabs[neighbor].view;
                uint ms = 190;
                var t1 = cur.TranslateToAsync(dir > 0 ? -w : w, 0, ms, Easing.CubicOut);
                var t2 = nv.TranslateToAsync(0, 0, ms, Easing.CubicOut);
                await System.Threading.Tasks.Task.WhenAll(t1, t2);

                cur.IsVisible = false;
                cur.TranslationX = 0;
                _current = neighbor;
                // Swipes must update this too, or a theme rebuild jumps back
                // to whichever tab was last *tapped* (usually Home).
                LastTab = neighbor;
                _panNeighbor = -1;
                AppSettings.Tap();
                SyncNav();
            }
            else
            {
                uint ms = 170;
                var tasks = new List<System.Threading.Tasks.Task> { cur.TranslateToAsync(0, 0, ms, Easing.CubicOut) };
                if (_panNeighbor >= 0 && _panNeighbor != _current)
                {
                    var nv = _tabs[_panNeighbor].view;
                    tasks.Add(nv.TranslateToAsync(dir > 0 ? w : -w, 0, ms, Easing.CubicOut));
                }
                await System.Threading.Tasks.Task.WhenAll(tasks);
                if (_panNeighbor >= 0 && _panNeighbor != _current)
                {
                    _tabs[_panNeighbor].view.IsVisible = false;
                    _tabs[_panNeighbor].view.TranslationX = 0;
                }
                _panNeighbor = -1;
            }
        }

        private void EndPanVisuals()
        {
            _panning = false;
            if (_panNeighbor >= 0 && _panNeighbor != _current)
            {
                _tabs[_panNeighbor].view.IsVisible = false;
                _tabs[_panNeighbor].view.TranslationX = 0;
            }
            _panNeighbor = -1;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Re-theme the system chrome here — on THIS freshly built page —
            // instead of in the toggle handler. During a theme switch this runs
            // under the cross-fade cover, so the window background, status bar,
            // and navigation bar never flash on the old screen before the swap.
            ThemeFx.SetWindowBackground(Theme.Bg);
            ThemeFx.ApplySystemBars();
            if (Application.Current != null)
                Application.Current.UserAppTheme = Theme.IsDark ? AppTheme.Dark : AppTheme.Light;

            // If a theme switch just rebuilt the app, the previous screen is
            // frozen on top at the native level — dissolve it away now that the
            // new UI is on screen. No-op on a normal appearance. (ThemesPage
            // handles its own dissolve after it re-pushes itself.)
            if (HoldThemeCoverOnce) HoldThemeCoverOnce = false;
            else ThemeFx.FadeOutThemeCover();

            if (_current >= 0)
                (_tabs[_current].view as ITabView)?.OnSelected();
        }

        protected override bool OnBackButtonPressed()
        {
            if (_current > 0) { GoToTab(0); return true; }
            return base.OnBackButtonPressed();
        }
    }
}
