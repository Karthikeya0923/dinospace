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

            stack.Add(new Label { Text = "App themes", FontFamily = Ui.Display, FontSize = Ui.S(30), TextColor = Theme.TextPrimary });
            stack.Add(new Label
            {
                Text = "Pick a look for every page of DinoSpace — wallpaper, colours, the lot.",
                FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), LineHeight = 1.4, TextColor = Theme.TextSecondary,
                Margin = new Thickness(0, 0, 0, 6)
            });

            foreach (var spec in Theme.Wallpapers)
                stack.Add(WallpaperCard(spec));

            var body = Nav.DetailScaffold("", stack, Theme.Accent, out _);
            Content = Ui.PageRoot(body);
        }

        private View WallpaperCard(Theme.Spec spec)
        {
            bool current = Theme.CurrentId == spec.Id;

            // Wallpaper thumbnail; if the file isn't there yet (theme6 before
            // the art is added) the theme's own colours stand in.
            var thumb = new Grid { BackgroundColor = ThemePreviewBg(spec) };
            thumb.Add(new Label
            {
                Text = "✶",
                FontSize = 24, TextColor = ThemePreviewAccent(spec),
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
            });
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
            "theme2" => Color.FromArgb("#0A0908"),
            "theme3" => Color.FromArgb("#1B1233"),
            "theme4" => Color.FromArgb("#070B14"),
            "theme5" => Color.FromArgb("#05100E"),
            "theme6" => Color.FromArgb("#120826"),
            "theme7" => Color.FromArgb("#221338"),
            _ => Color.FromArgb("#F6EFE2"),
        };

        private static Color ThemePreviewAccent(Theme.Spec spec) => spec.Id switch
        {
            "theme2" => Color.FromArgb("#E3BE55"),
            "theme3" => Color.FromArgb("#F08A3C"),
            "theme4" => Color.FromArgb("#7FB4FF"),
            "theme5" => Color.FromArgb("#4FE0B0"),
            "theme6" => Color.FromArgb("#D98CFF"),
            "theme7" => Color.FromArgb("#EDC46B"),
            _ => Color.FromArgb("#A5652A"),
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

            // Rebuild on the Settings tab and hold the freeze-frame until this
            // page is pushed back on top.
            RootPage.LastTab = 3;
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
