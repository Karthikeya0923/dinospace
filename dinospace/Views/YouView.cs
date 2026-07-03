using System;
using System.Linq;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The profile tab: level and XP, a friendly stats summary, bookmarks, and
    // settings (haptics, text size, about, clear data).
    public class YouView : ContentView, ITabView
    {
        private Label _levelLabel = null!, _xpLabel = null!, _statsSummary = null!;
        private ProgressBar _xpBar = null!;
        private VerticalStackLayout _savedArea = null!;
        private HorizontalStackLayout _sizePills = null!;

        public YouView() => Build();

        public void OnSelected()
        {
            RefreshHeader();
            RefreshStats();
            RefreshSaved();
        }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(16, 20, 16, 24) };

            stack.Add(new Label { Text = "You", FontFamily = Ui.Display, FontSize = 28, TextColor = Theme.TextPrimary });

            // level card
            _levelLabel = new Label { FontFamily = Ui.Display, FontSize = 20, TextColor = Theme.TextPrimary };
            _xpLabel = new Label { FontFamily = Ui.Fonts, FontSize = 12.5, TextColor = Theme.TextSecondary };
            _xpBar = new ProgressBar { HeightRequest = 8, ProgressColor = Theme.AccentNova };
            var levelCol = new VerticalStackLayout { Spacing = 8 };
            var levelTop = new Grid();
            levelTop.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            levelTop.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            levelTop.Add(_levelLabel, 0, 0);
            levelTop.Add(_xpLabel, 1, 0);
            levelCol.Add(levelTop);
            levelCol.Add(_xpBar);
            stack.Add(DetailUi.Card(levelCol));

            // stats summary
            stack.Add(Ui.Overline("Your journey"));
            _statsSummary = new Label { FontFamily = Ui.Fonts, FontSize = 14.5, LineHeight = 1.5, TextColor = Theme.TextPrimary };
            stack.Add(DetailUi.Card(_statsSummary));

            // saved
            stack.Add(Ui.Overline("Saved"));
            _savedArea = new VerticalStackLayout { Spacing = 8 };
            stack.Add(_savedArea);

            // settings
            stack.Add(Ui.Overline("Settings"));
            stack.Add(HapticsRow());
            stack.Add(TextSizeRow());
            stack.Add(SimpleRow("About DinoSpace", async () => await Nav.Push(new AboutPage())));
            stack.Add(SimpleRow("Send feedback", OpenFeedback));
            stack.Add(ClearRow());

            stack.Add(new Label { Text = "DinoSpace v2.0 · Made with curiosity", FontFamily = Ui.Fonts, FontSize = 11.5, TextColor = Theme.TextHint, HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0, 8) });

            Content = new ScrollView { Content = stack };
            OnSelected();
        }

        private void RefreshHeader()
        {
            _levelLabel.Text = $"Level {StatsStore.Level()}";
            int into = StatsStore.Xp() % 100;
            _xpLabel.Text = $"{into} / 100 XP to next level";
            _xpBar.Progress = StatsStore.LevelProgress();
        }

        private void RefreshStats()
        {
            int dinos = StatsStore.DinosSeen();
            int space = StatsStore.SpaceSeen();
            int saved = SavedStore.Count;
            int streak = StatsStore.Streak();
            string mostViewed = StatsStore.MostViewedName();
            string faveCategory = dinos >= space ? "dinosaurs" : "space";

            var sb = new System.Text.StringBuilder();
            sb.Append($"You've explored {dinos} of {DinoData.All.Count} dinosaurs and {space} of {SpaceData.All.Count} space objects, ");
            sb.Append($"and bookmarked {saved} {(saved == 1 ? "item" : "items")}. ");
            if (!string.IsNullOrEmpty(mostViewed)) sb.Append($"Your most-viewed entry is {mostViewed}. ");
            sb.Append($"You lean toward {faveCategory}! ");
            if (streak > 1) sb.Append($"You're on a {streak}-day streak — keep it up! 🔥");
            else sb.Append("Come back tomorrow to build a daily streak! 🔥");
            _statsSummary.Text = sb.ToString();
        }

        private void RefreshSaved()
        {
            _savedArea.Children.Clear();
            var dinoNames = SavedStore.Dinos;
            var spaceNames = SavedStore.Space;

            if (dinoNames.Count == 0 && spaceNames.Count == 0)
            {
                _savedArea.Add(new Border
                {
                    Content = new Label { Text = "No bookmarks yet. Tap the ☆ on any dinosaur or space object to save it here.", FontFamily = Ui.Fonts, FontSize = 13.5, LineHeight = 1.4, TextColor = Theme.TextSecondary, HorizontalTextAlignment = TextAlignment.Center },
                    BackgroundColor = Theme.Surface, Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(18)
                });
                return;
            }

            foreach (var name in dinoNames)
            {
                var d = DinoData.ByName(name);
                if (d != null) _savedArea.Add(SavedRow(d.ImageFile, d.Name, d.ShortDescription, Theme.AccentDino, async () => await Nav.OpenDino(d)));
            }
            foreach (var name in spaceNames)
            {
                var s = SpaceData.ByName(name);
                if (s != null) _savedArea.Add(SavedRow(s.ImageFile, s.Name, s.ShortDescription, Theme.AccentSpace, async () => await Nav.OpenSpace(s)));
            }
        }

        private View SavedRow(string image, string name, string sub, Color accent, Func<System.Threading.Tasks.Task> onTap)
        {
            var thumb = new Border
            {
                Content = new Image { Source = image, Aspect = Aspect.AspectFill, WidthRequest = 48, HeightRequest = 48 },
                WidthRequest = 48, HeightRequest = 48, BackgroundColor = Theme.ImgPlaceholder,
                Stroke = Theme.HairlineSoft, StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 12 }
            };
            var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            info.Add(new Label { Text = name, FontFamily = Ui.Display, FontSize = 15.5, TextColor = Theme.TextPrimary });
            info.Add(new Label { Text = sub, FontFamily = Ui.Fonts, FontSize = 12, TextColor = Theme.TextSecondary, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation });

            var grid = new Grid { ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.Add(thumb, 0, 0); grid.Add(info, 1, 0);

            var card = new Border
            {
                Content = grid,
                BackgroundColor = Theme.Surface, Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 14 }, Padding = new Thickness(10)
            };
            Ui.OnTap(card, async (_, _) => await onTap());
            return card;
        }

        // ----- settings rows -----
        private View HapticsRow()
        {
            var sw = new Switch { IsToggled = AppSettings.Haptics, OnColor = Theme.AccentNova, ThumbColor = Theme.TextPrimary, VerticalOptions = LayoutOptions.Center };
            sw.Toggled += (_, e) => { AppSettings.Haptics = e.Value; if (e.Value) AppSettings.Tap(); };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(SettingLabel("Haptic feedback"), 0, 0);
            grid.Add(sw, 1, 0);
            return SettingCard(grid);
        }

        private View TextSizeRow()
        {
            _sizePills = new HorizontalStackLayout { Spacing = 8 };
            var col = new VerticalStackLayout { Spacing = 10 };
            col.Add(SettingLabel("Text size"));
            col.Add(new Label { Text = "Applies to detail pages, the encyclopedia and quizzes. Reopen a screen to see the change.", FontFamily = Ui.Fonts, FontSize = 11.5, TextColor = Theme.TextHint });
            col.Add(_sizePills);
            BuildSizePills();
            return SettingCard(col);
        }

        private void BuildSizePills()
        {
            _sizePills.Children.Clear();
            string[] labels = { "S", "M", "L", "XL" };
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                bool active = AppSettings.TextSizeIndex == i;
                var label = new Label { Text = labels[i], FontFamily = Ui.Fonts, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = active ? Theme.TextOnAccent : Theme.ChipText, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
                var pill = new Border
                {
                    Content = label, WidthRequest = 52, HeightRequest = 40,
                    BackgroundColor = active ? Theme.AccentNova : Theme.ChipBg,
                    Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 12 }
                };
                Ui.OnTap(pill, (_, _) => { AppSettings.TextSizeIndex = idx; BuildSizePills(); });
                _sizePills.Add(pill);
            }
        }

        private View SimpleRow(string title, Action onTap)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(SettingLabel(title), 0, 0);
            grid.Add(new Label { Text = "›", FontSize = 22, TextColor = Theme.TextHint, VerticalOptions = LayoutOptions.Center }, 1, 0);
            var card = SettingCard(grid);
            Ui.OnTap(card, (_, _) => onTap());
            return card;
        }

        private View ClearRow()
        {
            var label = new Label { Text = "Reset progress & bookmarks", FontFamily = Ui.Fonts, FontSize = 15, TextColor = Theme.Danger, VerticalOptions = LayoutOptions.Center };
            var card = SettingCard(label);
            Ui.OnTap(card, async (_, _) =>
            {
                var page = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (page == null) return;
                bool sure = await page.DisplayAlert("Reset everything?", "This clears your XP, streak, quiz scores, viewed history, and bookmarks. NovaSaur and the encyclopedia stay. This can't be undone.", "Reset", "Cancel");
                if (!sure) return;
                StatsStore.ClearProgress();
                SavedStore.ClearAll();
                OnSelected();
            });
            return card;
        }

        private Label SettingLabel(string text) => new()
        { Text = text, FontFamily = Ui.Fonts, FontSize = 15, TextColor = Theme.TextPrimary, VerticalOptions = LayoutOptions.Center };

        private Border SettingCard(View content) => new()
        {
            Content = content,
            BackgroundColor = Theme.Surface, Stroke = Theme.HairlineSoft, StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 }, Padding = new Thickness(16, 14)
        };

        private async void OpenFeedback()
        {
            try { await Launcher.OpenAsync("mailto:dinospace.app@gmail.com?subject=DinoSpace%20Feedback"); }
            catch { }
        }
    }
}
