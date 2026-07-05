using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Shared building blocks for the dinosaur and space detail pages —
    // editorial style: clean full-bleed image (no scrim), serif headline
    // below it, caps+rule section headers with body text on the paper.
    public static class DetailUi
    {
        // Plain hero image. No overlays, no gradient boxes — the photo breathes.
        public static View Hero(string image, string title)
        {
            var img = new Image { Source = image, Aspect = Aspect.AspectFill, HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill };
            Ui.Describe(img, title);
            var grid = new Grid { HeightRequest = 320 };
            grid.Add(EntryCards.ArtFallback(title, 56));
            grid.Add(img);
            return grid;
        }

        // The headline block that sits under the hero.
        public static View TitleBlock(string title, string pronunciation, string meta)
        {
            var col = new VerticalStackLayout { Spacing = 6 };
            col.Add(new Label
            {
                Text = title,
                FontFamily = Ui.Display,
                FontSize = Ui.S(32),
                LineHeight = 1.05,
                TextColor = Theme.TextPrimary
            });
            if (!string.IsNullOrWhiteSpace(pronunciation))
                col.Add(new Label
                {
                    Text = pronunciation,
                    FontFamily = Ui.DisplayItalic,
                    FontSize = Ui.S(15),
                    TextColor = Theme.TextSecondary
                });
            if (!string.IsNullOrWhiteSpace(meta))
                col.Add(new Label
                {
                    Text = meta,
                    FontFamily = Ui.Fonts,
                    FontSize = Ui.S(13),
                    TextColor = Theme.TextSecondary,
                    Margin = new Thickness(0, 4, 0, 0)
                });
            return col;
        }

        public static View StatChip(string label, string value, Color accent)
        {
            var col = new VerticalStackLayout { Spacing = 3 };
            col.Add(new Label
            {
                Text = label.ToUpperInvariant(),
                FontFamily = Ui.Fonts, FontSize = Ui.S(10), FontAttributes = FontAttributes.Bold,
                CharacterSpacing = 1.2, TextColor = Theme.Accent
            });
            col.Add(new Label
            {
                Text = value,
                FontFamily = Ui.Display, FontSize = Ui.S(17), TextColor = Theme.TextPrimary
            });
            return new Border
            {
                Content = col,
                BackgroundColor = Theme.Surface,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Padding = new Thickness(14, 11),
                MinimumWidthRequest = 96,
                Shadow = Theme.CardShadow()
            };
        }

        public static View StatChipRow(IEnumerable<(string label, string value, Color accent)> stats)
        {
            var row = new HorizontalStackLayout { Spacing = 10, Padding = new Thickness(2, 4, 2, 8) };
            foreach (var (label, value, accent) in stats)
                if (!string.IsNullOrWhiteSpace(value))
                    row.Add(StatChip(label, value, accent));
            return new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = row };
        }

        // Caps+rule header with the body text directly on the paper.
        public static View Section(string title, string body, Color accent)
        {
            if (string.IsNullOrWhiteSpace(body)) return new ContentView { IsVisible = false };
            var col = new VerticalStackLayout { Spacing = 10 };
            col.Add(Ui.SectionHeader(title));
            col.Add(new Label
            {
                Text = body,
                FontFamily = Ui.Fonts,
                FontSize = Ui.S(15),
                LineHeight = 1.55,
                TextColor = Theme.TextPrimary
            });
            return col;
        }

        public static View FunFacts(string funFacts, Color accent)
        {
            if (string.IsNullOrWhiteSpace(funFacts)) return new ContentView { IsVisible = false };
            var col = new VerticalStackLayout { Spacing = 12 };
            col.Add(Ui.SectionHeader("Fun facts"));

            foreach (var raw in funFacts.Split('\n'))
            {
                var line = raw.TrimStart('•', ' ').Trim();
                if (line.Length == 0) continue;
                var dot = new Border
                {
                    WidthRequest = 7, HeightRequest = 7, BackgroundColor = Theme.Accent,
                    Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 4 },
                    Margin = new Thickness(0, 8, 0, 0), VerticalOptions = LayoutOptions.Start
                };
                var text = new Label { Text = line, FontFamily = Ui.Fonts, FontSize = Ui.S(14.5), LineHeight = 1.5, TextColor = Theme.TextPrimary };
                var row = new Grid { ColumnSpacing = 12 };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                row.Add(dot, 0, 0);
                row.Add(text, 1, 0);
                col.Add(row);
            }
            return col;
        }

        public static View TitleRow(string title, Color accent) => Ui.SectionHeader(title);

        public static Border Card(View content) => Ui.Card(content);

        // Floating over the hero: back left, bookmark right — white circles.
        public static View TopBar(bool saved, Action onBack, Action onSave, out Microsoft.Maui.Controls.Shapes.Path saveIcon)
        {
            var back = RoundIcon(Ui.IconBack, Colors.White);
            Ui.OnTap(back, (_, _) => onBack());
            Ui.Describe(back, "Go back");

            // Icons sit on a dark scrim circle, so white/gold always reads on
            // top of the hero photo in both light and dark themes.
            saveIcon = Ui.Icon(saved ? Ui.IconSavedFill : Ui.IconSaved, 22, saved ? Theme.Accent : Colors.White);
            var saveBtn = RoundWrap(saveIcon);
            Ui.OnTap(saveBtn, (_, _) => onSave());
            Ui.Describe(saveBtn, saved ? "Remove bookmark" : "Save to bookmarks");

            var grid = new Grid { Padding = new Thickness(12, 10) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(back, 0, 0);
            grid.Add(saveBtn, 2, 0);
            return grid;
        }

        private static Border RoundIcon(string glyph, Color color) => RoundWrap(Ui.Icon(glyph, 22, color));

        private static Border RoundWrap(View content) => new()
        {
            Content = content,
            WidthRequest = 42, HeightRequest = 42,
            BackgroundColor = Color.FromArgb("#8A000000"),
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 21 }
        };

        public static View AskNovaButton(string name)
            => Ui.PrimaryButton($"ASK NOVASAUR ABOUT {name.ToUpperInvariant()}", async (_, _) =>
            {
                NovaView.Ask($"Tell me an interesting fact about {name}.");
                await Nav.Push(() => new NovaPage());
            });

        // Related entries strip.
        public static View Related(IEnumerable<(string image, string name, object data)> items, Color accent)
        {
            var row = new HorizontalStackLayout { Spacing = 12, Padding = new Thickness(2, 4) };
            foreach (var (image, name, data) in items)
            {
                var imgGrid = new Grid();
                imgGrid.Add(EntryCards.ArtFallback(name, 22, stars: false));
                imgGrid.Add(new Image { Source = image, Aspect = Aspect.AspectFill, HeightRequest = 84, WidthRequest = 124 });
                var wrap = new Border
                {
                    Content = imgGrid, WidthRequest = 124, HeightRequest = 84,
                    Stroke = Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Shadow = Theme.CardShadow()
                };
                var label = new Label
                {
                    Text = name, FontFamily = Ui.Display, FontSize = Ui.S(13.5),
                    TextColor = Theme.TextPrimary, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation, WidthRequest = 124
                };
                var col = new VerticalStackLayout { Spacing = 6, Children = { wrap, label } };
                Ui.OnTap(col, async (_, _) =>
                {
                    if (data is Dinosaur d) await Nav.OpenDino(d);
                    else if (data is SpaceObject s) await Nav.OpenSpace(s);
                });
                row.Add(col);
            }
            var section = new VerticalStackLayout { Spacing = 12 };
            section.Add(Ui.SectionHeader("You might also like"));
            section.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = row });
            return section;
        }
    }
}
