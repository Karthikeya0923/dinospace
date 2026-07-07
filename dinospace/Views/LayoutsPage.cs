using System;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Media;

namespace dinospace.Views
{
    // Choose how the whole app is laid out: the grown-up "Native" look or the
    // big, rounded, kid-first "Playful" look. Each card shows a little live
    // glimpse of that layout, and switching uses the same freeze-frame
    // cross-fade as the theme picker, landing you right back here.
    public class LayoutsPage : ContentPage
    {
        private bool _switching;

        public LayoutsPage()
        {
            Build();
            SwipeBack.Attach(this);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ThemeFx.FadeOutThemeCover();
        }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(18, 4, 18, 28) };

            stack.Add(new Label { Text = "Choose a layout", FontFamily = Ui.Display, FontSize = Ui.S(30), TextColor = Theme.TextPrimary });
            stack.Add(new Label
            {
                Text = "Two totally different looks, same DinoSpace. Pick whichever feels right — you can switch any time.",
                FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), LineHeight = 1.4, TextColor = Theme.TextSecondary,
                Margin = new Thickness(0, 0, 0, 6)
            });

            stack.Add(LayoutCard("native", "Native",
                "Clean and grown-up — elegant serif headlines and a simple bar. The classic look.",
                NativePreview()));
            stack.Add(LayoutCard("playful", "Playful",
                "Big, rounded and colourful — made for young explorers. Chunky buttons, bubbly tabs, friendly type.",
                PlayfulPreview()));

            var body = Nav.DetailScaffold("", stack, Theme.Accent, out _);
            Content = Ui.PageRoot(body);
        }

        // A tiny mock of a screen in each layout, drawn with real shapes so the
        // difference is obvious at a glance.
        private View NativePreview()
        {
            var col = new VerticalStackLayout { Spacing = 6, Padding = new Thickness(12) };
            col.Add(new Label { Text = "DinoSpace", FontFamily = "Serif", FontSize = 20, TextColor = Theme.Accent });
            col.Add(new BoxView { HeightRequest = 1.5, Color = Theme.Hairline, WidthRequest = 40, HorizontalOptions = LayoutOptions.Start });
            col.Add(MiniCard(6));
            col.Add(new Label { Text = "DINOSAURS", FontFamily = "Nunito", FontSize = 10, FontAttributes = FontAttributes.Bold, CharacterSpacing = 2, TextColor = Theme.TextSecondary, Margin = new Thickness(0, 4, 0, 0) });
            col.Add(MiniBar());
            return PreviewFrame(col);
        }

        private View PlayfulPreview()
        {
            var col = new VerticalStackLayout { Spacing = 7, Padding = new Thickness(12) };
            col.Add(new Label { Text = "Hi there! 👋", FontFamily = "Baloo", FontSize = 20, TextColor = Theme.TextPrimary });
            col.Add(MiniCard(20));
            var head = new VerticalStackLayout { Spacing = 3 };
            head.Add(new Label { Text = "Dinosaurs", FontFamily = "Baloo", FontSize = 16, TextColor = Theme.TextPrimary });
            head.Add(new Border { WidthRequest = 34, HeightRequest = 5, BackgroundColor = Theme.Accent, Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 3 }, HorizontalOptions = LayoutOptions.Start });
            col.Add(head);
            col.Add(MiniBar(bubble: true));
            return PreviewFrame(col);
        }

        private View PreviewFrame(View content) => new Border
        {
            Content = content,
            BackgroundColor = Theme.Bg,
            Stroke = Theme.Hairline, StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            WidthRequest = 150, HeightRequest = 150
        };

        private View MiniCard(double radius) => new Border
        {
            HeightRequest = 46, BackgroundColor = Theme.Surface,
            Stroke = Theme.CardStroke, StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = radius },
            Content = new Border
            {
                WidthRequest = 40, HeightRequest = 40, Margin = new Thickness(3),
                BackgroundColor = Ui.MultiplyAlpha(Theme.Accent, 0.25f), Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = radius - 3 < 3 ? 3 : radius - 3 },
                HorizontalOptions = LayoutOptions.Start
            }
        };

        private View MiniBar(bool bubble = false)
        {
            var bar = new Grid { ColumnSpacing = 4, HeightRequest = 28, VerticalOptions = LayoutOptions.End };
            for (int i = 0; i < 4; i++) bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            for (int i = 0; i < 4; i++)
            {
                var dot = new Border
                {
                    WidthRequest = 14, HeightRequest = 14, HorizontalOptions = LayoutOptions.Center,
                    BackgroundColor = i == 0 ? Theme.Accent : Ui.MultiplyAlpha(Theme.TextHint, 0.5f),
                    Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 7 }
                };
                if (bubble && i == 0)
                    bar.Add(new Border { BackgroundColor = Theme.AccentSoft, Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 9 }, Content = dot, Padding = new Thickness(6, 4) }, i, 0);
                else
                    bar.Add(dot, i, 0);
            }
            return bar;
        }

        private View LayoutCard(string id, string name, string blurb, View preview)
        {
            bool current = AppSettings.LayoutId == id;

            var info = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
            info.Add(new Label { Text = name, FontFamily = Ui.Display, FontSize = Ui.S(20), TextColor = Theme.TextPrimary });
            info.Add(new Label { Text = blurb, FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), LineHeight = 1.35, TextColor = Theme.TextSecondary });
            if (current)
            {
                var chip = Ui.Chip("Current", Theme.AccentSoft, Theme.Accent);
                chip.HorizontalOptions = LayoutOptions.Start;
                chip.Margin = new Thickness(0, 4, 0, 0);
                info.Add(chip);
            }

            var grid = new Grid { ColumnSpacing = 14, RowSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.Add(preview, 0, 0);
            grid.Add(info, 1, 0);

            var card = Ui.Card(grid, 18, new Thickness(14, 14));
            Ui.OnTap(card, (_, _) => Apply(id));
            Ui.Describe(card, $"{name} layout{(current ? ", current" : "")}");
            return card;
        }

        // Freeze the screen, rebuild the whole app in the new layout underneath,
        // then come straight back here and dissolve the freeze-frame.
        private async void Apply(string id)
        {
            if (_switching || id == AppSettings.LayoutId) return;
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

            AppSettings.LayoutId = id;
            AppLayout.ApplyCurrent();
            NovaPage.ResetShared();

            RootPage.LastTab = 3;
            RootPage.HoldThemeCoverOnce = true;
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window != null) window.Page = new AppShell();

            Dispatcher.Dispatch(async () =>
            {
                await System.Threading.Tasks.Task.Delay(80);
                await Nav.Push(() => new LayoutsPage(), animated: false);
            });
        }
    }
}
