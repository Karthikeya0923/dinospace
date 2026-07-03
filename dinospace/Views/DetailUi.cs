using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Shared building blocks for the dinosaur and space detail pages, so both
    // look identical in structure and only differ in content.
    public static class DetailUi
    {
        // Full-bleed hero: image, a dark gradient scrim, and overlaid title.
        public static View Hero(string image, string title, string pronunciation, IEnumerable<(string text, Color accent)> chips, Color accent)
        {
            var img = new Image { Source = image, Aspect = Aspect.AspectFill, HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill };
            Ui.Describe(img, title);

            var scrim = new Border
            {
                Background = new LinearGradientBrush(new GradientStopCollection
                {
                    new GradientStop(Colors.Transparent, 0f),
                    new GradientStop(Color.FromArgb("#33060A12"), 0.5f),
                    new GradientStop(Color.FromArgb("#F2060A12"), 1f),
                }, new Point(0, 0), new Point(0, 1)),
                Stroke = Colors.Transparent,
                InputTransparent = true
            };

            var chipRow = new HorizontalStackLayout { Spacing = 6 };
            foreach (var (text, ac) in chips)
                chipRow.Add(Ui.TintChip(text.ToUpperInvariant(), ac));

            var name = new Label { Text = title, FontFamily = Ui.Display, FontSize = Ui.S(30), TextColor = Theme.TextPrimary };
            var pron = new Label { Text = pronunciation, FontFamily = Ui.Fonts, FontSize = Ui.S(13), FontAttributes = FontAttributes.Italic, TextColor = Theme.TextSecondary };

            var overlay = new VerticalStackLayout { Spacing = 8, Padding = new Thickness(16, 0, 16, 16), VerticalOptions = LayoutOptions.End };
            overlay.Add(chipRow);
            overlay.Add(name);
            if (!string.IsNullOrWhiteSpace(pronunciation)) overlay.Add(pron);

            var grid = new Grid { HeightRequest = 300, BackgroundColor = Theme.ImgPlaceholder };
            grid.Add(img);
            grid.Add(scrim);
            grid.Add(overlay);
            return grid;
        }

        // A small stat pill (label above, value below) for the horizontal row.
        public static View StatChip(string label, string value, Color accent)
        {
            var col = new VerticalStackLayout { Spacing = 2 };
            col.Add(new Label { Text = label.ToUpperInvariant(), FontFamily = Ui.Fonts, FontSize = Ui.S(10), FontAttributes = FontAttributes.Bold, CharacterSpacing = 0.8, TextColor = accent });
            col.Add(new Label { Text = value, FontFamily = Ui.Display, FontSize = Ui.S(17), TextColor = Theme.TextPrimary });
            return new Border
            {
                Content = col,
                BackgroundColor = Theme.Surface,
                Stroke = Theme.HairlineSoft,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Padding = new Thickness(14, 10),
                MinimumWidthRequest = 96
            };
        }

        public static View StatChipRow(IEnumerable<(string label, string value, Color accent)> stats)
        {
            var row = new HorizontalStackLayout { Spacing = 10, Padding = new Thickness(0, 2) };
            foreach (var (label, value, accent) in stats)
                if (!string.IsNullOrWhiteSpace(value))
                    row.Add(StatChip(label, value, accent));
            return new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = row };
        }

        // A titled text section inside a card.
        public static View Section(string title, string body, Color accent)
        {
            if (string.IsNullOrWhiteSpace(body)) return new ContentView { IsVisible = false };
            var col = new VerticalStackLayout { Spacing = 8 };
            col.Add(TitleRow(title, accent));
            col.Add(new Label { Text = body, FontFamily = Ui.Fonts, FontSize = Ui.S(14.5), LineHeight = 1.45, TextColor = Theme.TextPrimary });
            return Card(col);
        }

        // Fun facts rendered as accent-dotted lines.
        public static View FunFacts(string funFacts, Color accent)
        {
            if (string.IsNullOrWhiteSpace(funFacts)) return new ContentView { IsVisible = false };
            var col = new VerticalStackLayout { Spacing = 8 };
            col.Add(TitleRow("Fun Facts", accent));

            foreach (var raw in funFacts.Split('\n'))
            {
                var line = raw.TrimStart('•', ' ').Trim();
                if (line.Length == 0) continue;
                var dot = new Border
                {
                    WidthRequest = 7, HeightRequest = 7, BackgroundColor = accent,
                    Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 4 },
                    Margin = new Thickness(0, 7, 0, 0), VerticalOptions = LayoutOptions.Start
                };
                var text = new Label { Text = line, FontFamily = Ui.Fonts, FontSize = Ui.S(14), LineHeight = 1.4, TextColor = Theme.TextPrimary };
                var row = new Grid { ColumnSpacing = 10 };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                row.Add(dot, 0, 0);
                row.Add(text, 1, 0);
                col.Add(row);
            }
            return Card(col);
        }

        public static View TitleRow(string title, Color accent)
        {
            var bar = new Border { WidthRequest = 4, HeightRequest = 18, BackgroundColor = accent, Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 2 }, VerticalOptions = LayoutOptions.Center };
            var label = new Label { Text = title, FontFamily = Ui.Display, FontSize = Ui.S(18), TextColor = Theme.TextPrimary, VerticalOptions = LayoutOptions.Center };
            return new HorizontalStackLayout { Spacing = 10, Children = { bar, label } };
        }

        public static Border Card(View content) => new()
        {
            Content = content,
            BackgroundColor = Theme.Surface,
            Stroke = Theme.HairlineSoft,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Padding = new Thickness(16)
        };

        // Floating top bar over the hero: back, save, share.
        public static View TopBar(bool saved, Action onBack, Action onSave, Action onShare, out Label saveIcon)
        {
            var back = RoundGlyph("‹", 30);
            Ui.OnTap(back, (_, _) => onBack(), haptic: false);
            Ui.Describe(back, "Go back");

            saveIcon = new Label
            {
                Text = saved ? "★" : "☆",
                FontSize = Ui.S(22),
                TextColor = saved ? Theme.AccentDino : Theme.TextPrimary,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            var saveBtn = RoundWrap(saveIcon);
            Ui.OnTap(saveBtn, (_, _) => onSave());
            Ui.Describe(saveBtn, saved ? "Remove bookmark" : "Save to bookmarks");

            var share = RoundGlyph("↗", 20);
            Ui.OnTap(share, (_, _) => onShare());
            Ui.Describe(share, "Share");

            var right = new HorizontalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.End };
            right.Add(saveBtn);
            right.Add(share);

            var grid = new Grid { Padding = new Thickness(12, 10) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(back, 0, 0);
            grid.Add(right, 2, 0);
            return grid;
        }

        private static Border RoundGlyph(string glyph, double size)
        {
            var label = new Label
            {
                Text = glyph,
                FontSize = Ui.S(size),
                TextColor = Theme.TextPrimary,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            return RoundWrap(label);
        }

        private static Border RoundWrap(View content) => new()
        {
            Content = content,
            WidthRequest = 40, HeightRequest = 40,
            BackgroundColor = Color.FromArgb("#99060A12"),
            Stroke = Theme.HairlineSoft, StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 20 }
        };

        // Primary "Ask Nova about this" call-to-action.
        public static View AskNovaButton(string name)
        {
            var label = new Label
            {
                Text = $"Ask Nova about {name}",
                FontFamily = Ui.Fonts, FontSize = Ui.S(15), FontAttributes = FontAttributes.Bold,
                TextColor = Theme.TextOnAccent, HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            var btn = new Border
            {
                Content = label,
                BackgroundColor = Theme.AccentNova,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(16, 14)
            };
            Ui.OnTap(btn, async (_, _) =>
            {
                NovaView.Ask($"Tell me an interesting fact about {name}.");
                try { while (Shell.Current.Navigation.NavigationStack.Count > 1) await Shell.Current.Navigation.PopAsync(false); } catch { }
                RootPage.Current?.SwitchTab(2);
            });
            return btn;
        }

        // Related entries strip (same domain).
        public static View Related(IEnumerable<(string image, string name, object data)> items, Color accent)
        {
            var row = new HorizontalStackLayout { Spacing = 12 };
            foreach (var (image, name, data) in items)
            {
                var img = new Image { Source = image, Aspect = Aspect.AspectFill, HeightRequest = 84, WidthRequest = 120 };
                var wrap = new Border
                {
                    Content = img, WidthRequest = 120, HeightRequest = 84,
                    BackgroundColor = Theme.ImgPlaceholder, Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 }
                };
                var label = new Label { Text = name, FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextSecondary, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation, WidthRequest = 120 };
                var col = new VerticalStackLayout { Spacing = 5, Children = { wrap, label } };
                Ui.OnTap(col, async (_, _) =>
                {
                    if (data is Dinosaur d) await Nav.OpenDino(d);
                    else if (data is SpaceObject s) await Nav.OpenSpace(s);
                });
                row.Add(col);
            }
            var section = new VerticalStackLayout { Spacing = 10 };
            section.Add(TitleRow("You might also like", accent));
            section.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = row });
            return section;
        }

        public static async Task ShareText(string text)
        {
            try { await Share.Default.RequestAsync(new ShareTextRequest { Text = text, Title = "DinoSpace" }); } catch { }
        }
    }
}
