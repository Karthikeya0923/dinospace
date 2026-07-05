using System;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // A ranked, curated list (e.g. "Biggest Creatures Ever"). Rows show a rank
    // medal, thumbnail, name, and the stat this list is ordered by.
    public class CollectionPage : ContentPage
    {
        public CollectionPage(string collectionId)
        {
            var collection = CollectionData.ById(collectionId);
            var accent = collection?.Domain == "Space" ? Theme.AccentSpace : Theme.AccentDino;
            Build(collection, CollectionData.Entries(collectionId), accent);
            SwipeBack.Attach(this);
        }

        private void Build(Collection? c, System.Collections.Generic.List<CollectionEntry> entries, Color accent)
        {
            var stack = new VerticalStackLayout { Spacing = 8, Padding = new Thickness(16, 4, 16, 24) };

            stack.Add(new Label { Text = c?.Title ?? "Collection", FontFamily = Ui.Display, FontSize = Ui.S(26), TextColor = Theme.TextPrimary });
            stack.Add(new Label { Text = c?.Subtitle ?? "", FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), TextColor = Theme.TextSecondary, Margin = new Thickness(0, 0, 0, 8) });

            int rank = 1;
            foreach (var e in entries)
                stack.Add(Row(rank++, e, accent));

            var content = Nav.DetailScaffoldFixed("", new ScrollView { Content = stack });
            Content = Ui.PageRoot(content);
        }

        private View Row(int rank, CollectionEntry e, Color accent)
        {
            Color medal = rank switch { 1 => Color.FromArgb("#FFD24A"), 2 => Color.FromArgb("#C9D4EE"), 3 => Color.FromArgb("#E0A46A"), _ => Theme.TextHint };
            var rankLabel = new Label
            {
                Text = rank.ToString(), FontFamily = Ui.Display, FontSize = Ui.S(18),
                TextColor = rank <= 3 ? Theme.TextOnAccent : Theme.TextSecondary,
                HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center
            };
            var rankBadge = new Border
            {
                WidthRequest = 34, HeightRequest = 34,
                BackgroundColor = rank <= 3 ? medal : Theme.SurfaceAlt,
                Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 10 },
                VerticalOptions = LayoutOptions.Center, Content = rankLabel
            };

            var img = new Image { Source = e.Image, Aspect = Aspect.AspectFill, WidthRequest = 52, HeightRequest = 52 };
            var thumb = new Border
            {
                Content = img, WidthRequest = 52, HeightRequest = 52,
                BackgroundColor = Colors.Transparent, Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 12 }
            };

            var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            info.Add(new Label { Text = e.Name, FontFamily = Ui.Display, FontSize = Ui.S(16), TextColor = Theme.TextPrimary });
            info.Add(new Label { Text = e.StatText, FontFamily = Ui.Fonts, FontSize = Ui.S(13), FontAttributes = FontAttributes.Bold, TextColor = accent });

            var grid = new Grid { ColumnSpacing = 12, VerticalOptions = LayoutOptions.Center };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.Add(rankBadge, 0, 0); grid.Add(thumb, 1, 0); grid.Add(info, 2, 0);

            var card = new Border
            {
                Content = grid,
                BackgroundColor = Theme.Surface, Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(10), Margin = new Thickness(0, 4)
            };
            Ui.OnTap(card, async (_, _) =>
            {
                if (e.Data is Dinosaur d) await Nav.OpenDino(d);
                else if (e.Data is SpaceObject s) await Nav.OpenSpace(s);
            });
            return card;
        }
    }
}
