using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The list of curated collections, plus the user's own custom lists.
    // Rebuilt on every appearance so a list made or renamed a moment ago
    // shows up right away.
    public class CollectionsListPage : ContentPage
    {
        public CollectionsListPage()
        {
            Build();
            SwipeBack.Attach(this);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Build();
        }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(18, 4, 18, 24) };

            stack.Add(new Label
            {
                Text = "Collections",
                FontFamily = Ui.Display,
                FontSize = Ui.S(30),
                TextColor = Theme.TextPrimary
            });
            stack.Add(new Label
            {
                Text = "Curated, ranked lists to browse — or build your own.",
                FontFamily = Ui.Fonts,
                FontSize = Ui.S(14),
                LineHeight = 1.4,
                TextColor = Theme.TextSecondary,
                Margin = new Thickness(0, 0, 0, 6)
            });

            foreach (var c in CollectionData.All)
            {
                var id = c.Id;
                stack.Add(Row(c.Title, c.Subtitle, async () => await Nav.Push(() => new CollectionPage(id))));
            }

            // ----- the user's own lists -----
            stack.Add(Ui.SectionHeader("Your lists"));

            foreach (var l in CustomListStore.All())
            {
                string count = l.Entries.Count == 1 ? "1 entry" : $"{l.Entries.Count} entries";
                string sub = l.Subtitle.Length > 0 ? $"{l.Subtitle} · {count}" : count;
                var list = l;
                stack.Add(Row(l.Title.Length > 0 ? l.Title : "Untitled list", sub,
                    async () => await Nav.Push(() => new CustomListPage(list))));
            }

            // "make your own" — a friendly dashed card with a big plus
            var plusRow = new HorizontalStackLayout
            {
                Spacing = 10, HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = "＋", FontFamily = Ui.Display, FontSize = Ui.S(22), TextColor = Theme.Accent, VerticalOptions = LayoutOptions.Center },
                    new Label { Text = "Make your own list", FontFamily = Ui.Display, FontSize = Ui.S(18), TextColor = Theme.Accent, VerticalOptions = LayoutOptions.Center }
                }
            };
            var makeCard = new Border
            {
                Content = plusRow,
                BackgroundColor = Theme.AccentSoft,
                Stroke = Theme.Accent, StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 4, 4 },
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(16, 16)
            };
            Ui.OnTap(makeCard, async (_, _) => await CreateList());
            Ui.Describe(makeCard, "Make your own list of prehistoric creatures and space objects");
            stack.Add(makeCard);

            var body = Nav.DetailScaffoldFixed("", new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never });
            Content = Ui.PageRoot(body);
        }

        private View Row(string title, string subtitle, System.Action onTap)
        {
            var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            info.Add(new Label { Text = title, FontFamily = Ui.Display, FontSize = Ui.S(18), TextColor = Theme.TextPrimary });
            info.Add(new Label { Text = subtitle, FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), TextColor = Theme.TextSecondary });

            var chevron = Ui.Icon(Ui.IconChevron, 24, Theme.TextHint);
            chevron.VerticalOptions = LayoutOptions.Center;

            var grid = new Grid { ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(info, 0, 0);
            grid.Add(chevron, 1, 0);

            var card = Ui.Card(grid, radius: 16, padding: new Thickness(16, 14));
            Ui.OnTap(card, (_, _) => onTap());
            return card;
        }

        private async System.Threading.Tasks.Task CreateList()
        {
            string name = await DisplayPromptAsync("New list", "What's your list called?",
                "Create", "Cancel", placeholder: "Ultimate showdown squad", maxLength: 40);
            if (string.IsNullOrWhiteSpace(name)) return;
            var list = CustomListStore.Create(name);
            await Nav.Push(() => new CustomListPage(list), animated: false);
        }
    }
}
