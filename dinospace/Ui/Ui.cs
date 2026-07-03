using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace
{
    // Small, consistent view builders shared across screens. Keeping these in
    // one place is what makes every list row, chip, and card look identical
    // and lets the pages stay short.
    public static class Ui
    {
        public const string Fonts = "Nunito";
        public const string Display = "Baloo";

        // Text-size accessibility scale. Screens built on navigation (detail
        // pages, quiz) pick this up immediately; the persistent tabs pick it up
        // next time they're opened.
        public static double Scale => AppSettings.FontScale;
        public static double S(double size) => size * Scale;

        // ---------- primitives ----------

        public static Label Title(string text, double size = 28) => new()
        {
            Text = text,
            FontFamily = Display,
            FontSize = S(size),
            TextColor = Theme.TextPrimary
        };

        public static Label Heading(string text, double size = 19) => new()
        {
            Text = text,
            FontFamily = Display,
            FontSize = S(size),
            TextColor = Theme.TextPrimary
        };

        public static Label Body(string text, Color? color = null, double size = 15) => new()
        {
            Text = text,
            FontFamily = Fonts,
            FontSize = S(size),
            LineHeight = 1.42,
            TextColor = color ?? Theme.TextPrimary
        };

        public static Label Muted(string text, double size = 13) => new()
        {
            Text = text,
            FontFamily = Fonts,
            FontSize = S(size),
            LineHeight = 1.35,
            TextColor = Theme.TextSecondary
        };

        public static Label Overline(string text, Color? color = null) => new()
        {
            Text = text.ToUpperInvariant(),
            FontFamily = Fonts,
            FontSize = 11,
            CharacterSpacing = 1.6,
            FontAttributes = FontAttributes.Bold,
            TextColor = color ?? Theme.TextHint
        };

        // ---------- cards & chips ----------

        public static Border Card(View content, Color? bg = null, Color? stroke = null, double radius = 18, Thickness? padding = null) => new()
        {
            Content = content,
            BackgroundColor = bg ?? Theme.Surface,
            Stroke = stroke ?? Theme.HairlineSoft,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = radius },
            Padding = padding ?? new Thickness(16)
        };

        public static Border Chip(string text, Color? bg = null, Color? textColor = null) => new()
        {
            BackgroundColor = bg ?? Theme.ChipBg,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Padding = new Thickness(9, 3),
            VerticalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Text = text,
                FontFamily = Fonts,
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = textColor ?? Theme.ChipText
            }
        };

        // A colored dot + label pill, used for diet/type tags.
        public static View TintChip(string text, Color accent)
            => Chip(text, MultiplyAlpha(accent, 0.16f), accent);

        // ---------- tappable helpers ----------

        public static T OnTap<T>(T view, EventHandler<TappedEventArgs> handler, bool haptic = true) where T : View
        {
            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) => { if (haptic) AppSettings.Tap(); handler(s, e); };
            view.GestureRecognizers.Add(tap);
            return view;
        }

        // Screen-reader label (TalkBack / VoiceOver).
        public static T Describe<T>(T view, string description) where T : View
        {
            Microsoft.Maui.Controls.SemanticProperties.SetDescription(view, description);
            return view;
        }

        // ---------- list rows ----------

        // Thumbnail | (title row + subtitle) | chevron, inside a rounded card.
        public static Border EntryRow(string image, string title, string chipText, string subtitle, Color accent, EventHandler<TappedEventArgs> tapped)
        {
            var thumb = Thumb(image, 58, accent);

            var titleRow = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
            titleRow.Add(new Label
            {
                Text = title,
                FontFamily = Display,
                FontSize = 17,
                TextColor = Theme.TextPrimary,
                VerticalOptions = LayoutOptions.Center
            });
            if (!string.IsNullOrEmpty(chipText))
                titleRow.Add(TintChip(chipText, accent));

            var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            info.Add(titleRow);
            if (!string.IsNullOrEmpty(subtitle))
                info.Add(Muted(subtitle, 12.5));

            var chevron = new Label
            {
                Text = "›",
                FontSize = 24,
                TextColor = Theme.TextHint,
                VerticalOptions = LayoutOptions.Center
            };

            var grid = new Grid { ColumnSpacing = 12, VerticalOptions = LayoutOptions.Center };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(thumb, 0, 0);
            grid.Add(info, 1, 0);
            grid.Add(chevron, 2, 0);

            var card = new Border
            {
                Content = grid,
                BackgroundColor = Theme.Surface,
                Stroke = Theme.HairlineSoft,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(10, 10),
                Margin = new Thickness(0, 4)
            };
            return OnTap(card, tapped);
        }

        // Rounded thumbnail with a faint accent ring while the image loads.
        public static Border Thumb(string image, double size, Color accent)
        {
            var img = new Image
            {
                Source = image,
                WidthRequest = size,
                HeightRequest = size,
                Aspect = Aspect.AspectFill
            };
            return new Border
            {
                Content = img,
                WidthRequest = size,
                HeightRequest = size,
                BackgroundColor = Theme.ImgPlaceholder,
                Stroke = MultiplyAlpha(accent, 0.4f),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 14 }
            };
        }

        // ---------- stat bars ----------

        // Label + track + fill, normalized to a 0..1 fraction.
        public static View StatBar(string label, string value, double fraction, Color accent)
        {
            fraction = System.Math.Clamp(fraction, 0.04, 1.0);

            var top = new Grid();
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            top.Add(Muted(label, 12.5), 0, 0);
            top.Add(new Label
            {
                Text = value,
                FontFamily = Fonts,
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Theme.TextPrimary,
                HorizontalOptions = LayoutOptions.End
            }, 1, 0);

            var fill = new Border
            {
                BackgroundColor = accent,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 5 },
                HeightRequest = 8,
                HorizontalOptions = LayoutOptions.Start
            };

            var track = new Border
            {
                BackgroundColor = Theme.SurfaceAlt,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 5 },
                HeightRequest = 8,
                Content = fill
            };
            // Size the fill once the track has a width.
            track.SizeChanged += (_, _) =>
            {
                if (track.Width > 0) fill.WidthRequest = track.Width * fraction;
            };

            return new VerticalStackLayout { Spacing = 6, Children = { top, track } };
        }

        // ---------- section header ----------

        public static View SectionHeader(string title, string? action = null, EventHandler<TappedEventArgs>? onAction = null)
        {
            var grid = new Grid { Margin = new Thickness(0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(Heading(title), 0, 0);
            if (!string.IsNullOrEmpty(action))
            {
                var link = new Label
                {
                    Text = action,
                    FontFamily = Fonts,
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Theme.AccentNova,
                    VerticalOptions = LayoutOptions.Center
                };
                if (onAction != null) OnTap(link, onAction);
                grid.Add(link, 1, 0);
            }
            return grid;
        }

        // ---------- color math ----------

        public static Color MultiplyAlpha(Color c, float alpha)
            => new Color(c.Red, c.Green, c.Blue, alpha);
    }
}
