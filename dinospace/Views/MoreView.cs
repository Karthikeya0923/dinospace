using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The "more" tab, straight from the design sheet: a big centred
    // lowercase title and a two-column grid of tiles — scan sky, ask
    // novasaur, dino battle, draw entry, encyclopedia, quiz, collections and
    // saved — with settings as one wide tile reaching the bottom of the
    // page. The grid stretches so there is no dead space under settings.
    public class MoreView : ContentView, ITabView
    {
        private bool _built;
        private bool _short;      // the layout mode currently on screen

        public MoreView()
        {
            Build(false);
            // Rotating the phone changes which layout fits, so rebuild the
            // grid whenever the page crosses the short/tall line.
            SizeChanged += (_, _) =>
            {
                bool now = Ui.IsShort(Height);
                if (!_built || now != _short) Build(now);
            };
        }

        public void OnSelected() { }

        private void Build(bool shortScreen)
        {
            _built = true;
            _short = shortScreen;
            var title = new Label
            {
                Text = "more",
                FontFamily = Ui.Display,
                FontSize = Ui.S(shortScreen ? 22 : 32),
                TextColor = Theme.TextPrimary,
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = shortScreen ? new Thickness(18, 4, 18, 0) : new Thickness(18, 14, 18, 4)
            };

            // A phone in landscape is WIDER than the 600dp tablet mark but
            // only ~410dp tall, so sizing by width alone handed it the
            // tablet's tall fixed rows and pushed most of the tiles off the
            // bottom with nothing to scroll. It has width to spare and no
            // height, so there the grid turns on its side: four short columns
            // instead of two tall ones, which fits every tile and the
            // settings bar on one screen with nothing to reach for.
            bool wide = Ui.IsWideScreen && !shortScreen;
            // Landscape leaves roughly 300dp between the status bar and the
            // tab bar, so the two tile rows and the settings bar are sized to
            // land inside that with a little room to spare.
            int cols = shortScreen ? 4 : 2;
            double tileH = shortScreen ? 88 : 190;
            double settingsH = shortScreen ? 54 : 110;

            var grid = new Grid
            {
                ColumnSpacing = shortScreen ? 10 : 14,
                RowSpacing = shortScreen ? 8 : 14,
                Padding = shortScreen ? new Thickness(16, 2, 16, 6) : new Thickness(18, 8, 18, 16)
            };
            for (int c = 0; c < cols; c++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            int tileRows = (8 + cols - 1) / cols;
            for (int r = 0; r < tileRows; r++)
                grid.RowDefinitions.Add(new RowDefinition
                {
                    Height = (wide || shortScreen) ? new GridLength(tileH) : GridLength.Star
                });
            grid.RowDefinitions.Add(new RowDefinition
            {
                Height = (wide || shortScreen) ? new GridLength(settingsH) : new GridLength(0.62, GridUnitType.Star)
            });
            if (wide || shortScreen) grid.VerticalOptions = LayoutOptions.Start;

            var entries = new (View icon, string label, Func<System.Threading.Tasks.Task> tap)[]
            {
                (Ui.Icon(Ui.IconScanSky, 44), "scan sky", async () => await Nav.Push(() => new SkyPage())),
                (Ui.Mascot("mascot_nova", 44, Ui.IconAsk), "ask nova", async () => { if (await ParentMode.GateNova()) await Nav.Push(() => new NovaPage()); }),
                (Ui.Icon(Ui.IconBattles, 44), "dino battle", () => { RootPage.Current?.SwitchTab(2); return System.Threading.Tasks.Task.CompletedTask; }),
                (Ui.Icon(Ui.IconDraw, 44), "draw entry", async () => await Nav.Push(() => new CreationsPage())),
                (Ui.Icon(Ui.IconEncyclopedia, 44), "encyclopedia", () => { RootPage.Current?.SwitchTab(1); return System.Threading.Tasks.Task.CompletedTask; }),
                (Ui.Icon(Ui.IconQuiz, 44), "quiz", async () => await Nav.Push(() => new QuizSetupPage())),
                (Ui.Icon(Ui.IconCollections, 44), "collections", async () => await Nav.Push(() => new CollectionsListPage())),
                (Ui.Icon(Ui.IconCollection, 44), "saved", () => { RootPage.Current?.SwitchTab(3); return System.Threading.Tasks.Task.CompletedTask; }),
            };
            for (int i = 0; i < entries.Length; i++)
                grid.Add(Tile(entries[i].icon, entries[i].label, entries[i].tap, compact: shortScreen), i % cols, i / cols);

            // settings: one wide tile across the full width, reaching the
            // bottom of the page like the sheet's long panel.
            var settings = Tile(Ui.Icon(Ui.IconSettings, 36), "settings",
                async () => await Nav.Push(() => new HostPage("settings", new SettingsView())), wide: true, compact: shortScreen);
            grid.Add(settings, 0, tileRows);
            Grid.SetColumnSpan((BindableObject)settings, cols);

            // The tablet's fixed rows can still add up to more than the window
            // holds, so that layout scrolls. Landscape now fits by design, and
            // portrait stretches to fit — neither needs to.
            View tiles = wide
                ? new ScrollView { Content = grid, VerticalScrollBarVisibility = ScrollBarVisibility.Never }
                : grid;

            var root = new Grid { RowSpacing = 0 };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            root.Add(title, 0, 0);
            root.Add(tiles, 0, 1);
            // Landscape needs its full width for four columns; capping it to a
            // reading column is what squeezed the tiles in the first place.
            Content = shortScreen ? root : Ui.CapWidth(root);
        }

        private View Tile(View icon, string label, Func<System.Threading.Tasks.Task> onTap, bool wide = false, bool compact = false)
        {
            icon.HorizontalOptions = LayoutOptions.Center;

            View content;
            if (wide)
            {
                // icon and text side by side, centred in the long panel
                var row = new HorizontalStackLayout
                {
                    Spacing = 14,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };
                icon.VerticalOptions = LayoutOptions.Center;
                row.Add(icon);
                row.Add(new Label
                {
                    Text = label,
                    FontFamily = Ui.Display,
                    FontSize = Ui.S(compact ? 17 : 19),
                    TextColor = Theme.TextPrimary,
                    VerticalOptions = LayoutOptions.Center
                });
                content = row;
            }
            else
            {
                var col = new VerticalStackLayout
                {
                    Spacing = compact ? 6 : 12,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Center
                };
                var iconBox = new Grid { HeightRequest = compact ? 34 : 44 };
                iconBox.Add(icon);
                col.Add(iconBox);
                col.Add(new Label
                {
                    Text = label,
                    FontFamily = Ui.Display,
                    FontSize = Ui.S(compact ? 14.5 : 17),
                    TextColor = Theme.TextPrimary,
                    MaxLines = 1,
                    LineBreakMode = LineBreakMode.TailTruncation,
                    HorizontalTextAlignment = TextAlignment.Center
                });
                content = col;
            }

            var tile = new Border
            {
                Content = content,
                BackgroundColor = Theme.Surface,
                Stroke = Theme.CardStroke,
                StrokeThickness = 1.2,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Shadow = Theme.CardShadow()
            };
            Ui.OnTap(tile, async (_, _) => await onTap());
            Ui.Describe(tile, label);
            return tile;
        }
    }

    // A pushed page around any tab-style view (settings lives behind the
    // "more" grid instead of owning a tab).
    public class HostPage : ContentPage
    {
        public HostPage(string title, View body)
        {
            Content = Ui.PageRoot(Nav.DetailScaffoldFixed(Ui.T(title), body));
            Shell.SetNavBarIsVisible(this, false);
            SwipeBack.Attach(this);
        }
    }
}
