using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The list of curated collections, pushed from Home's Play section.
    public class CollectionsListPage : ContentPage
    {
        public CollectionsListPage()
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
                Text = "Curated, ranked lists to browse when you're not sure where to start.",
                FontFamily = Ui.Fonts,
                FontSize = Ui.S(14),
                LineHeight = 1.4,
                TextColor = Theme.TextSecondary,
                Margin = new Thickness(0, 0, 0, 6)
            });

            foreach (var c in CollectionData.All)
            {
                var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
                info.Add(new Label { Text = c.Title, FontFamily = Ui.Display, FontSize = Ui.S(18), TextColor = Theme.TextPrimary });
                info.Add(new Label { Text = c.Subtitle, FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), TextColor = Theme.TextSecondary });

                var chevron = Ui.Icon(Ui.IconChevron, 24, Theme.TextHint);
                chevron.VerticalOptions = LayoutOptions.Center;

                var grid = new Grid { ColumnSpacing = 12 };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.Add(info, 0, 0);
                grid.Add(chevron, 1, 0);

                var card = Ui.Card(grid, radius: 16, padding: new Thickness(16, 14));
                var id = c.Id;
                Ui.OnTap(card, async (_, _) => await Nav.Push(() => new CollectionPage(id)));
                stack.Add(card);
            }

            var body = Nav.DetailScaffold("", new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never }, Theme.Accent, out _);
            Content = Ui.PageRoot(body);
            SwipeBack.Attach(this);
        }
    }
}
