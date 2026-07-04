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
        // Icon names -> vector geometry (see IconData). Drawn as Paths, not a
        // font, because icon fonts render as tofu boxes on some Android builds.
        public const string IconHome = "home";
        public const string IconSearch = "search";
        public const string IconSaved = "saved";
        public const string IconSavedFill = "savedfill";
        public const string IconSettings = "settings";
        public const string IconBack = "back";
        public const string IconChevron = "chevron";
        public const string IconClose = "close";
        public const string IconStar = "star";
        public const string IconStarBorder = "star";
        public const string IconSwap = "swap";
        public const string IconBolt = "bolt";
        public const string IconQuiz = "quiz";
        public const string IconList = "list";
        public const string IconChat = "chat";
        public const string IconSend = "send";
        public const string IconStop = "stop";

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

        private static readonly Microsoft.Maui.Controls.Shapes.PathGeometryConverter _geoConv = new();

        // A crisp vector icon (24x24 geometry scaled to `size`).
        public static Microsoft.Maui.Controls.Shapes.Path Icon(string name, double size, Color color) => new()
        {
            Data = (Microsoft.Maui.Controls.Shapes.Geometry)_geoConv.ConvertFromInvariantString(IconData(name))!,
            Fill = new SolidColorBrush(color),
            Aspect = Stretch.Uniform,
            WidthRequest = size,
            HeightRequest = size,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        // Recolour / reshape an existing icon in place (nav selection, save toggle).
        public static void SetIcon(Microsoft.Maui.Controls.Shapes.Path icon, string name, Color color)
        {
            icon.Data = (Microsoft.Maui.Controls.Shapes.Geometry)_geoConv.ConvertFromInvariantString(IconData(name))!;
            icon.Fill = new SolidColorBrush(color);
        }

        // Material-style 24x24 path data.
        private static string IconData(string name) => name switch
        {
            "home" => "M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z",
            "search" => "M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z",
            "saved" => "M17 3H7c-1.1 0-1.99.9-1.99 2L5 21l7-3 7 3V5c0-1.1-.9-2-2-2zm0 15l-5-2.18L7 18V5h10v13z",
            "savedfill" => "M17 3H7c-1.1 0-1.99.9-1.99 2L5 21l7-3 7 3V5c0-1.1-.9-2-2-2z",
            "settings" => "M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.05.3-.09.63-.09.94s.02.64.07.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z",
            "back" => "M20 11H7.83l5.59-5.59L12 4l-8 8 8 8 1.41-1.41L7.83 13H20v-2z",
            "chevron" => "M10 6L8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z",
            "close" => "M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z",
            "star" => "M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z",
            "swap" => "M6.99 11L3 15l3.99 4v-3H14v-2H6.99v-3zM21 9l-3.99-4v3H10v2h7.01v3L21 9z",
            "bolt" => "M12 2.02c-5.51 0-9.98 4.47-9.98 9.98s4.47 9.98 9.98 9.98 9.98-4.47 9.98-9.98S17.51 2.02 12 2.02zM11.48 20v-6.26H8L13 4v6.26h3.35L11.48 20z",
            "list" => "M3 13h2v-2H3v2zm0 4h2v-2H3v2zm0-8h2V7H3v2zm4 4h14v-2H7v2zm0 4h14v-2H7v2zM7 7v2h14V7H7z",
            "quiz" => "M11 18h2v-2h-2v2zm1-16C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm0-14c-2.21 0-4 1.79-4 4h2c0-1.1.9-2 2-2s2 .9 2 2c0 2-3 1.75-3 5h2c0-2.25 3-2.5 3-5 0-2.21-1.79-4-4-4z",
            "chat" => "M20 2H4c-1.1 0-1.99.9-1.99 2L2 22l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2z",
            "send" => "M2.01 21L23 12 2.01 3 2 10l15 2-15 2z",
            "stop" => "M6 6h12v12H6z",
            _ => "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2z",
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
            Stroke = Theme.CardStroke,
            StrokeThickness = 1,
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
