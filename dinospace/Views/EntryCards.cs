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
        public static View GridCard(string image, string title, string meta, Action onTap)
        {
            var img = new Image { Source = image, Aspect = Aspect.AspectFill, HeightRequest = 118 };
            var imgWrap = new Grid { HeightRequest = 118, BackgroundColor = Theme.ImgPlaceholder };
            imgWrap.Add(img);

            var name = new Label
            {
                Text = title,
                FontFamily = Ui.Display,
                FontSize = Ui.S(17),
                LineHeight = 1.12,
                TextColor = Theme.TextPrimary
            };
            var sub = new Label
            {
                Text = meta,
                FontFamily = Ui.Fonts,
                FontSize = Ui.S(12),
                TextColor = Theme.TextSecondary
            };

            var info = new VerticalStackLayout { Spacing = 5, Padding = new Thickness(12, 11, 12, 14) };
            info.Add(name);
            if (!string.IsNullOrEmpty(meta)) info.Add(sub);

            var col = new VerticalStackLayout { Spacing = 0 };
            col.Add(imgWrap);
            col.Add(info);

            var card = new Border
            {
                Content = col,
                BackgroundColor = Theme.Surface,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Padding = 0,
                Shadow = Theme.CardShadow()
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
        public static View ListRow(string image, string title, string meta, Action onTap)
        {
            var thumb = new Border
            {
                Content = new Image { Source = image, Aspect = Aspect.AspectFill, WidthRequest = 54, HeightRequest = 54 },
                WidthRequest = 54, HeightRequest = 54,
                BackgroundColor = Theme.ImgPlaceholder,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 10 }
            };

            var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            info.Add(new Label { Text = title, FontFamily = Ui.Display, FontSize = Ui.S(17), TextColor = Theme.TextPrimary });
            if (!string.IsNullOrEmpty(meta))
                info.Add(new Label { Text = meta, FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextSecondary, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation });

            var chevron = Ui.Icon(Ui.IconChevron, 22, Theme.TextHint);
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
    }
}
