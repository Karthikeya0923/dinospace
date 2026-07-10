using System;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Media;

namespace dinospace.Views
{
    // Pick a look for the whole app. Each theme is a wallpaper + matching
    // colours, applied with the same freeze-frame cross-fade as dark mode —
    // and you land right back on this page, not on Home.
    public class ThemesPage : ContentPage
    {
        private bool _switching;

        public ThemesPage()
        {
            Build();
            SwipeBack.Attach(this);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // If we just got rebuilt for a new theme, the old screen is still
            // frozen on top — dissolve it now that we're back.
            ThemeFx.FadeOutThemeCover();
        }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(18, 4, 18, 28) };

            stack.Add(new Label
            {
                Text = "pick a page for every screen of dinospace — wallpaper, colours, the lot.",
                FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), LineHeight = 1.4, TextColor = Theme.TextSecondary,
                Margin = new Thickness(0, 0, 0, 6)
            });

            foreach (var spec in Theme.Wallpapers)
                stack.Add(WallpaperCard(spec));

            // text size lives here on the appearance page now
            stack.Add(Ui.SectionHeader("Text size"));
            stack.Add(new Label { Text = "Applies to entries, search, and quizzes.", FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextHint });
            _sizePills = new HorizontalStackLayout { Spacing = 8, Margin = new Thickness(0, 2, 0, 8) };
            BuildSizePills();
            stack.Add(_sizePills);

            var body = Nav.DetailScaffold("app themes", stack, Theme.Accent, out _);
            Content = Ui.PageRoot(body);
        }

        private HorizontalStackLayout _sizePills = null!;

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

        private View WallpaperCard(Theme.Spec spec)
        {
            bool current = Theme.CurrentId == spec.Id;

            var thumb = new Grid { BackgroundColor = ThemePreviewBg(spec) };
            if (spec.Wallpaper != null)
                thumb.Add(new Image { Source = spec.Wallpaper, Aspect = Aspect.AspectFill });

            var preview = new Border
            {
                WidthRequest = 84, HeightRequest = 84,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Padding = 0,
                Content = thumb
            };
            return ThemeCard(preview, spec.Name, spec.Blurb + ".", current, () => Apply(spec.Id));
        }

        private View ThemeCard(View preview, string name, string blurb, bool current, Action onPick)
        {
            var info = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
            info.Add(new Label { Text = name, FontFamily = Ui.Display, FontSize = Ui.S(19), TextColor = Theme.TextPrimary });
            info.Add(new Label { Text = blurb, FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), LineHeight = 1.3, TextColor = Theme.TextSecondary });
            if (current)
            {
                var chip = Ui.Chip("Current", Theme.AccentSoft, Theme.Accent);
                chip.HorizontalOptions = LayoutOptions.Start;
                chip.Margin = new Thickness(0, 4, 0, 0);
                info.Add(chip);
            }

            var grid = new Grid { ColumnSpacing = 14 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.Add(preview, 0, 0);
            grid.Add(info, 1, 0);

            var card = Ui.Card(grid, 18, new Thickness(14, 12));
            Ui.OnTap(card, (_, _) => onPick());
            Ui.Describe(card, $"{name} theme{(current ? ", current" : "")}");
            return card;
        }

        private static Color ThemePreviewBg(Theme.Spec spec) => spec.Id switch
        {
            "dinospace" => Color.FromArgb("#221338"),
            _ => Color.FromArgb("#EEF1E2"),
        };

        // Same trick as the dark-mode toggle: freeze the screen, rebuild the
        // whole app in the new theme underneath, then come straight back here
        // and dissolve the freeze-frame. No flash, no losing your place.
        private async void Apply(string id)
        {
            if (_switching || id == Theme.CurrentId) return;
            _switching = true;
            AppSettings.Tap();

            byte[]? snap = null;
            try
            {
                if (Screenshot.Default.IsCaptureSupported)
                {
                    var result = await Screenshot.Default.CaptureAsync();
                    using var s = await result.OpenReadAsync();
                    using var ms = new System.IO.MemoryStream();
                    await s.CopyToAsync(ms);
                    snap = ms.ToArray();
                }
            }
            catch { }
            if (snap != null) ThemeFx.ShowThemeCover(snap);

            AppSettings.ThemeId = id;
            Theme.ApplyCurrent();
            NovaPage.ResetShared();

            // Rebuild on the "more" tab (settings lives behind it) and hold
            // the freeze-frame until this page is pushed back on top.
            RootPage.LastTab = 4;
            RootPage.HoldThemeCoverOnce = true;
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window != null) window.Page = new AppShell();

            Dispatcher.Dispatch(async () =>
            {
                await System.Threading.Tasks.Task.Delay(80);
                await Nav.Push(() => new ThemesPage(), animated: false);
            });
        }
    }
}
