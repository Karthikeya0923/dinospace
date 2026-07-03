using System;
using System.Linq;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The activities hub: quizzes with size options, curated collections,
    // dino battles, and a rotating fact.
    public class PlayView : ContentView, ITabView
    {
        private readonly Action<int> _goTab;
        private Label _dinoBest = null!, _spaceBest = null!, _mixedBest = null!;
        private Label _fact = null!;

        public PlayView(Action<int> goTab)
        {
            _goTab = goTab;
            Build();
        }

        public void OnSelected() => RefreshBests();

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(16, 20, 16, 24) };

            stack.Add(new Label { Text = "Play", FontFamily = Ui.Display, FontSize = 28, TextColor = Theme.TextPrimary });

            // quizzes
            stack.Add(Ui.Overline("Quiz yourself"));
            _dinoBest = BestLabel(); _spaceBest = BestLabel(); _mixedBest = BestLabel();
            stack.Add(QuizCard("Dinosaur Quiz", "Test your prehistoric knowledge", Theme.AccentDino, "Dinosaurs", _dinoBest));
            stack.Add(QuizCard("Space Quiz", "Journey through the cosmos", Theme.AccentSpace, "Space", _spaceBest));
            stack.Add(QuizCard("Mixed Quiz", "A bit of everything", Theme.AccentNova, "Mixed", _mixedBest));

            // battle
            stack.Add(Ui.Overline("Face off"));
            stack.Add(BattleCard());

            // collections
            stack.Add(Ui.Overline("Curated collections"));
            foreach (var c in CollectionData.All)
                stack.Add(CollectionCard(c));

            // fact
            stack.Add(FactCard());

            Content = new ScrollView { Content = stack };
            RefreshBests();
        }

        private Label BestLabel() => new() { FontFamily = Ui.Fonts, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextHint };

        private View QuizCard(string title, string sub, Color accent, string mode, Label best)
        {
            var text = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            text.Add(new Label { Text = title, FontFamily = Ui.Display, FontSize = 17, TextColor = Theme.TextPrimary });
            text.Add(new Label { Text = sub, FontFamily = Ui.Fonts, FontSize = 12.5, TextColor = Theme.TextSecondary });
            text.Add(best);

            var play = new Border
            {
                WidthRequest = 46, HeightRequest = 46,
                BackgroundColor = Ui.MultiplyAlpha(accent, 0.18f),
                Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 23 },
                VerticalOptions = LayoutOptions.Center,
                Content = new Label { Text = "▶", FontSize = 16, TextColor = accent, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }
            };

            var grid = new Grid { ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(text, 0, 0); grid.Add(play, 1, 0);

            var card = new Border
            {
                Content = grid,
                BackgroundColor = Theme.Surface, Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(16, 14)
            };
            Ui.OnTap(card, async (_, _) => await StartQuiz(mode));
            return card;
        }

        private async System.Threading.Tasks.Task StartQuiz(string mode)
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return;
            string choice = await page.DisplayActionSheet("How many questions?", "Cancel", null, "5", "10", "25", "50", "100");
            if (!int.TryParse(choice, out int count)) return;
            await Nav.Push(new QuizPage(mode, count));
        }

        private View BattleCard()
        {
            var text = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            text.Add(new Label { Text = "Dino Battle", FontFamily = Ui.Display, FontSize = 17, TextColor = Theme.TextPrimary });
            text.Add(new Label { Text = "Pick two creatures and see who wins", FontFamily = Ui.Fonts, FontSize = 12.5, TextColor = Theme.TextSecondary });

            var icon = new Label { Text = "⚔", FontSize = 26, TextColor = Theme.AccentDino, VerticalOptions = LayoutOptions.Center };
            var grid = new Grid { ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(text, 0, 0); grid.Add(icon, 1, 0);

            var card = new Border
            {
                Content = grid,
                Background = new LinearGradientBrush(new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb("#2A1E10"), 0f),
                    new GradientStop(Theme.Surface, 1f)
                }, new Point(0, 0), new Point(1, 1)),
                Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(16, 16)
            };
            Ui.OnTap(card, async (_, _) => await Nav.Push(new BattlePage(null)));
            return card;
        }

        private View CollectionCard(Collection c)
        {
            var accent = c.Domain == "Space" ? Theme.AccentSpace : Theme.AccentDino;
            var text = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            text.Add(new Label { Text = c.Title, FontFamily = Ui.Display, FontSize = 16, TextColor = Theme.TextPrimary });
            text.Add(new Label { Text = c.Subtitle, FontFamily = Ui.Fonts, FontSize = 12, TextColor = Theme.TextSecondary });

            var chevron = new Label { Text = "›", FontSize = 22, TextColor = Theme.TextHint, VerticalOptions = LayoutOptions.Center };
            var bar = new Border { WidthRequest = 4, HeightRequest = 34, BackgroundColor = accent, Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 2 }, VerticalOptions = LayoutOptions.Center };

            var grid = new Grid { ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(bar, 0, 0); grid.Add(text, 1, 0); grid.Add(chevron, 2, 0);

            var card = new Border
            {
                Content = grid,
                BackgroundColor = Theme.Surface, Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(14, 14)
            };
            Ui.OnTap(card, async (_, _) => await Nav.Push(new CollectionPage(c.Id)));
            return card;
        }

        private View FactCard()
        {
            _fact = new Label { Text = FactData.Random(), FontFamily = Ui.Fonts, FontSize = 14, LineHeight = 1.4, TextColor = Theme.TextPrimary };
            var col = new VerticalStackLayout { Spacing = 8 };
            col.Add(new Label { Text = "DID YOU KNOW?", FontFamily = Ui.Fonts, FontSize = 11, FontAttributes = FontAttributes.Bold, CharacterSpacing = 1.2, TextColor = Theme.AccentNova });
            col.Add(_fact);
            col.Add(new Label { Text = "Tap for another", FontFamily = Ui.Fonts, FontSize = 12, TextColor = Theme.TextHint });
            var card = new Border
            {
                Content = col,
                BackgroundColor = Theme.Surface, Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(16)
            };
            Ui.OnTap(card, (_, _) => _fact.Text = FactData.Random());
            return card;
        }

        private void RefreshBests()
        {
            _dinoBest.Text = $"Best: {StatsStore.QuizBest("Dinosaurs")}%   ·   Accuracy: {StatsStore.QuizAccuracy("Dinosaurs")}";
            _spaceBest.Text = $"Best: {StatsStore.QuizBest("Space")}%   ·   Accuracy: {StatsStore.QuizAccuracy("Space")}";
            _mixedBest.Text = $"Best: {StatsStore.QuizBest("Mixed")}%   ·   Accuracy: {StatsStore.QuizAccuracy("Mixed")}";
        }
    }
}
