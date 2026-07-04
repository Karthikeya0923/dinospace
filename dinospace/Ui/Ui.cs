using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace
{
    // Shared view builders — the app's editorial component kit.
    // Serif for headlines, clean sans for body, caps+rule section headers,
    // white cards with soft shadows.
    public static class Ui
    {
        public const string Fonts = "Nunito";        // body sans
        public const string Display = "Serif";       // DM Serif Display
        public const string DisplayItalic = "SerifItalic";
        public const string Icons = "Icons";         // Material icons

        // Material icon glyphs used across the app.
        public const string IconHome = "";
        public const string IconSearch = "";
        public const string IconSaved = "";     // bookmark_border
        public const string IconSavedFill = ""; // bookmark
        public const string IconSettings = "";
        public const string IconBack = "";      // arrow_back
        public const string IconChevron = "";   // chevron_right
        public const string IconClose = "";
        public const string IconStar = "";
        public const string IconStarBorder = "";
        public const string IconSwap = "";      // swap_horiz
        public const string IconBolt = "";      // offline_bolt (battles)
        public const string IconQuiz = "";      // library_books-ish
        public const string IconList = "";      // list
        public const string IconChat = "";      // chat

        public static double Scale => AppSettings.FontScale;
        public static double S(double size) => size * Scale;

        // ---------- type ----------

        public static Label Title(string text, double size = 30) => new()
        {
            Text = text,
            FontFamily = Display,
            FontSize = S(size),
            TextColor = Theme.TextPrimary,
            LineHeight = 1.08
        };

        public static Label Heading(string text, double size = 21) => new()
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
            LineHeight = 1.45,
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

        public static Label Icon(string glyph, double size, Color color) => new()
        {
            Text = glyph,
            FontFamily = Icons,
            FontSize = size,
            TextColor = color,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        // ALL-CAPS letterspaced section header with a thin rule underneath —
        // the magazine look from the reference.
        public static View SectionHeader(string title, string? action = null, System.EventHandler<TappedEventArgs>? onAction = null)
        {
            var caps = new Label
            {
                Text = title.ToUpperInvariant(),
                FontFamily = Fonts,
                FontSize = S(14),
                FontAttributes = FontAttributes.Bold,
                CharacterSpacing = 2.2,
                TextColor = Theme.TextPrimary,
                VerticalOptions = LayoutOptions.End
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(caps, 0, 0);
            if (!string.IsNullOrEmpty(action))
            {
                var link = new Label
                {
                    Text = action,
                    FontFamily = Fonts,
                    FontSize = S(13),
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Theme.Accent,
                    VerticalOptions = LayoutOptions.End
                };
                if (onAction != null) OnTap(link, onAction);
                grid.Add(link, 1, 0);
            }

            var rule = new BoxView { HeightRequest = 1.5, Color = Theme.Hairline, Margin = new Thickness(0, 8, 0, 0) };
            return new VerticalStackLayout { Spacing = 0, Margin = new Thickness(0, 10, 0, 2), Children = { grid, rule } };
        }

        // ---------- cards ----------

        // White card, rounded, soft shadow. No border strokes.
        public static Border Card(View content, double radius = 16, Thickness? padding = null) => new()
        {
            Content = content,
            BackgroundColor = Theme.Surface,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = radius },
            Padding = padding ?? new Thickness(16),
            Shadow = Theme.CardShadow()
        };

        public static Border Chip(string text, Color? bg = null, Color? textColor = null) => new()
        {
            BackgroundColor = bg ?? Theme.ChipBg,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 100 },
            Padding = new Thickness(12, 6),
            VerticalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Text = text,
                FontFamily = Fonts,
                FontSize = S(12),
                FontAttributes = FontAttributes.Bold,
                TextColor = textColor ?? Theme.ChipText
            }
        };

        public static View TintChip(string text, Color accent)
            => Chip(text, Theme.AccentSoft, Theme.Accent);

        // Filled red pill button, like the reference's SUBSCRIBE.
        public static Border PrimaryButton(string text, System.EventHandler<TappedEventArgs> onTap)
        {
            var label = new Label
            {
                Text = text,
                FontFamily = Fonts,
                FontSize = S(15),
                FontAttributes = FontAttributes.Bold,
                CharacterSpacing = 0.6,
                TextColor = Theme.TextOnAccent,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            var btn = new Border
            {
                Content = label,
                BackgroundColor = Theme.Accent,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Padding = new Thickness(16, 15),
                Shadow = Theme.CardShadow()
            };
            return OnTap(btn, onTap);
        }

        public static Border GhostButton(string text, System.EventHandler<TappedEventArgs> onTap)
        {
            var label = new Label
            {
                Text = text,
                FontFamily = Fonts,
                FontSize = S(15),
                FontAttributes = FontAttributes.Bold,
                TextColor = Theme.Accent,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            var btn = new Border
            {
                Content = label,
                BackgroundColor = Theme.Surface,
                Stroke = Theme.Hairline,
                StrokeThickness = 1.2,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Padding = new Thickness(16, 14)
            };
            return OnTap(btn, onTap);
        }

        // ---------- interaction ----------

        public static T OnTap<T>(T view, System.EventHandler<TappedEventArgs> handler, bool haptic = true) where T : View
        {
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) =>
            {
                if (haptic) AppSettings.Tap();
                var v = s as View ?? view;
                v.Opacity = 0.6;
                handler(s, e);
                await Task.Delay(120);
                v.Opacity = 1;
            };
            view.GestureRecognizers.Add(tap);
            return view;
        }

        public static T Describe<T>(T view, string description) where T : View
        {
            Microsoft.Maui.Controls.SemanticProperties.SetDescription(view, description);
            return view;
        }

        // ---------- images ----------

        public static Border Thumb(string image, double size, Color accent)
        {
            var img = new Image { Source = image, WidthRequest = size, HeightRequest = size, Aspect = Aspect.AspectFill };
            return new Border
            {
                Content = img,
                WidthRequest = size,
                HeightRequest = size,
                BackgroundColor = Colors.Transparent,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 12 }
            };
        }

        // ---------- stat bars ----------

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
                FontSize = S(13),
                FontAttributes = FontAttributes.Bold,
                TextColor = Theme.TextPrimary,
                HorizontalOptions = LayoutOptions.End
            }, 1, 0);

            var fill = new Border
            {
                BackgroundColor = accent,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 5 },
                HeightRequest = 7,
                HorizontalOptions = LayoutOptions.Start
            };
            var track = new Border
            {
                BackgroundColor = Theme.SurfaceAlt,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 5 },
                HeightRequest = 7,
                Content = fill
            };
            track.SizeChanged += (_, _) => { if (track.Width > 0) fill.WidthRequest = track.Width * fraction; };

            return new VerticalStackLayout { Spacing = 6, Children = { top, track } };
        }

        // ---------- colour math ----------

        public static Color MultiplyAlpha(Color c, float alpha)
            => new Color(c.Red, c.Green, c.Blue, alpha);
    }
}
