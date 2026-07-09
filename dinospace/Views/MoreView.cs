using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The Playful layout's "more" tab, straight from the design sheet: a
    // centred lowercase title and a two-column grid of outlined tiles — scan
    // sky, ask dino, dino battle, draw entry, encyclopedia, quiz, collections
    // and saved — plus a quiet settings gear in the corner.
    public class MoreView : ContentView, ITabView
    {
        public MoreView() => Build();

        public void OnSelected() { }

        private void Build()
        {
            var header = new Grid { Padding = new Thickness(18, 18, 18, 6) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var title = new Label
            {
                Text = "more",
                FontFamily = Ui.Display,
                FontSize = Ui.S(24),
                TextColor = Theme.TextPrimary,
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            };
            header.Add(title, 0, 0);
            Grid.SetColumnSpan(title, 2);

            var gear = Ui.Icon(Ui.IconSettings, 24, Theme.TextSecondary);
            var gearWrap = new Border
            {
                Content = gear,
                WidthRequest = 42, HeightRequest = 42,
                BackgroundColor = Colors.Transparent,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 21 },
                VerticalOptions = LayoutOptions.Center
            };
            // SettingsView draws its own big lowercase title, so the host
            // page's bar stays title-less — just the back arrow.
            Ui.OnTap(gearWrap, async (_, _) =>
                await Nav.Push(() => new HostPage("", new SettingsView())));
            Ui.Describe(gearWrap, "Settings");
            header.Add(gearWrap, 1, 0);

            var grid = new Grid { ColumnSpacing = 14, RowSpacing = 14, Padding = new Thickness(18, 8, 18, 24) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            for (int r = 0; r < 4; r++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            grid.Add(Tile(Ui.IconTelescope, null, "scan sky", async () => await Nav.Push(() => new SkyViewPage())), 0, 0);
            grid.Add(Tile(null, "🦕", "ask dino", async () => await Nav.Push(() => new NovaPage())), 1, 0);
            grid.Add(Tile(Ui.IconSwords, null, "dino battle", () => { RootPage.Current?.SwitchTab(2); return System.Threading.Tasks.Task.CompletedTask; }), 0, 1);
            grid.Add(Tile(Ui.IconPencil, null, "draw entry", async () => await Nav.Push(() => new CreationsPage())), 1, 1);
            grid.Add(Tile(Ui.IconBook, null, "encyclopedia", () => { RootPage.Current?.SwitchTab(1); return System.Threading.Tasks.Task.CompletedTask; }), 0, 2);
            grid.Add(Tile(Ui.IconQuiz, null, "quiz", async () => await Nav.Push(() => new QuizSetupPage())), 1, 2);
            grid.Add(Tile(Ui.IconStar, null, "collections", async () => await Nav.Push(() => new CollectionsListPage())), 0, 3);
            grid.Add(Tile(Ui.IconSaved, null, "saved", () => { RootPage.Current?.SwitchTab(3); return System.Threading.Tasks.Task.CompletedTask; }), 1, 3);

            var stack = new VerticalStackLayout { Spacing = 0 };
            stack.Add(header);
            stack.Add(grid);
            Content = new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
        }

        private View Tile(string? icon, string? emoji, string label, Func<System.Threading.Tasks.Task> onTap)
        {
            var col = new VerticalStackLayout
            {
                Spacing = 10,
                Padding = new Thickness(10, 20),
                HorizontalOptions = LayoutOptions.Fill
            };
            if (icon != null)
            {
                var ic = Ui.Icon(icon, 30, Theme.TextPrimary);
                ic.HorizontalOptions = LayoutOptions.Center;
                col.Add(ic);
            }
            else
                col.Add(new Label { Text = emoji, FontSize = 26, HorizontalTextAlignment = TextAlignment.Center });

            col.Add(new Label
            {
                Text = label,
                FontFamily = Ui.Display,
                FontSize = Ui.S(15),
                TextColor = Theme.TextPrimary,
                HorizontalTextAlignment = TextAlignment.Center
            });

            var tile = new Border
            {
                Content = col,
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

    // A pushed page around any tab-style view (used for settings in the
    // Playful layout, where settings lives behind the "more" gear instead of
    // owning a tab).
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
