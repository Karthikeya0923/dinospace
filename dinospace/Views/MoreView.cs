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
        public MoreView() => Build();

        public void OnSelected() { }

        private void Build()
        {
            var title = new Label
            {
                Text = "more",
                FontFamily = Ui.Display,
                FontSize = Ui.S(32),
                TextColor = Theme.TextPrimary,
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(18, 14, 18, 4)
            };

            var grid = new Grid { ColumnSpacing = 14, RowSpacing = 14, Padding = new Thickness(18, 8, 18, 16) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            // On a phone the tile rows are star-sized so they stretch to fill
            // the page with no dead space. On a big screen that stretch turns
            // the tiles into tall pills, so there they get a fixed card height
            // and sit at the top instead.
            bool wide = Ui.IsWideScreen;
            for (int r = 0; r < 4; r++)
                grid.RowDefinitions.Add(new RowDefinition { Height = wide ? new GridLength(190) : GridLength.Star });
            grid.RowDefinitions.Add(new RowDefinition { Height = wide ? new GridLength(110) : new GridLength(0.62, GridUnitType.Star) });
            if (wide) grid.VerticalOptions = LayoutOptions.Start;

            grid.Add(Tile(Ui.Icon(Ui.IconScanSky, 44), "scan sky", async () => await Nav.Push(() => new SkyPage())), 0, 0);
            grid.Add(Tile(Ui.Mascot("mascot_ask", 44, Ui.IconAsk), "ask nova", async () => await Nav.Push(() => new NovaPage())), 1, 0);
            grid.Add(Tile(Ui.Icon(Ui.IconBattles, 44), "dino battle", () => { RootPage.Current?.SwitchTab(2); return System.Threading.Tasks.Task.CompletedTask; }), 0, 1);
            grid.Add(Tile(Ui.Icon(Ui.IconDraw, 44), "draw entry", async () => await Nav.Push(() => new CreationsPage())), 1, 1);
            grid.Add(Tile(Ui.Icon(Ui.IconEncyclopedia, 44), "encyclopedia", () => { RootPage.Current?.SwitchTab(1); return System.Threading.Tasks.Task.CompletedTask; }), 0, 2);
            grid.Add(Tile(Ui.Icon(Ui.IconQuiz, 44), "quiz", async () => await Nav.Push(() => new QuizSetupPage())), 1, 2);
            grid.Add(Tile(Ui.Icon(Ui.IconCollections, 44), "collections", async () => await Nav.Push(() => new CollectionsListPage())), 0, 3);
            grid.Add(Tile(Ui.Icon(Ui.IconCollection, 44), "saved", () => { RootPage.Current?.SwitchTab(3); return System.Threading.Tasks.Task.CompletedTask; }), 1, 3);

            // settings: one wide tile across both columns, reaching the
            // bottom of the page like the sheet's long panel.
            var settings = Tile(Ui.Icon(Ui.IconSettings, 36), "settings",
                async () => await Nav.Push(() => new HostPage("settings", new SettingsView())), wide: true);
            grid.Add(settings, 0, 4);
            Grid.SetColumnSpan((BindableObject)settings, 2);

            var root = new Grid { RowSpacing = 0 };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            root.Add(title, 0, 0);
            root.Add(grid, 0, 1);
            Content = Ui.CapWidth(root);
        }

        private View Tile(View icon, string label, Func<System.Threading.Tasks.Task> onTap, bool wide = false)
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
                    FontSize = Ui.S(19),
                    TextColor = Theme.TextPrimary,
                    VerticalOptions = LayoutOptions.Center
                });
                content = row;
            }
            else
            {
                var col = new VerticalStackLayout
                {
                    Spacing = 12,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Center
                };
                var iconBox = new Grid { HeightRequest = 44 };
                iconBox.Add(icon);
                col.Add(iconBox);
                col.Add(new Label
                {
                    Text = label,
                    FontFamily = Ui.Display,
                    FontSize = Ui.S(17),
                    TextColor = Theme.TextPrimary,
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
