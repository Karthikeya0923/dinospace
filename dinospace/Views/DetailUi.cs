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
        // The design sheet's page header: back arrow left, the lowercase
        // section name centred, and a save star on the right that turns gold.
        public static View HeaderBar(string section, bool saved, Action onBack, Action onSave,
            out Ui.IconToggle saveIcon, bool showSave = true)
        {
            var back = Ui.Icon(Ui.IconBack, 24);
            var backWrap = new Border
            {
                Content = back, WidthRequest = 44, HeightRequest = 44,
                BackgroundColor = Colors.Transparent, Stroke = Colors.Transparent
            };
            Ui.OnTap(backWrap, (_, _) => onBack());
            Ui.Describe(backWrap, "Go back");

            var title = new Label
            {
                Text = Ui.T(section),
                FontFamily = Ui.Display, FontSize = Ui.S(22), TextColor = Theme.TextPrimary,
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            };

            saveIcon = new Ui.IconToggle(Ui.IconStarOutline, Ui.IconStar, 26);
            saveIcon.Show(saved);
            var saveWrap = new Border
            {
                Content = saveIcon, WidthRequest = 44, HeightRequest = 44,
                BackgroundColor = Colors.Transparent, Stroke = Colors.Transparent,
                IsVisible = showSave
            };
            Ui.OnTap(saveWrap, (_, _) => onSave());
            Ui.Describe(saveWrap, saved ? "Remove from saved" : "Save this entry");

            var grid = new Grid { Padding = new Thickness(8, 10) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
            grid.Add(backWrap, 0, 0);
            grid.Add(title, 1, 0);
            grid.Add(saveWrap, 2, 0);
            return grid;
        }

        // The entry's picture, centred on the page itself like the design
        // sheet — no photo box, nothing else around it. Entries whose art
        // hasn't arrived yet show the starfield placeholder card.
        public static View EntryImage(string image, string title)
        {
            string baseName = image.EndsWith(".png") ? image[..^4] : image;
            if (!Ui.HasImage(baseName))
            {
                return new Border
                {
                    Content = EntryCards.PlayfulArt(title, 54),
                    HeightRequest = 210, Stroke = Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = 24 }
                };
            }

            var img = new Image
            {
                Source = image, Aspect = Aspect.AspectFit,
                HeightRequest = 230, HorizontalOptions = LayoutOptions.Center
            };
            Ui.Describe(img, title);

            var g = new Grid { HeightRequest = 244 };
            g.Add(img);
            return g;
        }

        // Simple label/value rows, exactly like the sheet's entry pages —
        // no boxes, no bars.
        public static View StatRows(IEnumerable<(string label, string value)> rows)
        {
            var col = new VerticalStackLayout { Spacing = 13, Margin = new Thickness(2, 4) };
            foreach (var (label, value) in rows)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                var grid = new Grid { ColumnSpacing = 12 };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.Add(new Label { Text = label, FontFamily = Ui.Fonts, FontSize = Ui.S(14.5), TextColor = Theme.TextSecondary }, 0, 0);
                grid.Add(new Label { Text = value, FontFamily = Ui.Fonts, FontSize = Ui.S(14.5), TextColor = Theme.TextPrimary, HorizontalOptions = LayoutOptions.End }, 1, 0);
                col.Add(grid);
            }
            return col;
        }

        // The two little tags under the name — different colours, like the
        // sheet ("Carnivore" quiet, "Late Cretaceous" green).
        public static View TagChips(params string[] tags)
        {
            var row = new HorizontalStackLayout { Spacing = 8 };
            for (int i = 0; i < tags.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(tags[i])) continue;
                bool green = i % 2 == 1;
                row.Add(new Border
                {
                    BackgroundColor = green ? Theme.AccentSoft : Theme.SurfaceAlt,
                    Stroke = green ? Theme.Accent.WithAlpha(0.45f) : Theme.Hairline.WithAlpha(0.6f),
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = 100 },
                    Padding = new Thickness(13, 6),
                    Content = new Label
                    {
                        Text = tags[i], FontFamily = Ui.Fonts, FontSize = Ui.S(12.5),
                        TextColor = green ? Theme.Accent : Theme.ChipText
                    }
                });
            }
            return row;
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
                CharacterSpacing = 1.2, TextColor = AppLayout.Playful ? PlayfulKit.OnSurface(accent) : Theme.Accent
            });
            col.Add(new Label
            {
                Text = value,
                FontFamily = Ui.Display, FontSize = Ui.S(17), TextColor = Theme.TextPrimary
            });
            // Playful gives each stat a soft colourful bubble; classic keeps the
            // quiet surface card with a shadow.
            var chip = new Border
            {
                Content = col,
                BackgroundColor = AppLayout.Playful ? accent.WithAlpha(0.15f) : Theme.Surface,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = AppLayout.Playful ? 18 : 14 },
                Padding = new Thickness(14, 11),
                MinimumWidthRequest = 96
            };
            if (!AppLayout.Playful) chip.Shadow = Theme.CardShadow();
            return chip;
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

            var lines = funFacts.Split('\n');

            // The light bulb (icon_funfact.png) presents the first fun fact
            // in a little speech bubble; the rest stay in the dotted list
            // below.
            int start = 0;
            if (Ui.HasImage("icon_funfact"))
            {
                string first = lines[0].TrimStart('•', ' ').Trim();
                if (first.Length > 0)
                {
                    start = 1;
                    var bubble = new Border
                    {
                        Content = new Label { Text = first, FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), LineHeight = 1.4, TextColor = Theme.TextPrimary },
                        BackgroundColor = Theme.Surface,
                        Stroke = Theme.CardStroke, StrokeThickness = 1.4,
                        StrokeShape = new RoundRectangle { CornerRadius = 18 },
                        Padding = new Thickness(14, 10),
                        VerticalOptions = LayoutOptions.Center
                    };
                    var row = new Grid { ColumnSpacing = 10 };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                    row.Add(Ui.Icon("icon_funfact", 72), 0, 0);
                    row.Add(bubble, 1, 0);
                    col.Add(row);
                }
            }

            foreach (var raw in lines[start..])
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

        public static View AskNovaButton(string name)
            => Ui.PrimaryButton($"ASK NOVA ABOUT {name.ToUpperInvariant()}", async (_, _) =>
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
                // Real art shows the whole creature on a clean ground; the
                // letter tile appears only while an entry has no art yet.
                if (Ui.HasImage(FaceArt.BaseName(image)))
                {
                    imgGrid.BackgroundColor = Theme.SurfaceAlt;
                    imgGrid.Add(new Image { Source = image, Aspect = Aspect.AspectFit, HeightRequest = 84, WidthRequest = 124, Margin = new Thickness(4) });
                }
                else
                    imgGrid.Add(EntryCards.ArtFallback(name, 22, stars: false));
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
