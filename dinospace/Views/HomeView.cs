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
        // The storybook cover, straight from the design sheet: the lowercase
        // "dinospace" wordmark, the friendly sauropod mascot in the middle of
        // the starred paper, and two outlined pills — scan sky and ask dino.
        private void BuildPlayful()
        {
            var grid = new Grid { Padding = new Thickness(28, 10, 28, 26), RowSpacing = 0 };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.7, GridUnitType.Star) });

            var wordmark = new Label
            {
                Text = "dinospace",
                FontFamily = Ui.Display,
                FontSize = 42,
                TextColor = Theme.TextPrimary,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.End
            };
            Ui.Describe(wordmark, "dinospace");
            grid.Add(wordmark, 0, 1);

            var mascot = new GraphicsView
            {
                Drawable = new PlayfulDinoDrawable(),
                HeightRequest = 280,
                InputTransparent = true,
                VerticalOptions = LayoutOptions.Center
            };
            grid.Add(mascot, 0, 2);

            var buttons = new VerticalStackLayout { Spacing = 14, VerticalOptions = LayoutOptions.Start };
            buttons.Add(HomePill(Ui.IconTelescope, "scan sky", async () => await Nav.Push(() => new SkyViewPage())));
            buttons.Add(HomePill(null, "ask dino", async () => await Nav.Push(() => new NovaPage())));
            grid.Add(buttons, 0, 3);

            Content = grid;
        }

        // An outlined storybook pill: soft sage fill, fine olive stroke, a
        // little line icon (or the mascot's face) beside lowercase Baloo text.
        private View HomePill(string? icon, string text, Func<System.Threading.Tasks.Task> onTap)
        {
            var row = new HorizontalStackLayout
            {
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            if (icon != null)
                row.Add(Ui.Icon(icon, 22, Theme.TextPrimary));
            else
                row.Add(new Label { Text = "🦕", FontSize = 18, VerticalOptions = LayoutOptions.Center });
            row.Add(new Label
            {
                Text = text,
                FontFamily = Ui.Display,
                FontSize = 19,
                TextColor = Theme.TextPrimary,
                VerticalOptions = LayoutOptions.Center
            });

            var pill = new Border
            {
                Content = row,
                BackgroundColor = Theme.AccentSoft,
                Stroke = Theme.TextPrimary.WithAlpha(0.55f),
                StrokeThickness = 1.6,
                StrokeShape = new RoundRectangle { CornerRadius = 100 },
                HeightRequest = 58,
                Padding = new Thickness(20, 0)
            };
            Ui.OnTap(pill, async (_, _) => await onTap());
            Ui.Describe(pill, text);
            return pill;
        }
        // ================= END PLAYFUL =================

        private void Build()
        {
            if (AppLayout.Playful) { BuildPlayful(); return; }

            var stack = new VerticalStackLayout { Spacing = 18, Padding = new Thickness(18, 10, 18, 28) };

            stack.Add(Masthead());

            // A different line each day — quiet, factual, a little wondrous.
            string[] hellos =
            {
                "What will you discover today?",
                "The sky is different every night.",
                "Every fossil tells a story.",
                "Look up. Look back.",
                "Somewhere, a star is being born.",
                "Some of these creatures had no name until someone curious found them.",
            };
            string helloText = hellos[DateTime.Now.DayOfYear % hellos.Length];
            stack.Add(new Label
            {
                Text = helloText,
                FontFamily = Ui.Fonts,
                FontSize = Ui.S(15),
                TextColor = Theme.TextSecondary,
                Margin = new Thickness(2, -6, 0, 0)
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

            // Play — big colour tiles, not a list of rows.
            stack.Add(Ui.SectionHeader("Play"));
            var play = new Grid { ColumnSpacing = 12, RowSpacing = 12 };
            play.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            play.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            for (int r = 0; r < 3; r++) play.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            play.Add(PlayTile(Ui.IconQuiz, "Quizzes", "Test what you know", "#379E6B", "#25714C", async () => await StartQuiz()), 0, 0);
            play.Add(PlayTile(Ui.IconBolt, "Dino Battle", "Two creatures face off", "#E0684B", "#B44432", async () => await Nav.Push(() => new BattlePage(null))), 1, 0);
            play.Add(PlayTile(Ui.IconBrush, "Your Creations", "Draw your own entry", "#C25AA4", "#93407C", async () => await Nav.Push(() => new CreationsPage())), 0, 1);
            play.Add(PlayTile(Ui.IconList, "Collections", "Curated ranked lists", "#4C74D9", "#3352A8", async () => await Nav.Push(() => new CollectionsListPage())), 1, 1);
            play.Add(PlayTile(Ui.IconStar, "Surprise Me", "Meet someone new", "#C99231", "#98691D", async () => await OpenSurprise()), 0, 2);
            play.Add(PlayTile(Ui.IconSearch, "Scan the Sky", "Point at the real sky", "#4B66C9", "#2F4494", async () => await Nav.Push(() => new SkyViewPage())), 1, 2);
            stack.Add(play);

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

        // ----- play tiles -----
        // Native keeps these quiet: plain theme cards with a tinted icon chip
        // and ink text — the colour parameters are ignored so the grid reads
        // editorial, never candy. (Playful's home has its own pastel tiles.)
        private View PlayTile(string icon, string title, string sub, string c1, string c2, Func<System.Threading.Tasks.Task> onTap)
        {
            var col = new VerticalStackLayout { Spacing = 3, Padding = new Thickness(14, 14, 14, 13) };
            var iconWrap = new Border
            {
                WidthRequest = 40, HeightRequest = 40,
                BackgroundColor = Theme.AccentSoft,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Content = Ui.Icon(icon, 22, Theme.Accent),
                Margin = new Thickness(0, 0, 0, 7)
            };
            col.Add(iconWrap);
            col.Add(new Label { Text = title, FontFamily = Ui.Display, FontSize = Ui.S(17), TextColor = Theme.TextPrimary, LineHeight = 1.0 });
            col.Add(new Label { Text = sub, FontFamily = Ui.Fonts, FontSize = Ui.S(11.5), TextColor = Theme.TextSecondary, LineHeight = 1.2 });

            var tile = new Border
            {
                Content = col,
                BackgroundColor = Theme.Surface,
                Stroke = Theme.CardStroke,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = AppLayout.CardRadius },
                Shadow = Theme.CardShadow()
            };
            Ui.OnTap(tile, async (_, _) => await onTap());
            Ui.Describe(tile, $"{title}: {sub}");
            return tile;
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
