using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Your explorer page, opened from the settings profile card: the mascot
    // profile picture up top, then everything the app knows about your
    // journey — entries discovered, favourites, creations, streak, and the
    // lifetime quiz score in every topic.
    public class ProfilePage : ContentPage
    {
        public ProfilePage()
        {
            BackgroundColor = Theme.Bg;
            Build();
            Shell.SetNavBarIsVisible(this, false);
            SwipeBack.Attach(this);
        }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(20, 4, 20, 30) };

            // The profile picture, big. The pfp art is itself a round badge
            // (cropped tight to its own edge), so it renders at the frame's
            // full size and BECOMES the circle — sized any smaller it floats
            // as a circle-in-a-circle.
            var face = new Border
            {
                WidthRequest = 104, HeightRequest = 104,
                BackgroundColor = Theme.AccentSoft,
                Stroke = Theme.Hairline.WithAlpha(0.5f), StrokeThickness = 1.4,
                StrokeShape = new RoundRectangle { CornerRadius = 52 },
                Content = Ui.Mascot("mascot_pfp", 104),
                HorizontalOptions = LayoutOptions.Center
            };
            stack.Add(face);

            stack.Add(new Label
            {
                Text = "Explorer",
                FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), TextColor = Theme.TextSecondary,
                HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 0, 6)
            });

            // ---- the journey so far ----
            int dinoSeen = StatsStore.DinosSeen(), dinoTotal = DinoData.All.Count;
            int spaceSeen = StatsStore.SpaceSeen(), spaceTotal = SpaceData.All.Count;
            string mostViewed = StatsStore.MostViewedName();
            int saved = SavedStore.Count;
            int creations = CreationStore.All().Count;
            int streak = StatsStore.Streak();

            stack.Add(Ui.SectionHeader("Your journey"));
            stack.Add(Card(DetailUi.StatRows(new[]
            {
                ("Dinosaurs discovered", $"{dinoSeen} of {dinoTotal}"),
                ("Space objects discovered", $"{spaceSeen} of {spaceTotal}"),
                ("Most viewed entry", string.IsNullOrEmpty(mostViewed) ? "None yet" : mostViewed),
                ("Favourites saved", saved.ToString()),
                ("Creations drawn", creations.ToString()),
                ("Day streak", streak > 0 ? $"{streak} {(streak == 1 ? "day" : "days")}" : "Start one today!"),
            })));

            // ---- lifetime quiz scores, one row per topic ----
            stack.Add(Ui.SectionHeader("Quiz scores"));
            stack.Add(Card(QuizRows()));

            var header = BackHeader();
            var main = new Grid { RowSpacing = 0 };
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            main.Add(header, 0, 0);
            main.Add(new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never }, 0, 1);
            Content = Ui.PageRoot(main);
        }

        private View QuizRows()
        {
            var col = new VerticalStackLayout { Spacing = 0 };
            (string label, string mode)[] topics =
            {
                ("Dinosaurs", "Dinosaurs"),
                ("Space", "Space"),
                ("Mixed", "Mixed"),
            };
            for (int i = 0; i < topics.Length; i++)
            {
                var (label, mode) = topics[i];
                var (correct, answered) = StatsStore.QuizTotals(mode);
                string score = answered == 0 ? "Not played yet" : $"{correct} of {answered} right";
                string detail = answered == 0 ? "" : $"{StatsStore.QuizAccuracy(mode)} · best run {StatsStore.QuizBest(mode)}%";

                var left = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
                left.Add(new Label { Text = label, FontFamily = Ui.Display, FontSize = Ui.S(15.5), TextColor = Theme.TextPrimary });
                if (detail.Length > 0)
                    left.Add(new Label { Text = detail, FontFamily = Ui.Fonts, FontSize = Ui.S(11.5), TextColor = Theme.TextHint });

                var grid = new Grid { Padding = new Thickness(0, 12), ColumnSpacing = 12 };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.Add(left, 0, 0);
                grid.Add(new Label
                {
                    Text = score, FontFamily = Ui.Fonts, FontSize = Ui.S(14),
                    TextColor = answered == 0 ? Theme.TextHint : Theme.TextPrimary,
                    VerticalOptions = LayoutOptions.Center
                }, 1, 0);
                col.Add(grid);
                if (i < topics.Length - 1) col.Add(new BoxView { HeightRequest = 1, Color = Theme.HairlineSoft });
            }
            return col;
        }

        private static Border Card(View content) => new()
        {
            Content = content, BackgroundColor = Theme.Surface,
            Stroke = Theme.CardStroke, StrokeThickness = 1.4,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Padding = new Thickness(16, 6), Margin = new Thickness(0, 2, 0, 4),
            Shadow = Theme.CardShadow()
        };

        private View BackHeader()
        {
            var back = Ui.Icon(Ui.IconBack, 24);
            var backWrap = new Border
            {
                Content = back, WidthRequest = 44, HeightRequest = 44,
                BackgroundColor = Colors.Transparent, Stroke = Colors.Transparent
            };
            Ui.OnTap(backWrap, async (_, _) =>
            {
                try { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); } catch { }
            });
            Ui.Describe(backWrap, "Go back");

            var grid = new Grid { Padding = new Thickness(8, 2, 8, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
            grid.Add(backWrap, 0, 0);
            grid.Add(new Label
            {
                Text = "you",
                FontFamily = Ui.Display, FontSize = Ui.S(26), TextColor = Theme.TextPrimary,
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
            }, 1, 0);
            return grid;
        }
    }
}
