using System;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The landing tab: app logo, the day's featured creature/object, quick
    // jumps into the app, live progress, and a rotating fact.
    public class HomeView : ContentView, ITabView
    {
        private readonly Action<int> _goTab;

        private Label _streakValue = null!, _seenValue = null!, _savedValue = null!;
        private Label _factLabel = null!;
        private Border _featuredCard = null!;
        private bool _showDino = true;
        private readonly Dinosaur _dino;
        private readonly SpaceObject _space;

        public HomeView(Action<int> goTab)
        {
            _goTab = goTab;
            _dino = DinoData.All[DateTime.Now.DayOfYear % DinoData.All.Count];
            _space = SpaceData.All[DateTime.Now.DayOfYear % SpaceData.All.Count];
            Build();
        }

        public void OnSelected()
        {
            StatsStore.UpdateStreak();
            RefreshProgress();
            RefreshFeatured();
        }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 18, Padding = new Thickness(16, 16, 16, 24) };

            // ----- logo header (friend-supplied art; blank until added) -----
            var logo = new Image
            {
                Source = "mainlogo.png",
                Aspect = Aspect.AspectFit,
                HeightRequest = 96,
                HorizontalOptions = LayoutOptions.Center
            };
            Ui.Describe(logo, "DinoSpace");
            stack.Add(logo);

            // ----- featured of the day -----
            _featuredCard = BuildFeatured();
            stack.Add(_featuredCard);

            // ----- quick actions -----
            stack.Add(Ui.Overline("Jump in"));
            stack.Add(QuickActions());

            // ----- progress -----
            stack.Add(ProgressRow());

            // ----- fact -----
            stack.Add(FactCard());

            Content = new ScrollView { Content = stack };
            RefreshProgress();
            RefreshFeatured();
        }

        private Border BuildFeatured()
        {
            var tag = new Label { Text = "DINOSAUR OF THE DAY", FontFamily = Ui.Fonts, FontSize = 11, FontAttributes = FontAttributes.Bold, CharacterSpacing = 1.2, TextColor = Theme.AccentDino };
            var name = new Label { Text = _dino.Name, FontFamily = Ui.Display, FontSize = 26, TextColor = Theme.TextPrimary };
            var sub = new Label { Text = _dino.ShortDescription, FontFamily = Ui.Fonts, FontSize = 13.5, TextColor = Theme.TextSecondary };

            var img = new Image { Source = _dino.ImageFile, Aspect = Aspect.AspectFill, HeightRequest = 150 };
            var imgWrap = new Border
            {
                Content = img,
                BackgroundColor = Colors.Transparent,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                HeightRequest = 150,
                Margin = new Thickness(0, 10, 0, 12)
            };

            var textCol = new VerticalStackLayout { Spacing = 3 };
            textCol.Add(tag); textCol.Add(name); textCol.Add(sub);

            // Tapping the card opens the entry; Flip switches dino <-> space.
            var flipBtn = new Label { Text = "Flip ⇄", FontFamily = Ui.Fonts, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextSecondary, HorizontalOptions = LayoutOptions.End };
            Ui.OnTap(flipBtn, (_, _) => { _showDino = !_showDino; RefreshFeatured(); });

            var col = new VerticalStackLayout { Spacing = 0 };
            col.Add(textCol);
            col.Add(imgWrap);
            col.Add(flipBtn);

            var card = new Border
            {
                Content = col,
                Background = Grad("#2A1E10"),
                Stroke = Theme.HairlineSoft,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Padding = new Thickness(16)
            };
            Ui.OnTap(card, async (_, _) =>
            {
                if (_showDino) await Nav.OpenDino(_dino);
                else await Nav.OpenSpace(_space);
            });

            card.BindingContext = new FeaturedRefs(tag, name, sub, img, card);
            return card;
        }

        private record FeaturedRefs(Label Tag, Label Name, Label Sub, Image Img, Border Card);

        private void RefreshFeatured()
        {
            if (_featuredCard.BindingContext is not FeaturedRefs r) return;
            if (_showDino)
            {
                r.Tag.Text = "DINOSAUR OF THE DAY"; r.Tag.TextColor = Theme.AccentDino;
                r.Name.Text = _dino.Name; r.Sub.Text = _dino.ShortDescription;
                r.Img.Source = _dino.ImageFile;
                r.Card.Background = Grad("#2A1E10");
            }
            else
            {
                r.Tag.Text = "SPACE OBJECT OF THE DAY"; r.Tag.TextColor = Theme.AccentSpace;
                r.Name.Text = _space.Name; r.Sub.Text = _space.ShortDescription;
                r.Img.Source = _space.ImageFile;
                r.Card.Background = Grad("#1B2050");
            }
        }

        private static Brush Grad(string top) => new LinearGradientBrush(new GradientStopCollection
        {
            new GradientStop(Color.FromArgb(top), 0f),
            new GradientStop(Theme.Surface, 1f)
        }, new Point(0, 0), new Point(1, 1));

        private View QuickActions()
        {
            var grid = new Grid { ColumnSpacing = 12, RowSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            grid.Add(ActionCard("Dinosaurs", "dinopedialogo.png", () => { ExploreView.RequestSegment(1); _goTab(1); }), 0, 0);
            grid.Add(ActionCard("Space", "spacepedialogo.png", () => { ExploreView.RequestSegment(2); _goTab(1); }), 1, 0);
            grid.Add(ActionCard("Ask Nova AI", "askailogo.png", async () => await Nav.Push(new NovaPage())), 0, 1);
            grid.Add(ActionCard("Play & Quiz", "quizlogo.png", () => _goTab(2)), 1, 1);
            return grid;
        }

        // Square: friend-supplied logo image on top, title below. Nothing else.
        private Border ActionCard(string title, string image, Action onTap)
        {
            var img = new Image
            {
                Source = image,
                Aspect = Aspect.AspectFit,
                HeightRequest = 64,
                HorizontalOptions = LayoutOptions.Center
            };

            var col = new VerticalStackLayout { Spacing = 10 };
            col.Add(img);
            col.Add(new Label { Text = title, FontFamily = Ui.Display, FontSize = 16, TextColor = Theme.TextPrimary, HorizontalTextAlignment = TextAlignment.Center });

            var card = new Border
            {
                Content = col,
                BackgroundColor = Theme.Surface,
                Stroke = Theme.HairlineSoft,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(14, 16)
            };
            Ui.OnTap(card, (_, _) => onTap());
            Ui.Describe(card, title);
            return card;
        }

        private View ProgressRow()
        {
            var grid = new Grid { ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            _streakValue = StatNumber();
            _seenValue = StatNumber();
            _savedValue = StatNumber();
            grid.Add(StatTile("Day streak", _streakValue), 0, 0);
            grid.Add(StatTile("Entries seen", _seenValue), 1, 0);
            grid.Add(StatTile("Bookmarks", _savedValue), 2, 0);
            return grid;
        }

        private Label StatNumber() => new()
        { FontFamily = Ui.Display, FontSize = 24, TextColor = Theme.TextPrimary };

        private Border StatTile(string label, Label value)
        {
            var col = new VerticalStackLayout { Spacing = 2, HorizontalOptions = LayoutOptions.Center };
            col.Add(value);
            col.Add(new Label { Text = label, FontFamily = Ui.Fonts, FontSize = 11, TextColor = Theme.TextSecondary, HorizontalTextAlignment = TextAlignment.Center });
            return new Border
            {
                Content = col,
                BackgroundColor = Theme.Surface,
                Stroke = Theme.HairlineSoft,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Padding = new Thickness(8, 14)
            };
        }

        private View FactCard()
        {
            _factLabel = new Label { Text = FactData.Random(), FontFamily = Ui.Fonts, FontSize = 14, LineHeight = 1.4, TextColor = Theme.TextPrimary };
            var col = new VerticalStackLayout { Spacing = 8 };
            col.Add(new Label { Text = "DID YOU KNOW?", FontFamily = Ui.Fonts, FontSize = 11, FontAttributes = FontAttributes.Bold, CharacterSpacing = 1.2, TextColor = Theme.AccentNova });
            col.Add(_factLabel);
            col.Add(new Label { Text = "Tap for another", FontFamily = Ui.Fonts, FontSize = 12, TextColor = Theme.TextHint });

            var card = new Border
            {
                Content = col,
                BackgroundColor = Theme.Surface,
                Stroke = Theme.HairlineSoft,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(16)
            };
            Ui.OnTap(card, (_, _) => _factLabel.Text = FactData.Random());
            return card;
        }

        private void RefreshProgress()
        {
            _streakValue.Text = StatsStore.Streak().ToString();
            _seenValue.Text = (StatsStore.DinosSeen() + StatsStore.SpaceSeen()).ToString();
            _savedValue.Text = SavedStore.Count.ToString();
        }
    }
}
