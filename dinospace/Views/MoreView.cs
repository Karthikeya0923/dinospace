using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The "more" tab, straight from the design sheet: a centred lowercase
    // title and a two-column grid of outlined tiles — scan sky, ask novasaur,
    // dino battle, draw entry, encyclopedia, quiz, collections and saved —
    // with settings as one wide tile across the bottom.
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
                FontSize = Ui.S(24),
                TextColor = Theme.TextPrimary,
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(18, 18, 18, 6)
            };

            var grid = new Grid { ColumnSpacing = 14, RowSpacing = 14, Padding = new Thickness(18, 8, 18, 24) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            for (int r = 0; r < 5; r++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            grid.Add(Tile(Ui.Sticker("st_telescope.png", 32), "scan sky", async () => await Nav.Push(() => new SkyViewPage())), 0, 0);
            grid.Add(Tile(Ui.Mascot("mascot_ask", 32, "st_bub_green_dots.png"), "ask novasaur", async () => await Nav.Push(() => new NovaPage())), 1, 0);
            grid.Add(Tile(PathIcon(Ui.IconSwords), "dino battle", () => { RootPage.Current?.SwitchTab(2); return System.Threading.Tasks.Task.CompletedTask; }), 0, 1);
            grid.Add(Tile(PathIcon(Ui.IconPencil), "draw entry", async () => await Nav.Push(() => new CreationsPage())), 1, 1);
            grid.Add(Tile(PathIcon(Ui.IconBook), "encyclopedia", () => { RootPage.Current?.SwitchTab(1); return System.Threading.Tasks.Task.CompletedTask; }), 0, 2);
            grid.Add(Tile(Ui.Sticker("st_icon_question.png", 32), "quiz", async () => await Nav.Push(() => new QuizSetupPage())), 1, 2);
            grid.Add(Tile(Ui.Sticker("st_badge_star.png", 32), "collections", async () => await Nav.Push(() => new CollectionsListPage())), 0, 3);
            grid.Add(Tile(PathIcon(Ui.IconSaved), "saved", () => { RootPage.Current?.SwitchTab(3); return System.Threading.Tasks.Task.CompletedTask; }), 1, 3);

            // settings: one wide tile across both columns, like the sheet's
            // long panel — lives down here instead of a gear in the corner.
            var settings = Tile(PathIcon(Ui.IconSettings), "settings",
                async () => await Nav.Push(() => new HostPage("", new SettingsView())), wide: true);
            grid.Add(settings, 0, 4);
            Grid.SetColumnSpan((BindableObject)settings, 2);

            var stack = new VerticalStackLayout { Spacing = 0 };
            stack.Add(title);
            stack.Add(grid);
            Content = new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
        }

        private static View PathIcon(string name)
        {
            var ic = Ui.Icon(name, 30, Theme.TextPrimary);
            ic.HorizontalOptions = LayoutOptions.Center;
            return ic;
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
                    Spacing = 12,
                    Padding = new Thickness(10, 18),
                    HorizontalOptions = LayoutOptions.Center
                };
                icon.VerticalOptions = LayoutOptions.Center;
                row.Add(icon);
                row.Add(new Label
                {
                    Text = label,
                    FontFamily = Ui.Display,
                    FontSize = Ui.S(15),
                    TextColor = Theme.TextPrimary,
                    VerticalOptions = LayoutOptions.Center
                });
                content = row;
            }
            else
            {
                var col = new VerticalStackLayout
                {
                    Spacing = 10,
                    Padding = new Thickness(10, 20),
                    HorizontalOptions = LayoutOptions.Fill
                };
                var iconBox = new Grid { HeightRequest = 32 };
                iconBox.Add(icon);
                col.Add(iconBox);
                col.Add(new Label
                {
                    Text = label,
                    FontFamily = Ui.Display,
                    FontSize = Ui.S(15),
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
