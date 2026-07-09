using System;
using System.Linq;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The Settings tab, magazine-style: serif rows separated by hairlines
    // under quiet ALL-CAPS section headers.
    public class SettingsView : ContentView, ITabView
    {
        private Label _journey = null!;
        private HorizontalStackLayout _sizePills = null!;

        public SettingsView() => Build();

        public void OnSelected() => RefreshJourney();

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 6, Padding = new Thickness(18, 16, 18, 28) };

            stack.Add(new Label
            {
                Text = Ui.T("Settings"),
                FontFamily = Ui.Display,
                FontSize = Ui.S(28),
                TextColor = Theme.TextPrimary,
                Margin = new Thickness(0, 0, 0, 6)
            });

            // Your journey
            stack.Add(Ui.SectionHeader("Your journey"));
            _journey = new Label { FontFamily = Ui.Fonts, FontSize = Ui.S(14.5), LineHeight = 1.5, TextColor = Theme.TextPrimary };
            if (AppLayout.Playful)
                stack.Add(new Border
                {
                    Content = _journey, BackgroundColor = Ui.MultiplyAlpha(PlayfulKit.Tab(0), 0.14f),
                    Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 20 },
                    Padding = new Thickness(16, 14), Margin = new Thickness(0, 6, 0, 2)
                });
            else { _journey.Margin = new Thickness(0, 10, 0, 8); stack.Add(_journey); }

            // Visuals — themes and type size.
            stack.Add(Ui.SectionHeader("Visuals"));
            stack.Add(Group(
                LinkRow("Choose a theme", CurrentThemeName(), async () => await Nav.Push(() => new ThemesPage())),
                TextSizeRow()));

            // NovaSaur AI — install, pause/resume, or remove the optional model.
            stack.Add(Ui.SectionHeader("NovaSaur AI"));
            stack.Add(new NovaModelCard(hideWhenInstalled: false) { Margin = new Thickness(0, 8, 0, 4) });

            // General
            stack.Add(Ui.SectionHeader("General"));
            stack.Add(Group(
                HapticRow(),
                LinkRow("Contact us", "dinospace.app@gmail.com", OpenFeedback),
                DangerRow("Reset progress & bookmarks", ConfirmReset)));

            stack.Add(new Label
            {
                Text = "DinoSpace v1.0",
                FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextHint,
                HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0, 22, 0, 0)
            });

            Content = new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            RefreshJourney();
        }

        // A settings group. Playful wraps the rows in one rounded card with
        // faint dividers; the classic layout keeps flat hairline-separated rows.
        private static View Group(params View[] rows)
        {
            var col = new VerticalStackLayout { Spacing = 0 };
            for (int i = 0; i < rows.Length; i++)
            {
                col.Add(rows[i]);
                if (i < rows.Length - 1) col.Add(new BoxView { HeightRequest = 1, Color = Theme.HairlineSoft });
            }
            if (!AppLayout.Playful) return col;
            return new Border
            {
                Content = col, BackgroundColor = Theme.Surface, Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 20 }, Padding = new Thickness(16, 2),
                Margin = new Thickness(0, 4), Shadow = Theme.CardShadow()
            };
        }

        private static Label RowTitle(string text) => new()
        {
            Text = text,
            FontFamily = Ui.Display,
            FontSize = Ui.S(19),
            TextColor = Theme.TextPrimary,
            VerticalOptions = LayoutOptions.Center
        };

        private View SwitchRow(string title, string caption, bool value, Action<bool> onChange)
        {
            var sw = new Switch { IsToggled = value, OnColor = Theme.Accent, ThumbColor = Colors.White, VerticalOptions = LayoutOptions.Center };
            sw.Toggled += (_, e) => onChange(e.Value);

            var text = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
            text.Add(RowTitle(title));
            if (!string.IsNullOrEmpty(caption))
                text.Add(new Label { Text = caption, FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextHint });

            var grid = new Grid { Padding = new Thickness(0, 14), ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(text, 0, 0);
            grid.Add(sw, 1, 0);
            return grid;
        }

        private static string CurrentThemeName()
        {
            foreach (var s in Theme.Wallpapers)
                if (s.Id == Theme.CurrentId) return s.Name;
            return Theme.Wallpapers[0].Name;
        }

        // Off / Light / Medium / Strong — a toggle wasn't enough, people feel
        // vibration very differently phone to phone.
        private HorizontalStackLayout _hapticPills = null!;

        private View HapticRow()
        {
            _hapticPills = new HorizontalStackLayout { Spacing = 8 };
            BuildHapticPills();

            var col = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(0, 14) };
            col.Add(RowTitle("Haptic feedback"));
            col.Add(new Label { Text = "How strong taps and saves feel.", FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextHint });
            col.Add(_hapticPills);
            return col;
        }

        private void BuildHapticPills()
        {
            _hapticPills.Children.Clear();
            string[] labels = { "Off", "Light", "Medium", "Strong" };
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                bool active = AppSettings.HapticLevel == i;
                var label = new Label
                {
                    Text = labels[i],
                    FontFamily = Ui.Fonts, FontSize = 13, FontAttributes = FontAttributes.Bold,
                    TextColor = active ? Theme.TextOnAccent : Theme.ChipText,
                    HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center
                };
                var pill = new Border
                {
                    Content = label, MinimumWidthRequest = 62, HeightRequest = 40,
                    BackgroundColor = active ? Theme.Accent : Theme.ChipBg,
                    Stroke = Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Padding = new Thickness(10, 0)
                };
                // haptic:false — the demo pulse below is the feedback here.
                Ui.OnTap(pill, (_, _) =>
                {
                    AppSettings.HapticLevel = idx;
                    BuildHapticPills();
                    AppSettings.Tap();   // let them feel the level they just picked
                }, haptic: false);
                _hapticPills.Add(pill);
            }
        }

        private View TextSizeRow()
        {
            _sizePills = new HorizontalStackLayout { Spacing = 8 };
            BuildSizePills();

            var col = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(0, 14) };
            col.Add(RowTitle("Text size"));
            col.Add(new Label { Text = "Applies to entries, search, and quizzes.", FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextHint });
            col.Add(_sizePills);
            return col;
        }

        private void BuildSizePills()
        {
            _sizePills.Children.Clear();
            string[] labels = { "S", "M", "L", "XL" };
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                bool active = AppSettings.TextSizeIndex == i;
                var label = new Label
                {
                    Text = labels[i],
                    FontFamily = Ui.Fonts, FontSize = 14, FontAttributes = FontAttributes.Bold,
                    TextColor = active ? Theme.TextOnAccent : Theme.ChipText,
                    HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center
                };
                var pill = new Border
                {
                    Content = label, WidthRequest = 52, HeightRequest = 40,
                    BackgroundColor = active ? Theme.Accent : Theme.ChipBg,
                    Stroke = Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 }
                };
                Ui.OnTap(pill, (_, _) => { AppSettings.TextSizeIndex = idx; BuildSizePills(); });
                _sizePills.Add(pill);
            }
        }

        private View LinkRow(string title, string value, Action onTap)
        {
            var val = new Label { Text = value, FontFamily = Ui.Fonts, FontSize = Ui.S(13), TextColor = Theme.TextSecondary, VerticalOptions = LayoutOptions.Center };
            var grid = new Grid { Padding = new Thickness(0, 14), ColumnSpacing = 10 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(RowTitle(title), 0, 0);
            grid.Add(val, 1, 0);
            Ui.OnTap(grid, (_, _) => onTap());
            return grid;
        }

        private View DangerRow(string title, Action onTap)
        {
            var label = new Label
            {
                Text = title,
                FontFamily = Ui.Display, FontSize = Ui.S(19),
                TextColor = Theme.Danger, VerticalOptions = LayoutOptions.Center
            };
            var grid = new Grid { Padding = new Thickness(0, 14) };
            grid.Add(label);
            Ui.OnTap(grid, (_, _) => onTap());
            return grid;
        }

        private void RefreshJourney()
        {
            int dinos = StatsStore.DinosSeen();
            int space = StatsStore.SpaceSeen();
            int saved = SavedStore.Count;
            int streak = StatsStore.Streak();
            string mostViewed = StatsStore.MostViewedName();

            var sb = new System.Text.StringBuilder();
            sb.Append($"You've explored {dinos} of {DinoData.All.Count} prehistoric creatures and {space} of {SpaceData.All.Count} space objects, ");
            sb.Append($"and saved {saved} {(saved == 1 ? "favourite" : "favourites")}. ");
            if (!string.IsNullOrEmpty(mostViewed)) sb.Append($"Your most-viewed entry is {mostViewed}. ");
            if (streak > 1) sb.Append($"You're on a {streak}-day streak — keep it going!");
            else sb.Append("Come back tomorrow to start a streak!");
            _journey.Text = sb.ToString();
        }

        private async void OpenFeedback()
        {
            try { await Launcher.OpenAsync("mailto:dinospace.app@gmail.com?subject=DinoSpace%20Feedback"); } catch { }
        }

        private async void ConfirmReset()
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return;
            bool sure = await page.DisplayAlertAsync("Reset everything?",
                "This clears your streak, quiz scores, viewed history, bookmarks, and your NovaSaur chat. The encyclopedia and the AI itself stay. This can't be undone.",
                "Reset", "Cancel");
            if (!sure) return;
            StatsStore.ClearProgress();
            SavedStore.ClearAll();
            NovaView.DeleteSavedChat();
            RefreshJourney();
        }
    }
}
