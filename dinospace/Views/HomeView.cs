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

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 18, Padding = new Thickness(18, 10, 18, 28) };

            stack.Add(Masthead());

            var hello = new Label
            {
                Text = "Let's explore.",
                FontFamily = Ui.Display,
                FontSize = Ui.S(28),
                TextColor = Theme.TextSecondary
            };
            stack.Add(hello);

            _featuredCard = BuildFeatured();
            stack.Add(_featuredCard);

            // Reserved for the upcoming "Scan Sky" feature.
            stack.Add(Ui.SectionHeader("Your sky"));
            stack.Add(VisibleRightNowPlaceholder());

            // Dinosaurs
            stack.Add(Ui.SectionHeader("Dinosaurs", "View all", async (_, _) => await Nav.Push(new BrowsePage("Dinosaurs"))));
            stack.Add(EntryCards.TwoColumn(new (string, string, string, Action)[]
            {
                (Item(DinoData.ByName("Tyrannosaurus Rex"))),
                (Item(DinoData.ByName("Spinosaurus"))),
                (Item(DinoData.ByName("Triceratops"))),
                (Item(DinoData.ByName("Velociraptor"))),
            }));

            // Space
            stack.Add(Ui.SectionHeader("Space", "View all", async (_, _) => await Nav.Push(new BrowsePage("Space"))));
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
            stack.Add(PlayRow(Ui.IconBolt, "Dino Battle", "Two creatures face off", async () => await Nav.Push(new BattlePage(null))));
            stack.Add(PlayRow(Ui.IconList, "Collections", "Curated ranked lists", async () => await Nav.Push(new CollectionsListPage())));

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

        // Teaser card for the upcoming Scan Sky feature.
        private View VisibleRightNowPlaceholder()
        {
            var col = new VerticalStackLayout { Spacing = 8 };
            col.Add(Ui.Icon(Ui.IconSearch, 30, Theme.Accent));
            col.Add(new Label
            {
                Text = "Coming soon",
                FontFamily = Ui.Display, FontSize = Ui.S(19), TextColor = Theme.TextPrimary
            });
            col.Add(new Label
            {
                Text = "Point your phone at the night sky to spot the planets and stars above you.",
                FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), LineHeight = 1.4, TextColor = Theme.TextSecondary
            });
            return Ui.Card(col, radius: 18, padding: new Thickness(18, 16));
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
            var img = new Image { Source = _dino.ImageFile, Aspect = Aspect.AspectFill, HeightRequest = 250 };
            var imgWrap = new Grid { HeightRequest = 250, BackgroundColor = Theme.ImgPlaceholder };
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

            card.BindingContext = new FeaturedRefs(tag, name, sub, img);
            return card;
        }

        private record FeaturedRefs(Label Tag, Label Name, Label Sub, Image Img);

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
            col.Add(Ui.PrimaryButton("ASK A QUESTION", async (_, _) => await Nav.Push(new NovaPage())));
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

        private async System.Threading.Tasks.Task StartQuiz()
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return;
            string mode = await page.DisplayActionSheet("Pick a quiz", "Cancel", null, "Dinosaurs", "Space", "Mixed");
            if (mode is not ("Dinosaurs" or "Space" or "Mixed")) return;
            string choice = await page.DisplayActionSheet("How many questions?", "Cancel", null, "5", "10", "25", "50", "100");
            if (!int.TryParse(choice, out int count)) return;
            await Nav.Push(new QuizPage(mode, count));
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
