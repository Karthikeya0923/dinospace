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
        // One rounded family everywhere: Baloo 2 Bold for display text,
        // Baloo 2 Medium (registered as "Body") for everything else.
        public static string Fonts => AppLayout.BodyFont;          // body sans
        public static string Display => AppLayout.DisplayFont;     // Baloo
        public static string DisplayItalic => AppLayout.DisplayItalicFont;
        // Every icon in the app is a named SLOT for Karthik's hand-drawn art:
        // a transparent PNG in Resources/Images with the slot's exact name.
        // Drop the file in and it appears everywhere that slot is used; until
        // then the slot just holds its space, blank. No built-in glyphs, no
        // emojis, no symbols — every picture in the app is his.
        public const string IconHome = "icon_home";                    // tab bar
        public const string IconEncyclopedia = "icon_encyclopedia";    // tab bar + more tile
        public const string IconBattles = "icon_battles";              // tab bar + more tile
        public const string IconCollection = "icon_collection";        // tab bar + "saved" more tile
        public const string IconMore = "icon_more";                    // tab bar
        public const string IconBack = "icon_back";                    // every back arrow
        public const string IconChevron = "icon_chevron";              // list rows / settings rows
        public const string IconClose = "icon_close";                  // scan sky close, remove-from-list
        public const string IconSearch = "icon_search";                // encyclopedia search bar
        public const string IconSend = "icon_send";                    // chat send
        public const string IconStop = "icon_stop";                    // chat stop-while-answering
        public const string IconStar = "icon_star";                    // saved star (gold), battle winner
        public const string IconStarOutline = "icon_star_outline";     // unsaved star
        public const string IconPlus = "icon_plus";                    // battle choose, own-list add, colour mixer
        public const string IconScanSky = "icon_scan_sky";             // home pill + more tile
        public const string IconAsk = "icon_ask";                      // ask-nova pill/tile/avatar fallback if the robot avatar is missing
        public const string IconDraw = "icon_draw";                    // draw entry more tile
        public const string IconQuiz = "icon_quiz";                    // quiz more tile
        public const string IconCollections = "icon_collections";      // collections more tile
        public const string IconSettings = "icon_settings";            // settings more tile
        public const string IconAppearance = "icon_appearance";        // settings row
        public const string IconSound = "icon_sound";                  // settings row
        public const string IconNovaAi = "icon_nova";                  // settings row
        public const string IconPrivacy = "icon_privacy";              // settings row
        public const string IconAbout = "icon_about";                  // settings row
        public const string IconContact = "icon_contact";              // settings row
        public const string IconDelete = "icon_delete";                // creation page header (delete)
        public const string IconCorrect = "icon_correct";              // quiz right answer
        public const string IconWrong = "icon_wrong";                  // quiz wrong answer

        public static double Scale => AppSettings.FontScale;
        public static double S(double size) => size * Scale;

        // ---------- live text scaling ----------

        // Every screen bakes its font sizes in through S() the moment it is
        // built, so changing the size setting used to leave the screens that
        // were already up untouched — you had to back out of the app's tabs
        // and come back before anything moved. This walks everything that is
        // alive right now (the page in front of you, the pages stacked behind
        // it, and all five tabs, which are built together at launch) and
        // multiplies each piece of text by how much the size just changed.
        // The whole app resizes under your finger. Screens created later read
        // the new scale from S() themselves, so both paths agree.
        // Views marked with this sit out live scaling. The tab bar is the
        // case that matters: five fixed columns cannot fit "encyclopedia" at
        // the largest setting, and a clipped "encyclope" reads as broken, so
        // the bar keeps its own size while everything inside the app scales.
        public const string NoScaleTag = "noscale";

        public static T NoScale<T>(T view) where T : Element
        {
            view.StyleId = NoScaleTag;
            return view;
        }

        public static void RescaleText(double factor)
        {
            if (double.IsNaN(factor) || double.IsInfinity(factor) || System.Math.Abs(factor - 1) < 0.0001) return;

            var seen = new System.Collections.Generic.HashSet<Element>();

            static double Grow(double size, double f) => size > 0 ? size * f : size;

            void Visit(Element? el)
            {
                // A view can be reached twice (a page is both the window's and
                // the navigation stack's), so each one is only touched once —
                // scaling something twice would double the jump.
                if (el == null || !seen.Add(el)) return;

                // Opted-out views keep their size, but their children are
                // still walked — only this one element is left alone.
                if (el.StyleId == NoScaleTag)
                {
                    if (el is IVisualTreeElement skipped)
                        foreach (var child in skipped.GetVisualChildren())
                            if (child is Element skippedChild) Visit(skippedChild);
                    return;
                }

                switch (el)
                {
                    case Label l:
                        l.FontSize = Grow(l.FontSize, factor);
                        if (l.FormattedText != null)
                            foreach (var span in l.FormattedText.Spans)
                                span.FontSize = Grow(span.FontSize, factor);
                        break;
                    case Button b: b.FontSize = Grow(b.FontSize, factor); break;
                    case Entry e: e.FontSize = Grow(e.FontSize, factor); break;
                    case Editor ed: ed.FontSize = Grow(ed.FontSize, factor); break;
                    case SearchBar s: s.FontSize = Grow(s.FontSize, factor); break;
                }

                if (el is IVisualTreeElement tree)
                    foreach (var child in tree.GetVisualChildren())
                        if (child is Element childEl) Visit(childEl);
            }

            try
            {
                if (Application.Current?.Windows is { } windows)
                    foreach (var window in windows)
                        Visit(window.Page);

                // Pushed pages live on Shell's stack, and they are still alive
                // behind whatever is on top — they have to resize too, or
                // going back reveals the old size.
                if (Shell.Current?.Navigation is { } nav)
                {
                    foreach (var page in nav.NavigationStack) Visit(page);
                    foreach (var page in nav.ModalStack) Visit(page);
                }
            }
            catch { }
        }

        // Display-case for labels: the Playful layout writes everything in
        // friendly lowercase (like its storybook wordmark); Native keeps the
        // text exactly as authored.
        public static string T(string text) => AppLayout.Playful ? text.ToLowerInvariant() : text;

        // The screen's width in device-independent units, or 0 if unknown.
        public static double ScreenDpWidth
        {
            get
            {
                try
                {
                    var d = DeviceDisplay.MainDisplayInfo;
                    return d.Density > 0 ? d.Width / d.Density : d.Width;
                }
                catch { return 0; }
            }
        }

        // True on tablets and other large screens (≥600dp wide). Phones stay
        // false, so any tablet-only layout tweak is a guaranteed no-op there.
        public static bool IsWideScreen => ScreenDpWidth >= 600;

        // True when the window is too short to hold a page that fills the
        // height — in practice, a phone turned landscape (~410dp tall). A
        // phone in landscape is also WIDER than the 600dp tablet mark, so
        // width alone can't tell the two apart: layouts that stretch or size
        // themselves must check this too, or their content runs off the
        // bottom with no way to reach it.
        public static bool IsShort(double heightDp) => heightDp > 0 && heightDp < 560;

        // Centres content and caps it to a comfortable reading width on big
        // screens, so a tablet doesn't stretch phone-first layouts edge to
        // edge. Implemented as symmetric side padding rather than a width
        // request, because that centres reliably inside any host — a
        // ScrollView, a Grid or a ContentView alike.
        //
        // The padding is recomputed from the wrapper's OWN width every time
        // it changes, never captured once at build time: a page rebuilt while
        // the device is still rotating (returning from the landscape sky
        // scanner used to do exactly this) would otherwise bake landscape
        // padding into a portrait page and squeeze everything into a column.
        public static View CapWidth(View content, double max = 640)
        {
            var wrap = new Grid { Children = { content } };
            void Apply(double w)
            {
                if (w <= 0) return;
                double pad = w < 600 ? 0 : System.Math.Max(0, (w - System.Math.Min(max, w - 24)) / 2);
                if (System.Math.Abs(wrap.Padding.Left - pad) > 0.5)
                    wrap.Padding = new Thickness(pad, 0);
            }
            wrap.SizeChanged += (_, _) => Apply(wrap.Width);
            Apply(ScreenDpWidth);
            return wrap;
        }

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

        // A moon phase as a hand-drawn slot: "Waxing Gibbous" looks for
        // waxinggibbous.png — one of the eight phase drawings. Blank until
        // the art lands.
        public static string MoonSlot(string phaseName)
            => phaseName.Replace(" ", "").ToLowerInvariant();

        // A hand-drawn icon slot: the PNG if Karthik has drawn it yet, or an
        // invisible box that reserves exactly the same space until he has.
        public static View Icon(string slot, double size)
        {
            if (HasImage(slot))
                return new Image
                {
                    Source = slot + ".png",
                    WidthRequest = size, HeightRequest = size,
                    Aspect = Aspect.AspectFit,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    InputTransparent = true
                };
            return new BoxView
            {
                WidthRequest = size, HeightRequest = size,
                Color = Colors.Transparent, InputTransparent = true
            };
        }

        // A two-state icon slot (saved/unsaved star, send/stop) that swaps
        // between two hand-drawn slots in place.
        public sealed class IconToggle : Grid
        {
            private readonly View _first, _second;
            public IconToggle(string firstSlot, string secondSlot, double size)
            {
                InputTransparent = true;
                HorizontalOptions = LayoutOptions.Center;
                VerticalOptions = LayoutOptions.Center;
                _first = Icon(firstSlot, size);
                _second = Icon(secondSlot, size);
                _second.IsVisible = false;
                Children.Add(_first);
                Children.Add(_second);
            }
            public void Show(bool second) { _first.IsVisible = !second; _second.IsVisible = second; }
        }

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
            // The accent underline hugs the title and runs its FULL width —
            // from the first letter to the last, never a short stub.
            var underline = new Border
            {
                HeightRequest = 5, HorizontalOptions = LayoutOptions.Fill,
                BackgroundColor = Theme.Accent, Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 3 }, Margin = new Thickness(1, 5, 1, 0)
            };
            var headWrap = new VerticalStackLayout
            {
                Spacing = 0, HorizontalOptions = LayoutOptions.Start,
                Children = { head, underline }
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(headWrap, 0, 0);
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
            return new VerticalStackLayout { Spacing = 0, Margin = new Thickness(0, 12, 0, 4), Children = { grid } };
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
            StrokeThickness = 1.4,
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
                FontFamily = Display,
                FontSize = S(16),
                TextColor = Theme.TextOnAccent,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            var btn = new Border
            {
                Content = label,
                BackgroundColor = Theme.Accent,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 100 },
                Padding = new Thickness(20, 16),
                Shadow = Theme.CardShadow()
            };
            return OnTap(btn, onTap);
        }

        public static Border GhostButton(string text, System.EventHandler<TappedEventArgs> onTap)
        {
            var label = new Label
            {
                Text = T(text),
                FontFamily = Display,
                FontSize = S(16),
                TextColor = Theme.TextPrimary,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            var btn = new Border
            {
                Content = label,
                BackgroundColor = Theme.AccentSoft,
                Stroke = Theme.TextPrimary.WithAlpha(0.55f),
                StrokeThickness = 1.6,
                StrokeShape = new RoundRectangle { CornerRadius = 100 },
                Padding = new Thickness(20, 14)
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

        // The mascot if its art exists; otherwise the given hand-drawn icon
        // slot; if neither has arrived, an invisible box holds the space.
        public static View Mascot(string slot, double height, string? fallbackIcon = null)
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
            if (fallbackIcon != null)
                return Icon(fallbackIcon, height);
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
