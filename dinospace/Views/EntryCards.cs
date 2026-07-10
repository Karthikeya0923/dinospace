using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The editorial entry card used on Home, Browse, and Search: image on
    // top, serif title and quiet meta below, white card, soft shadow.
    // Cards auto-size vertically so titles never get clipped.
    public static class EntryCards
    {
        // Shown behind every entry image: a small window of night sky with the
        // entry's initial in gold. Entries whose art hasn't arrived yet look
        // intentional instead of like an empty beige box; when the image loads
        // it simply covers this.
        public static View ArtFallback(string title, double letterSize, bool stars = true)
        {
            var grid = new Grid { BackgroundColor = Color.FromArgb("#111527") };
            if (stars)
            {
                int seed = 0;
                foreach (char c in title) seed = seed * 31 + c;   // stable per entry
                grid.Add(new GraphicsView { Drawable = new StarFieldDrawable { Seed = seed }, InputTransparent = true });
            }
            grid.Add(new Label
            {
                Text = string.IsNullOrEmpty(title) ? "•" : title[..1].ToUpperInvariant(),
                FontFamily = Ui.Display,
                FontSize = letterSize,
                TextColor = Color.FromArgb("#E3BE55"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            });
            return grid;
        }

        // A user drawing, shown WHOLE. AspectFill crops a tall canvas into a
        // wide slot — kids kept losing the head and feet of their creature —
        // so drawings letterbox with AspectFit on the exact canvas colour they
        // were painted on, and the bars blend invisibly into the picture.
        public static View Drawing(string imagePath, string bgHex, double height = -1)
        {
            Color bg;
            try { bg = Color.FromArgb(string.IsNullOrWhiteSpace(bgHex) ? "#FFFFFF" : bgHex); }
            catch { bg = Colors.White; }
            var grid = new Grid { BackgroundColor = bg };
            if (height > 0) grid.HeightRequest = height;
            grid.Add(new Image { Source = ImageSource.FromFile(imagePath), Aspect = Aspect.AspectFit });
            return grid;
        }

        // A bright gradient stand-in for the Playful layout — a cheerful splash
        // of colour with the entry's initial, instead of the quiet night sky.
        public static View PlayfulArt(string title, double letterSize)
        {
            var grid = new Grid { Background = PlayfulKit.GradientFill(title) };
            grid.Add(new Label
            {
                Text = string.IsNullOrEmpty(title) ? "•" : title[..1].ToUpperInvariant(),
                FontFamily = Ui.Display, FontSize = letterSize,
                TextColor = Colors.White.WithAlpha(0.92f),
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
            });
            return grid;
        }

        // Fixed heights so every card is identical whether the name is one or
        // two lines. The title area always reserves two lines and the meta
        // sits pinned at the bottom, so "Late Cretaceous" never gets clipped
        // and short-named cards don't leave an awkward gap.
        public static View GridCard(string image, string title, string meta, Action onTap)
        {
            if (AppLayout.Playful) return PlayfulGridCard(image, title, meta, onTap);
            double imgH = 118;
            var img = new Image { Source = image, Aspect = Aspect.AspectFill, HeightRequest = imgH };
            var imgWrap = new Grid { HeightRequest = imgH };
            // stars:false — a plain label fallback keeps grids of cards cheap.
            imgWrap.Add(ArtFallback(title, 30, stars: false));
            imgWrap.Add(img);

            var name = new Label
            {
                Text = title,
                FontFamily = Ui.Display,
                FontSize = Ui.S(16.5),
                LineHeight = 1.1,
                MaxLines = 2,
                LineBreakMode = LineBreakMode.TailTruncation,
                TextColor = Theme.TextPrimary
            };
            var sub = new Label
            {
                Text = meta,
                FontFamily = Ui.Fonts,
                FontSize = Ui.S(12),
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation,
                TextColor = Theme.TextSecondary
            };

            // Title and meta grouped together right under the image, so a short
            // name no longer leaves a big gap above its meta line. The card
            // still has a fixed height so a two-line name never clips, just a
            // little shorter than before.
            var info = new VerticalStackLayout { Padding = new Thickness(12, 8, 12, 9), Spacing = 5, VerticalOptions = LayoutOptions.Start };
            info.Add(name);
            if (!string.IsNullOrEmpty(meta)) info.Add(sub);

            var col = new VerticalStackLayout { Spacing = 0 };
            col.Add(imgWrap);
            col.Add(info);

            var card = new Border
            {
                Content = col,
                BackgroundColor = Theme.Surface,
                Stroke = Theme.CardStroke,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Padding = 0,
                HeightRequest = 196,
                Shadow = Theme.CardShadow()
            };
            Ui.OnTap(card, (_, _) => onTap());
            Ui.Describe(card, title);
            return card;
        }

        // Playful card: big rounded corners, a bright gradient image frame, a
        // chunky rounded name and a colourful little meta pill. Built for
        // tapping and for delighting a five-year-old.
        private static View PlayfulGridCard(string image, string title, string meta, Action onTap)
        {
            var hue = PlayfulKit.GradientFor(title);
            double imgH = 124;
            var img = new Image { Source = image, Aspect = Aspect.AspectFill, HeightRequest = imgH };
            var imgGrid = new Grid { HeightRequest = imgH };
            imgGrid.Add(PlayfulArt(title, 34));
            imgGrid.Add(img);
            var imgWrap = new Border
            {
                Content = imgGrid, HeightRequest = imgH,
                Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 18 }
            };

            var name = new Label
            {
                Text = title, FontFamily = Ui.Display, FontSize = Ui.S(16.5), LineHeight = 1.05,
                MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation, TextColor = Theme.TextPrimary
            };

            var info = new VerticalStackLayout { Padding = new Thickness(11, 9, 11, 11), Spacing = 6, VerticalOptions = LayoutOptions.Start };
            info.Add(name);
            if (!string.IsNullOrEmpty(meta))
                info.Add(new Border
                {
                    BackgroundColor = hue.a.WithAlpha(0.16f), Stroke = Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = 100 }, Padding = new Thickness(9, 3),
                    HorizontalOptions = LayoutOptions.Start,
                    Content = new Label { Text = meta, FontFamily = Ui.Fonts, FontSize = Ui.S(11), FontAttributes = FontAttributes.Bold, TextColor = PlayfulKit.OnSurface(hue.a), MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation }
                });

            var col = new VerticalStackLayout { Spacing = 0, Padding = new Thickness(6, 6, 6, 0) };
            col.Add(imgWrap);
            col.Add(info);

            var card = new Border
            {
                Content = col, BackgroundColor = Theme.Surface, Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 24 }, Padding = 0,
                HeightRequest = 214, Shadow = Theme.CardShadow()
            };
            Ui.OnTap(card, (_, _) => onTap());
            Ui.Describe(card, title);
            return card;
        }

        // Two-column grid of entry cards (plain grid; rows auto-size).
        public static View TwoColumn(IEnumerable<(string image, string title, string meta, Action onTap)> items)
        {
            var grid = new Grid { ColumnSpacing = 12, RowSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            int i = 0;
            foreach (var (image, title, meta, onTap) in items)
            {
                int row = i / 2, col = i % 2;
                if (col == 0) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.Add(GridCard(image, title, meta, onTap), col, row);
                i++;
            }
            return grid;
        }

        // Compact list row for Search and Saved: thumb, serif name, meta, chevron.
        public static View ListRow(string image, string title, string meta, Action onTap, bool goldStar = false)
        {
            if (AppLayout.Playful) return PlayfulListRow(image, title, meta, onTap, goldStar);
            var thumbGrid = new Grid();
            thumbGrid.Add(ArtFallback(title, 20, stars: false));
            thumbGrid.Add(new Image { Source = image, Aspect = Aspect.AspectFill, WidthRequest = 54, HeightRequest = 54 });
            var thumb = new Border
            {
                Content = thumbGrid,
                WidthRequest = 54, HeightRequest = 54,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 10 }
            };

            var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            info.Add(new Label { Text = title, FontFamily = Ui.Display, FontSize = Ui.S(17), TextColor = Theme.TextPrimary });
            if (!string.IsNullOrEmpty(meta))
                info.Add(new Label { Text = meta, FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextSecondary, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation });

            var chevron = Ui.Icon(Ui.IconChevron, 22);
            chevron.VerticalOptions = LayoutOptions.Center;

            var row = new Grid { ColumnSpacing = 12, Padding = new Thickness(2, 10) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Add(thumb, 0, 0);
            row.Add(info, 1, 0);
            row.Add(chevron, 2, 0);

            var wrap = new VerticalStackLayout { Spacing = 0 };
            wrap.Add(row);
            wrap.Add(new BoxView { HeightRequest = 1, Color = Theme.HairlineSoft, Margin = new Thickness(66, 0, 0, 0) });
            Ui.OnTap(wrap, (_, _) => onTap());
            return wrap;
        }

        // Playful list row: a rounded, tappable "pill card" with a bright round
        // thumbnail and a chunky name — no thin hairline rows.
        private static View PlayfulListRow(string image, string title, string meta, Action onTap, bool goldStar = false)
        {
            var thumbGrid = new Grid();
            thumbGrid.Add(PlayfulArt(title, 22));
            thumbGrid.Add(new Image { Source = image, Aspect = Aspect.AspectFill, WidthRequest = 58, HeightRequest = 58 });
            var thumb = new Border
            {
                Content = thumbGrid, WidthRequest = 58, HeightRequest = 58,
                Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 18 }
            };

            var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            info.Add(new Label { Text = title, FontFamily = Ui.Display, FontSize = Ui.S(17.5), TextColor = Theme.TextPrimary });
            if (!string.IsNullOrEmpty(meta))
                info.Add(new Label { Text = meta, FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextSecondary, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation });

            // saved rows carry the gold star from the design sheet instead
            // of a chevron
            var chevron = goldStar
                ? Ui.Icon(Ui.IconStar, 22)
                : Ui.Icon(Ui.IconChevron, 22);
            chevron.VerticalOptions = LayoutOptions.Center;

            var row = new Grid { ColumnSpacing = 13, Padding = new Thickness(10, 9) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Add(thumb, 0, 0);
            row.Add(info, 1, 0);
            row.Add(chevron, 2, 0);

            var card = new Border
            {
                Content = row, BackgroundColor = Theme.Surface, Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 20 }, Padding = 0,
                Margin = new Thickness(0, 5), Shadow = Theme.CardShadow()
            };
            Ui.OnTap(card, (_, _) => onTap());
            Ui.Describe(card, title);
            return card;
        }
    }
}
