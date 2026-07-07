using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The landing tab, magazine-style: logo masthead, a big featured story,
    // curated entry grids with "View all", the play features, and a fact.
    public class HomeView : ContentView, ITabView
    {
        private Border _featuredCard = null!;
        private bool _showDino = true;
        private readonly Dinosaur _dino;
        private readonly SpaceObject _space;
        private Label _factLabel = null!;

        public HomeView(Action<int> goTab)
        {
            _dino = DinoData.All[DateTime.Now.DayOfYear % DinoData.All.Count];
            _space = SpaceData.All[DateTime.Now.DayOfYear % SpaceData.All.Count];
            Build();
        }

        public void OnSelected() => StatsStore.UpdateStreak();

        // ================= PLAYFUL LAYOUT =================
        // A completely different home for young explorers: a big welcome, a
        // grid of vibrant gradient "worlds" to jump into, and a quick row of
        // extras. Colourful, chunky, and built for tapping, not reading.
        private void BuildPlayful()
        {
            var stack = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(16, 14, 16, 30) };

            // A big, colourful welcome banner — a mascot, the wordmark, and a
            // cheery tagline on a deep-space gradient.
            var titleCol = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };
            titleCol.Add(new Label { Text = "DinoSpace", FontFamily = Ui.Display, FontSize = Ui.S(34), TextColor = Colors.White, LineHeight = 1.0 });
            titleCol.Add(new Label { Text = "Explore · Learn · Imagine!", FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), FontAttributes = FontAttributes.Bold, TextColor = Colors.White.WithAlpha(0.92f) });

            var bannerGrid = new Grid { ColumnSpacing = 12, Padding = new Thickness(18, 18) };
            bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bannerGrid.Add(new Label { Text = "🦕", FontSize = 40, VerticalOptions = LayoutOptions.Center }, 0, 0);
            bannerGrid.Add(titleCol, 1, 0);
            bannerGrid.Add(new Label { Text = "🚀", FontSize = 34, VerticalOptions = LayoutOptions.Center }, 2, 0);

            stack.Add(new Border
            {
                Content = bannerGrid,
                Background = PlayfulKit.Gradient(PlayfulKit.Space),
                Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 26 },
                Shadow = Theme.CardShadow()
            });

            // Live sky teaser (kept — it's a lovely, dynamic touch).
            stack.Add(SkyCard());

            // The six big worlds, two per row.
            var grid = new Grid { ColumnSpacing = 14, RowSpacing = 14 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            for (int r = 0; r < 3; r++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            grid.Add(WorldTile("🦕", "Dinosaurs", "Meet amazing creatures", "#3FBF6A", "#2E8B57", async () => await Nav.Push(() => new BrowsePage("Dinosaurs"))), 0, 0);
            grid.Add(WorldTile("🪐", "Space", "Planets, stars & galaxies", "#A66BFF", "#7C3AED", async () => await Nav.Push(() => new BrowsePage("Space"))), 1, 0);
            grid.Add(WorldTile("🔭", "Sky Scanner", "Point at the real sky!", "#3E9BFF", "#2E5FD0", async () => await Nav.Push(() => new SkyViewPage())), 0, 1);
            grid.Add(WorldTile("🤖", "Ask NovaSaur", "Your dino & space buddy", "#25C7C7", "#0E9E9E", async () => await Nav.Push(() => new NovaPage())), 1, 1);
            grid.Add(WorldTile("⚔️", "Dino Battles", "Who would win?", "#FF7361", "#E23B3B", async () => await Nav.Push(() => new BattlePage(null))), 0, 2);
            grid.Add(WorldTile("🎨", "Editor", "Draw your own entry!", "#FF6FB0", "#E14A97", async () => await Nav.Push(() => new CreationsPage())), 1, 2);
            stack.Add(grid);

            // A quick row of extras.
            var quick = new Grid { ColumnSpacing = 12 };
            quick.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            quick.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            quick.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            quick.Add(QuickTile("🧩", "Quizzes", "#F0A81E", async () => await StartQuiz()), 0, 0);
            quick.Add(QuickTile("📚", "Collections", "#4C6EF5", async () => await Nav.Push(() => new CollectionsListPage())), 1, 0);
            quick.Add(QuickTile("🎲", "Surprise Me", "#12A594", async () => await OpenSurprise()), 2, 0);
            stack.Add(quick);

            // Fun fact at the bottom.
            stack.Add(new Label { Text = "Did you know?", FontFamily = Ui.Display, FontSize = Ui.S(20), TextColor = Theme.TextPrimary, Margin = new Thickness(2, 6, 0, 0) });
            stack.Add(FactCard());

            Content = new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
        }

        private View WorldTile(string emoji, string title, string sub, string c1, string c2, Func<System.Threading.Tasks.Task> onTap)
        {
            var col = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(16, 18, 16, 16), HeightRequest = 150, VerticalOptions = LayoutOptions.Fill };
            col.Add(new Label { Text = emoji, FontSize = 38 });
            col.Add(new BoxView { HeightRequest = 4, Color = Colors.Transparent });
            col.Add(new Label { Text = title, FontFamily = Ui.Display, FontSize = Ui.S(20), TextColor = Colors.White, LineHeight = 1.0 });
            col.Add(new Label { Text = sub, FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Colors.White.WithAlpha(0.9f), LineHeight = 1.2 });

            var tile = new Border
            {
                Content = col,
                Background = new LinearGradientBrush(
                    new GradientStopCollection { new GradientStop(Color.FromArgb(c1), 0), new GradientStop(Color.FromArgb(c2), 1) },
                    new Point(0, 0), new Point(1, 1)),
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 24 },
                Shadow = Theme.CardShadow()
            };
            Ui.OnTap(tile, async (_, _) => await onTap());
            Ui.Describe(tile, $"{title}: {sub}");
            return tile;
        }

        private View QuickTile(string emoji, string title, string c, Func<System.Threading.Tasks.Task> onTap)
        {
            var col = new VerticalStackLayout { Spacing = 3, Padding = new Thickness(6, 14), HorizontalOptions = LayoutOptions.Fill };
            col.Add(new Label { Text = emoji, FontSize = 24, HorizontalTextAlignment = TextAlignment.Center });
            col.Add(new Label { Text = title, FontFamily = Ui.Display, FontSize = Ui.S(13.5), TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center });

            var tile = new Border
            {
                Content = col,
                BackgroundColor = Color.FromArgb(c),
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Shadow = Theme.CardShadow()
            };
            Ui.OnTap(tile, async (_, _) => await onTap());
            Ui.Describe(tile, title);
            return tile;
        }
        // ================= END PLAYFUL =================

        private void Build()
        {
            if (AppLayout.Playful) { BuildPlayful(); return; }

            var stack = new VerticalStackLayout { Spacing = 18, Padding = new Thickness(18, 10, 18, 28) };

            stack.Add(Masthead());

            // A different hello each day — tiny thing, but it makes the app
            // feel alive instead of like a menu.
            string[] hellos =
            {
                "Let's explore.", "Ready to roar?", "The sky is calling.",
                "Time to dig for treasure.", "Big teeth. Bigger universe.", "Look up. Look back.",
                "Adventure awaits, explorer.", "What will you discover today?", "Stomp. Zoom. Wonder.",
                "65 million years in your pocket.", "Somewhere, a star is being born.", "Dig in!"
            };
            string helloText = hellos[DateTime.Now.DayOfYear % hellos.Length];
            stack.Add(new Label
            {
                Text = helloText,
                FontFamily = Ui.Display,
                FontSize = Ui.S(28),
                TextColor = Theme.TextSecondary
            });

            if (ExplorerRow() is View explorer)
                stack.Add(explorer);

            _featuredCard = BuildFeatured();
            stack.Add(_featuredCard);

            stack.Add(Ui.SectionHeader("Your sky"));
            stack.Add(SkyCard());

            // Dinosaurs
            stack.Add(Ui.SectionHeader("Dinosaurs", "View all", async (_, _) => await Nav.Push(() => new BrowsePage("Dinosaurs"))));
            stack.Add(EntryCards.TwoColumn(new (string, string, string, Action)[]
            {
                (Item(DinoData.ByName("Tyrannosaurus Rex"))),
                (Item(DinoData.ByName("Spinosaurus"))),
                (Item(DinoData.ByName("Triceratops"))),
                (Item(DinoData.ByName("Velociraptor"))),
            }));

            // Space
            stack.Add(Ui.SectionHeader("Space", "View all", async (_, _) => await Nav.Push(() => new BrowsePage("Space"))));
            stack.Add(EntryCards.TwoColumn(new (string, string, string, Action)[]
            {
                (ItemS(SpaceData.ByName("Saturn"))),
                (ItemS(SpaceData.ByName("Sun"))),
                (ItemS(SpaceData.ByName("Sagittarius A*"))),
                (ItemS(SpaceData.ByName("Mars"))),
            }));

            // NovaSaur
            stack.Add(Ui.SectionHeader("Ask NovaSaur"));
            stack.Add(NovaCard());

            // Play
            stack.Add(Ui.SectionHeader("Play"));
            stack.Add(PlayRow(Ui.IconQuiz, "Quizzes", "Test what you know", async () => await StartQuiz()));
            stack.Add(PlayRow(Ui.IconBolt, "Dino Battle", "Two creatures face off", async () => await Nav.Push(() => new BattlePage(null))));
            stack.Add(PlayRow(Ui.IconList, "Collections", "Curated ranked lists", async () => await Nav.Push(() => new CollectionsListPage())));
            stack.Add(PlayRow(Ui.IconBrush, "Your Creations", "Draw your own dino or planet", async () => await Nav.Push(() => new CreationsPage())));
            stack.Add(PlayRow(Ui.IconStar, "Surprise Me", "Spin the wheel — meet someone new", async () => await OpenSurprise()));

            // Fact
            stack.Add(Ui.SectionHeader("Did you know?"));
            stack.Add(FactCard());

            Content = new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
        }

        private static (string, string, string, Action) Item(Dinosaur? d)
        {
            d ??= DinoData.All[0];
            return (d.ImageFile, d.Name, d.Era, async () => await Nav.OpenDino(d));
        }

        private static (string, string, string, Action) ItemS(SpaceObject? s)
        {
            s ??= SpaceData.All[0];
            return (s.ImageFile, s.Name, s.TypeLabel, async () => await Nav.OpenSpace(s));
        }

        // Live Sky Tonight teaser: tonight's moon, drawn correctly, no
        // location or permissions needed. Tapping opens the full sky report.
        private View SkyCard()
        {
            var moon = SkyCalc.Moon(DateTime.UtcNow);

            var moonView = new GraphicsView
            {
                Drawable = new MoonPhaseDrawable { ElongationDeg = moon.ElongationDeg },
                WidthRequest = 44, HeightRequest = 44,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                InputTransparent = true
            };
            var moonWrap = new Border
            {
                Content = moonView,
                WidthRequest = 64, HeightRequest = 64,
                BackgroundColor = Color.FromArgb("#111527"),   // a little window of night sky
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                VerticalOptions = LayoutOptions.Center
            };

            int lit = (int)Math.Round(moon.Illumination * 100);
            var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            info.Add(new Label { Text = moon.PhaseName, FontFamily = Ui.Display, FontSize = Ui.S(18), TextColor = Theme.TextPrimary });
            info.Add(new Label { Text = $"{lit}% lit tonight", FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), TextColor = Theme.TextSecondary });
            info.Add(new Label
            {
                Text = "See the planets and constellations above you",
                FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), FontAttributes = FontAttributes.Bold,
                TextColor = Theme.Accent, Margin = new Thickness(0, 3, 0, 0)
            });

            var chevron = Ui.Icon(Ui.IconChevron, 24, Theme.TextHint);
            chevron.VerticalOptions = LayoutOptions.Center;

            var grid = new Grid { ColumnSpacing = 14 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(moonWrap, 0, 0);
            grid.Add(info, 1, 0);
            grid.Add(chevron, 2, 0);

            var card = Ui.Card(grid, radius: 18, padding: new Thickness(14, 12));
            Ui.OnTap(card, async (_, _) => await Nav.Push(() => new SkyPage()));
            Ui.Describe(card, $"Sky Tonight: {moon.PhaseName}, {lit} percent lit. Opens tonight's sky report.");
            return card;
        }

        // ----- masthead: friend's logo, serif fallback -----
        private View Masthead()
        {
            var grid = new Grid { HeightRequest = 46 };

            var fallback = new Label
            {
                Text = "DinoSpace",
                FontFamily = Ui.Display,
                FontSize = 30,
                TextColor = Theme.Accent,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            var logo = new Image
            {
                Source = "mainlogo.png",
                Aspect = Aspect.AspectFit,
                HeightRequest = 42,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            // If the logo art loads, it covers the wordmark; until then the
            // serif wordmark carries the masthead.
            grid.Add(fallback);
            grid.Add(logo);
            Ui.Describe(grid, "DinoSpace");
            return grid;
        }

        // ----- featured story -----
        private Border BuildFeatured()
        {
            double heroH = 250;
            var img = new Image { Source = _dino.ImageFile, Aspect = Aspect.AspectFill, HeightRequest = heroH };
            var imgWrap = new Grid { HeightRequest = heroH };
            var fallback = new Grid();
            fallback.Add(EntryCards.ArtFallback(_dino.Name, 44));
            imgWrap.Add(fallback);
            imgWrap.Add(img);

            // Big flip pill — generous hitbox, top-right of the photo.
            var flipContent = new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    Ui.Icon(Ui.IconSwap, 18, Theme.TextOnAccent),
                    new Label { Text = "Flip", FontFamily = Ui.Fonts, FontSize = 13.5, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextOnAccent, VerticalOptions = LayoutOptions.Center }
                }
            };
            var flip = new Border
            {
                Content = flipContent,
                BackgroundColor = Theme.Accent,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 100 },
                Padding = new Thickness(16, 10),
                MinimumHeightRequest = 42,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 12, 12, 0),
                Shadow = Theme.CardShadow()
            };
            Ui.OnTap(flip, (_, _) => { _showDino = !_showDino; RefreshFeatured(); });
            Ui.Describe(flip, "Flip between dinosaur and space object of the day");
            imgWrap.Add(flip);

            var tag = new Label { Text = "DINOSAUR OF THE DAY", FontFamily = Ui.Fonts, FontSize = 11, FontAttributes = FontAttributes.Bold, CharacterSpacing = 1.8, TextColor = Theme.Accent };
            var name = new Label { Text = _dino.Name, FontFamily = Ui.Display, FontSize = Ui.S(26), LineHeight = 1.05, TextColor = Theme.TextPrimary };
            var sub = new Label { Text = "“" + _dino.ShortDescription + "”", FontFamily = Ui.Fonts, FontSize = Ui.S(14), LineHeight = 1.4, TextColor = Theme.TextSecondary };

            var info = new VerticalStackLayout { Spacing = 6, Padding = new Thickness(16, 14, 16, 18) };
            info.Add(tag); info.Add(name); info.Add(sub);

            var col = new VerticalStackLayout { Spacing = 0 };
            col.Add(imgWrap);
            col.Add(info);

            var card = new Border
            {
                Content = col,
                BackgroundColor = Theme.Surface,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                Padding = 0,
                Shadow = Theme.CardShadow()
            };
            Ui.OnTap(card, async (_, _) =>
            {
                if (_showDino) await Nav.OpenDino(_dino);
                else await Nav.OpenSpace(_space);
            });

            card.BindingContext = new FeaturedRefs(tag, name, sub, img, fallback);
            return card;
        }

        private record FeaturedRefs(Label Tag, Label Name, Label Sub, Image Img, Grid Fallback);

        private void RefreshFeatured()
        {
            if (_featuredCard.BindingContext is not FeaturedRefs r) return;
            if (_showDino)
            {
                r.Tag.Text = "DINOSAUR OF THE DAY";
                r.Name.Text = _dino.Name;
                r.Sub.Text = "“" + _dino.ShortDescription + "”";
                r.Img.Source = _dino.ImageFile;
            }
            else
            {
                r.Tag.Text = "SPACE OBJECT OF THE DAY";
                r.Name.Text = _space.Name;
                r.Sub.Text = "“" + _space.ShortDescription + "”";
                r.Img.Source = _space.ImageFile;
            }
            // Keep the night-sky stand-in's initial in step with the flip.
            r.Fallback.Children.Clear();
            r.Fallback.Add(EntryCards.ArtFallback(_showDino ? _dino.Name : _space.Name, 44));
        }

        // ----- Nova card -----
        private View NovaCard()
        {
            var col = new VerticalStackLayout { Spacing = 10 };
            col.Add(new Label
            {
                Text = "Curious about anything prehistoric or cosmic?",
                FontFamily = Ui.Display, FontSize = Ui.S(20), LineHeight = 1.15, TextColor = Theme.TextPrimary
            });
            col.Add(new Label
            {
                Text = "NovaSaur answers right on your phone — no internet needed.",
                FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), LineHeight = 1.4, TextColor = Theme.TextSecondary
            });
            col.Add(Ui.PrimaryButton("ASK A QUESTION", async (_, _) => await Nav.Push(() => new NovaPage())));
            return Ui.Card(col, radius: 18, padding: new Thickness(18, 16));
        }

        // ----- play rows -----
        private View PlayRow(string icon, string title, string sub, Func<System.Threading.Tasks.Task> onTap)
        {
            var iconWrap = new Border
            {
                WidthRequest = 44, HeightRequest = 44,
                BackgroundColor = Theme.AccentSoft,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 22 },
                VerticalOptions = LayoutOptions.Center,
                Content = Ui.Icon(icon, 22, Theme.Accent)
            };

            var info = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };
            info.Add(new Label { Text = title, FontFamily = Ui.Display, FontSize = Ui.S(18), TextColor = Theme.TextPrimary });
            info.Add(new Label { Text = sub, FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), TextColor = Theme.TextSecondary });

            var chevron = Ui.Icon(Ui.IconChevron, 24, Theme.TextHint);
            chevron.VerticalOptions = LayoutOptions.Center;

            var grid = new Grid { ColumnSpacing = 14 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(iconWrap, 0, 0);
            grid.Add(info, 1, 0);
            grid.Add(chevron, 2, 0);

            var card = Ui.Card(grid, radius: 16, padding: new Thickness(14, 12));
            Ui.OnTap(card, async (_, _) => await onTap());
            return card;
        }

        private static async System.Threading.Tasks.Task StartQuiz()
            => await Nav.Push(() => new QuizSetupPage());

        // Surprise Me — a random creature or space object, weighted toward
        // ones the explorer hasn't met yet so it keeps feeling fresh.
        private static readonly Random _surprise = new();
        private static async System.Threading.Tasks.Task OpenSurprise()
        {
            bool dino = _surprise.Next(2) == 0;
            if (dino)
            {
                var unseen = DinoData.All.Where(d => StatsStore.Views(d.Name) == 0).ToList();
                var pool = unseen.Count > 0 ? unseen : DinoData.All.ToList();
                await Nav.OpenDino(pool[_surprise.Next(pool.Count)]);
            }
            else
            {
                var unseen = SpaceData.All.Where(s => StatsStore.Views(s.Name) == 0).ToList();
                var pool = unseen.Count > 0 ? unseen : SpaceData.All.ToList();
                await Nav.OpenSpace(pool[_surprise.Next(pool.Count)]);
            }
        }

        // A one-line "you, the explorer" strip: the daily streak and how much
        // of the encyclopedia has been discovered so far. Collection progress
        // is half the fun at this age — and it quietly rewards coming back.
        private View? ExplorerRow()
        {
            int streak = StatsStore.Streak();
            int seen = StatsStore.DinosSeen() + StatsStore.SpaceSeen();
            int total = DinoData.All.Count + SpaceData.All.Count;
            if (streak <= 1 && seen == 0) return null;   // brand-new explorer, nothing to brag about yet

            var row = new HorizontalStackLayout { Spacing = 8 };
            if (streak > 1)
                row.Add(Ui.Chip($"🔥 {streak}-day streak", Theme.AccentSoft, Theme.Accent));
            if (seen > 0)
                row.Add(Ui.Chip($"✦ {seen} of {total} discovered", Theme.AccentSoft, Theme.Accent));
            return row;
        }

        // ----- fact -----
        private View FactCard()
        {
            _factLabel = new Label
            {
                Text = FactData.Random(),
                FontFamily = Ui.Display,
                FontSize = Ui.S(18),
                LineHeight = 1.25,
                TextColor = Theme.TextPrimary
            };
            var col = new VerticalStackLayout { Spacing = 10 };
            col.Add(_factLabel);
            col.Add(new Label { Text = "Tap for another", FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextHint });
            var card = Ui.Card(col, radius: 18, padding: new Thickness(18, 16));
            Ui.OnTap(card, (_, _) => _factLabel.Text = FactData.Random());
            return card;
        }
    }
}
