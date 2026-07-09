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
        // Font families are layout-aware: the Playful layout swaps the serif
        // display for rounded Baloo. Body stays Nunito in both.
        public static string Fonts => AppLayout.BodyFont;          // body sans
        public static string Display => AppLayout.DisplayFont;     // DM Serif / Baloo
        public static string DisplayItalic => AppLayout.DisplayItalicFont;
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
        public const string IconBrush = "brush";
        public const string IconSend = "send";
        public const string IconStop = "stop";
        public const string IconBook = "book";
        public const string IconSwords = "swords";
        public const string IconCollection = "collection";
        public const string IconMore = "more";
        public const string IconTelescope = "telescope";
        public const string IconPencil = "pencil";

        public static double Scale => AppSettings.FontScale;
        public static double S(double size) => size * Scale;

        // Display-case for labels: the Playful layout writes everything in
        // friendly lowercase (like its storybook wordmark); Native keeps the
        // text exactly as authored.
        public static string T(string text) => AppLayout.Playful ? text.ToLowerInvariant() : text;

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
            "brush" => "M7 14c-1.66 0-3 1.34-3 3 0 1.31-1.16 2-2 2 .92 1.22 2.49 2 4 2 2.21 0 4-1.79 4-4 0-1.66-1.34-3-3-3zm13.71-9.37l-1.34-1.34c-.39-.39-1.02-.39-1.41 0L9 12.25 11.75 15l8.96-8.96c.39-.39.39-1.02 0-1.41z",
            "send" => "M2.01 21L23 12 2.01 3 2 10l15 2-15 2z",
            "stop" => "M6 6h12v12H6z",
            "book" => "M21 5c-1.11-.35-2.33-.5-3.5-.5-1.95 0-4.05.4-5.5 1.5-1.45-1.1-3.55-1.5-5.5-1.5S2.45 4.9 1 6v14.65c0 .25.25.5.5.5.1 0 .15-.05.25-.05C3.1 20.45 5.05 20 6.5 20c1.95 0 4.05.4 5.5 1.5 1.35-.85 3.8-1.5 5.5-1.5 1.65 0 3.35.3 4.75 1.05.1.05.15.05.25.05.25 0 .5-.25.5-.5V6c-.6-.45-1.25-.75-2-1zm0 13.5c-1.1-.35-2.3-.5-3.5-.5-1.7 0-4.15.65-5.5 1.5V8c1.35-.85 3.8-1.5 5.5-1.5 1.2 0 2.4.15 3.5.5v11.5z",
            "swords" => "M3 4.2 4.2 3 20 18.8 18.8 20 Z M19.8 3 21 4.2 5.2 20 4 18.8 Z M15.9 20.1 20.1 15.9 21.3 17.1 17.1 21.3 Z M2.7 17.1 3.9 15.9 8.1 20.1 6.9 21.3 Z",
            "collection" => "M20 2H4c-1 0-2 .9-2 2v3.01c0 .72.43 1.34 1 1.69V20c0 1.1 1.1 2 2 2h14c.9 0 2-.9 2-2V8.7c.57-.35 1-.97 1-1.69V4c0-1.1-1-2-2-2zm-5 12H9v-2h6v2zm5-7H4V4h16v3z",
            "more" => "M6 10c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2zm12 0c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2zm-6 0c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2z",
            "telescope" => "M2.2 11.2 15.1 4.1c.5-.3 1.1-.1 1.4.4l1.5 2.7c.3.5.1 1.1-.4 1.4L4.7 15.7c-.5.3-1.1.1-1.4-.4l-1.5-2.7c-.3-.5-.1-1.1.4-1.4z M18.6 4.7l1.9-1c.4-.2.9-.1 1.1.3l1.1 2c.2.4.1.9-.3 1.1l-1.9 1c-.4.2-.9.1-1.1-.3l-1.1-2c-.2-.4 0-.9.3-1.1z M11.1 14.6l1.9-1 .9 1.6-4.6 6.6c-.2.3-.6.4-.9.2l-.5-.3c-.3-.2-.4-.6-.2-.9l3.4-6.2z M13.6 15.9l1.4-.8 3.6 6.1c.2.3.1.7-.2.9l-.5.3c-.3.2-.7.1-.9-.2l-3.4-6.3z",
            "pencil" => "M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z",
            _ => "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2z",
        };

        // Section header. Native: tight ALL-CAPS + hairline rule (magazine
        // look). Playful: a big rounded Baloo title with a short chunky accent
        // underline — friendlier and easier for young readers to scan.
        public static View SectionHeader(string title, string? action = null, System.EventHandler<TappedEventArgs>? onAction = null)
        {
            if (AppLayout.FriendlyHeaders)
                return FriendlySectionHeader(title, action, onAction);

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

        private static View FriendlySectionHeader(string title, string? action, System.EventHandler<TappedEventArgs>? onAction)
        {
            var head = new Label
            {
                Text = T(title),
                FontFamily = Display,
                FontSize = S(20),
                TextColor = Theme.TextPrimary,
                VerticalOptions = LayoutOptions.End
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(head, 0, 0);
            if (!string.IsNullOrEmpty(action))
            {
                var link = new Label
                {
                    Text = action, FontFamily = Fonts, FontSize = S(13.5), FontAttributes = FontAttributes.Bold,
                    TextColor = Theme.Accent, VerticalOptions = LayoutOptions.Center
                };
                if (onAction != null) OnTap(link, onAction);
                grid.Add(link, 1, 0);
            }
            var underline = new Border
            {
                WidthRequest = 42, HeightRequest = 5, HorizontalOptions = LayoutOptions.Start,
                BackgroundColor = Theme.Accent, Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 3 }, Margin = new Thickness(2, 5, 0, 0)
            };
            return new VerticalStackLayout { Spacing = 0, Margin = new Thickness(0, 12, 0, 4), Children = { grid, underline } };
        }

        // ---------- cards ----------

        // White card, rounded, soft shadow. No border strokes. The default
        // radius follows the layout (rounder in Playful); callers that pass an
        // explicit radius still win.
        public static Border Card(View content, double radius = -1, Thickness? padding = null) => new()
        {
            Content = content,
            BackgroundColor = Theme.Surface,
            Stroke = Theme.CardStroke,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = radius < 0 ? AppLayout.CardRadius : radius },
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

        // Filled pill button. Text renders lowercase, like every button on the
        // design sheet.
        public static Border PrimaryButton(string text, System.EventHandler<TappedEventArgs> onTap)
        {
            var label = new Label
            {
                Text = T(text),
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
                StrokeShape = new RoundRectangle { CornerRadius = AppLayout.ButtonRadius },
                Padding = new Thickness(16, AppLayout.Playful ? 17 : 15),
                Shadow = Theme.CardShadow()
            };
            return OnTap(btn, onTap);
        }

        public static Border GhostButton(string text, System.EventHandler<TappedEventArgs> onTap)
        {
            var label = new Label
            {
                Text = T(text),
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
                StrokeShape = new RoundRectangle { CornerRadius = AppLayout.ButtonRadius },
                Padding = new Thickness(16, 14)
            };
            return OnTap(btn, onTap);
        }

        // ---------- interaction ----------

        public static T OnTap<T>(T view, System.EventHandler<TappedEventArgs> handler, bool haptic = true) where T : View
        {
            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) =>
            {
                if (haptic) AppSettings.Tap();
                var v = s as View ?? view;
                // Immediate, springy press feedback that never blocks the
                // action. The old version dimmed the view and then held it for
                // 120ms after the tap, which is what made every button feel
                // laggy. Here the dim springs straight back while the handler
                // runs, and navigation yields a frame (see Nav.Push) so the
                // press paints before any heavy work.
                v.Opacity = 0.55;
                v.FadeToAsync(1, 160, Easing.CubicOut);
                handler(s, e);
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

        // A little cut-out illustration from the app's sticker sheet
        // (st_*.png). Height fixed, width follows the art's own shape.
        public static Image Sticker(string name, double height, double width = -1) => new()
        {
            Source = name,
            HeightRequest = height,
            WidthRequest = width > 0 ? width : -1,
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true
        };

        // ---------- mascot slots ----------
        // Named spots reserved for the hand-drawn mascot art (mascot_*.png).
        // Until a slot's file is dropped into Resources/Images, the space
        // stays reserved: either empty, or showing a stand-in sticker.
        private static readonly System.Collections.Generic.Dictionary<string, bool> _mascotCache = new();

        public static bool HasImage(string baseName)
        {
            if (_mascotCache.TryGetValue(baseName, out bool known)) return known;
            bool found = false;
#if ANDROID
            try
            {
                var ctx = Android.App.Application.Context;
                found = ctx.Resources?.GetIdentifier(baseName, "drawable", ctx.PackageName) > 0;
            }
            catch { }
#endif
            _mascotCache[baseName] = found;
            return found;
        }

        // The mascot if its art exists; otherwise the fallback sticker; if
        // neither, an invisible box that holds the reserved space.
        public static View Mascot(string slot, double height, string? fallbackSticker = null)
        {
            if (HasImage(slot))
                return new Image
                {
                    Source = slot + ".png",
                    HeightRequest = height,
                    Aspect = Aspect.AspectFit,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    InputTransparent = true
                };
            if (fallbackSticker != null)
                return Sticker(fallbackSticker, height);
            return new BoxView { HeightRequest = height, Color = Colors.Transparent, InputTransparent = true };
        }

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

            double barH = AppLayout.Playful ? 12 : 7;
            double barR = barH / 2;
            var fill = new Border
            {
                BackgroundColor = accent,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = barR },
                HeightRequest = barH,
                HorizontalOptions = LayoutOptions.Start
            };
            var track = new Border
            {
                BackgroundColor = Theme.SurfaceAlt,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = barR },
                HeightRequest = barH,
                Content = fill
            };
            track.SizeChanged += (_, _) => { if (track.Width > 0) fill.WidthRequest = track.Width * fraction; };

            return new VerticalStackLayout { Spacing = 6, Children = { top, track } };
        }

        // ---------- page backdrop ----------

        // Standard page root: theme colour, plus the theme's wallpaper (when
        // one is picked) stretched behind the content. Every page uses this so
        // a wallpaper theme actually shows up everywhere, not just on Home.
        public static Grid PageRoot(View body)
        {
            var root = new Grid { BackgroundColor = Theme.Bg };
            if (Theme.Wallpaper is string wp)
            {
                root.Add(new Image { Source = wp, Aspect = Aspect.AspectFill, InputTransparent = true });
                // readability wash — art stays visible, text stays legible
                root.Add(new BoxView { Color = Theme.WallpaperDim, InputTransparent = true });
            }
            root.Add(body);
            return root;
        }

        // ---------- colour math ----------

        public static Color MultiplyAlpha(Color c, float alpha)
            => new Color(c.Red, c.Green, c.Blue, alpha);
    }
}
