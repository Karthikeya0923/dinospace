using System;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The landing tab: a warm welcome, the day's featured creature and object,
    // quick jumps into the app, live progress, and a rotating fact.
    public class HomeView : ContentView, ITabView
    {
        private readonly Action<int> _goTab;

        private Label _levelChip = null!;
        private Label _streakValue = null!, _seenValue = null!, _xpValue = null!;
        private Label _factLabel = null!;
        private Border _featuredCard = null!;
        private bool _showDino = true;
        private Dinosaur _dino = null!;
        private SpaceObject _space = null!;

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
            var stack = new VerticalStackLayout { Spacing = 18, Padding = new Thickness(16, 20, 16, 24) };

            // ----- header -----
            var hi = new VerticalStackLayout { Spacing = 2 };
            hi.Add(new Label { Text = "Welcome to", FontFamily = Ui.Fonts, FontSize = 14, TextColor = Theme.TextSecondary });
            var wordmark = new HorizontalStackLayout { Spacing = 0 };
            wordmark.Add(new Label { Text = "Dino", FontFamily = Ui.Display, FontSize = 34, TextColor = Theme.AccentDino });
            wordmark.Add(new Label { Text = "Space", FontFamily = Ui.Display, FontSize = 34, TextColor = Theme.AccentSpace });
            hi.Add(wordmark);

            _levelChip = new Label { FontFamily = Ui.Fonts, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextOnAccent };
            var levelBadge = new Border
            {
                Content = _levelChip,
                BackgroundColor = Theme.AccentNova,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Padding = new Thickness(12, 6),
                VerticalOptions = LayoutOptions.Center
            };

            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.Add(hi, 0, 0);
            header.Add(levelBadge, 1, 0);
            stack.Add(header);

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
                BackgroundColor = Theme.ImgPlaceholder,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                HeightRequest = 150,
                Margin = new Thickness(0, 10, 0, 12)
            };

            var textCol = new VerticalStackLayout { Spacing = 3 };
            textCol.Add(tag); textCol.Add(name); textCol.Add(sub);

            var openBtn = new Label { Text = "Open  ›", FontFamily = Ui.Fonts, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Theme.AccentDino };
            var flipBtn = new Label { Text = "Flip ⇄", FontFamily = Ui.Fonts, FontSize = 13, TextColor = Theme.TextSecondary };
            var actions = new Grid();
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actions.Add(openBtn, 0, 0);
            actions.Add(flipBtn, 1, 0);
            Ui.OnTap(flipBtn, (_, _) => { _showDino = !_showDino; RefreshFeatured(); });

            var col = new VerticalStackLayout { Spacing = 0 };
            col.Add(textCol);
            col.Add(imgWrap);
            col.Add(actions);

            var card = new Border
            {
                Content = col,
                Background = new LinearGradientBrush(new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb("#2A1E10"), 0f),
                    new GradientStop(Theme.Surface, 1f)
                }, new Point(0, 0), new Point(1, 1)),
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

            // stash references for refresh
            card.BindingContext = new FeaturedRefs(tag, name, sub, img, openBtn, card);
            return card;
        }

        private record FeaturedRefs(Label Tag, Label Name, Label Sub, Image Img, Label Open, Border Card);

        private void RefreshFeatured()
        {
            if (_featuredCard.BindingContext is not FeaturedRefs r) return;
            if (_showDino)
            {
                r.Tag.Text = "DINOSAUR OF THE DAY"; r.Tag.TextColor = Theme.AccentDino;
                r.Name.Text = _dino.Name; r.Sub.Text = _dino.ShortDescription;
                r.Img.Source = _dino.ImageFile; r.Open.TextColor = Theme.AccentDino;
                r.Card.Background = Grad("#2A1E10");
            }
            else
            {
                r.Tag.Text = "SPACE OBJECT OF THE DAY"; r.Tag.TextColor = Theme.AccentSpace;
                r.Name.Text = _space.Name; r.Sub.Text = _space.ShortDescription;
                r.Img.Source = _space.ImageFile; r.Open.TextColor = Theme.AccentSpace;
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

            grid.Add(ActionCard("Dinosaurs", $"{DinoData.All.Count} creatures", Theme.AccentDino, () => { ExploreView.RequestSegment(1); _goTab(1); }), 0, 0);
            grid.Add(ActionCard("Space", $"{SpaceData.All.Count} objects", Theme.AccentSpace, () => { ExploreView.RequestSegment(2); _goTab(1); }), 1, 0);
            grid.Add(ActionCard("Ask Nova AI", "Your offline guide", Theme.AccentNova, () => _goTab(2)), 0, 1);
            grid.Add(ActionCard("Play & Quiz", "Test yourself", Theme.AccentDino, () => _goTab(3)), 1, 1);
            return grid;
        }

        private Border ActionCard(string title, string sub, Color accent, Action onTap)
        {
            var dot = new Border
            {
                WidthRequest = 34, HeightRequest = 34,
                BackgroundColor = Ui.MultiplyAlpha(accent, 0.18f),
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Content = new Border
                {
                    WidthRequest = 14, HeightRequest = 14,
                    BackgroundColor = accent,
                    Stroke = Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = 5 },
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            var col = new VerticalStackLayout { Spacing = 8 };
            col.Add(dot);
            col.Add(new Label { Text = title, FontFamily = Ui.Display, FontSize = 16, TextColor = Theme.TextPrimary });
            col.Add(new Label { Text = sub, FontFamily = Ui.Fonts, FontSize = 12, TextColor = Theme.TextSecondary });

            var card = new Border
            {
                Content = col,
                BackgroundColor = Theme.Surface,
                Stroke = Theme.HairlineSoft,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(14)
            };
            Ui.OnTap(card, (_, _) => onTap());
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
            _xpValue = StatNumber();
            grid.Add(StatTile("Day streak", _streakValue, Theme.AccentDino), 0, 0);
            grid.Add(StatTile("Entries seen", _seenValue, Theme.AccentSpace), 1, 0);
            grid.Add(StatTile("XP earned", _xpValue, Theme.AccentNova), 2, 0);
            return grid;
        }

        private Label StatNumber() => new()
        { FontFamily = Ui.Display, FontSize = 24, TextColor = Theme.TextPrimary };

        private Border StatTile(string label, Label value, Color accent)
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
            _levelChip.Text = $"Level {StatsStore.Level()}";
            _streakValue.Text = StatsStore.Streak().ToString();
            _seenValue.Text = (StatsStore.DinosSeen() + StatsStore.SpaceSeen()).ToString();
            _xpValue.Text = StatsStore.Xp().ToString();
        }
    }
}
