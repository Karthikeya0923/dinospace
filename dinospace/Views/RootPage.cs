using System.Collections.Generic;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The app's home surface: five tabs hosted in a single page. Switching a
    // tab just flips which child is visible — instant, with none of the
    // multi-page scroll jank the old CarouselView had.
    public class RootPage : ContentPage
    {
        private readonly List<(string label, View view, Color accent)> _tabs = new();
        private readonly List<Border> _navItems = new();
        private readonly List<Label> _navLabels = new();
        private Grid _content = null!;
        private int _current = -1;

        // Lets pushed detail pages jump to a tab (e.g. "Ask Nova about this").
        public static RootPage? Current { get; private set; }
        public void SwitchTab(int index) => GoToTab(index);

        public RootPage()
        {
            Current = this;
            var home = new HomeView(GoToTab);
            var explore = new ExploreView();
            var nova = new NovaView();
            var play = new PlayView(GoToTab);
            var you = new YouView();

            _tabs.Add(("Home", home, Theme.AccentDino));
            _tabs.Add(("Explore", explore, Theme.AccentSpace));
            _tabs.Add(("Nova AI", nova, Theme.AccentNova));
            _tabs.Add(("Play", play, Theme.AccentDino));
            _tabs.Add(("You", you, Theme.AccentSpace));

            Build();
            GoToTab(0);
        }

        private void Build()
        {
            _content = new Grid();
            foreach (var (_, view, _) in _tabs)
            {
                view.IsVisible = false;
                _content.Add(view);
            }

            var nav = BuildNav();

            var root = new Grid { BackgroundColor = Theme.Bg };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Gradient backdrop sits behind the whole app.
            var bg = new Border
            {
                Background = new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb("#0B1224"), 0f),
                        new GradientStop(Color.FromArgb("#070B14"), 0.5f),
                        new GradientStop(Color.FromArgb("#05070E"), 1f),
                    },
                    new Point(0, 0), new Point(0, 1)),
                Stroke = Colors.Transparent,
                InputTransparent = true
            };
            Grid.SetRowSpan(bg, 2);

            root.Add(bg);
            root.Add(_content, 0, 0);
            root.Add(nav, 0, 1);
            Content = root;
        }

        private View BuildNav()
        {
            var grid = new Grid { Padding = new Thickness(10, 8, 10, 12), ColumnSpacing = 4 };
            for (int i = 0; i < _tabs.Count; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            for (int i = 0; i < _tabs.Count; i++)
            {
                int index = i;
                var label = new Label
                {
                    Text = _tabs[i].label,
                    FontFamily = Ui.Fonts,
                    FontSize = 12.5,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Theme.TextHint,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                };
                _navLabels.Add(label);

                var pill = new Border
                {
                    Content = label,
                    BackgroundColor = Colors.Transparent,
                    Stroke = Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = 14 },
                    Padding = new Thickness(6, 9),
                    HorizontalOptions = LayoutOptions.Fill
                };
                _navItems.Add(pill);

                Ui.OnTap(pill, (_, _) => GoToTab(index));
                grid.Add(pill, i, 0);
            }

            return new Border
            {
                Content = grid,
                BackgroundColor = Theme.BgRaised,
                Stroke = Theme.HairlineSoft,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(20, 20, 0, 0) }
            };
        }

        private void GoToTab(int index)
        {
            if (index < 0 || index >= _tabs.Count || index == _current) return;

            if (_current >= 0) _tabs[_current].view.IsVisible = false;
            _current = index;
            _tabs[index].view.IsVisible = true;

            for (int i = 0; i < _navItems.Count; i++)
            {
                bool on = i == index;
                var accent = _tabs[i].accent;
                _navItems[i].BackgroundColor = on ? Ui.MultiplyAlpha(accent, 0.16f) : Colors.Transparent;
                _navLabels[i].TextColor = on ? accent : Theme.TextHint;
            }

            (_tabs[index].view as ITabView)?.OnSelected();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_current >= 0)
                (_tabs[_current].view as ITabView)?.OnSelected();
        }

        // Hardware back: return to Home first, then let the system exit.
        protected override bool OnBackButtonPressed()
        {
            if (_current > 0) { GoToTab(0); return true; }
            return base.OnBackButtonPressed();
        }
    }
}
