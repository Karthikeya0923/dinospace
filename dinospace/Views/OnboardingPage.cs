using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // First-launch intro: three quick slides, then into the app.
    public class OnboardingPage : ContentPage
    {
        private record Slide(string Glyph, Color Accent, string Title, string Body);

        private readonly List<Slide> _slides = new()
        {
            new("🦖", Theme.AccentDino, "Two worlds, one app",
                "Explore a rich encyclopedia of dinosaurs and space — from the mighty T. Rex to distant black holes — all in one place."),
            new("✦", Theme.AccentNova, "Meet NovaSaur",
                "Ask our on-device AI anything about dinosaurs or space. It's grounded in real facts, safe for kids, and works completely offline."),
            new("🏆", Theme.AccentSpace, "Play, battle & collect",
                "Take quizzes, stage epic dino battles, climb curated collections, and earn XP as you learn. Ready to explore?"),
        };

        private CarouselView _carousel = null!;
        private IndicatorView _indicator = null!;
        private Label _nextLabel = null!;

        public OnboardingPage()
        {
            BackgroundColor = Theme.Bg;
            Build();
        }

        private void Build()
        {
            _carousel = new CarouselView
            {
                ItemsSource = _slides,
                Loop = false,
                ItemTemplate = new DataTemplate(SlideTemplate),
                IndicatorView = null
            };
            _carousel.PositionChanged += (_, _) => SyncButton();

            _indicator = new IndicatorView
            {
                IndicatorColor = Theme.SurfaceAlt,
                SelectedIndicatorColor = Theme.AccentNova,
                IndicatorSize = 9,
                HorizontalOptions = LayoutOptions.Center
            };
            _carousel.IndicatorView = _indicator;

            var skip = new Label { Text = "Skip", FontFamily = Ui.Fonts, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextSecondary, HorizontalOptions = LayoutOptions.End, Margin = new Thickness(0, 8, 4, 0) };
            Ui.OnTap(skip, (_, _) => Finish());

            _nextLabel = new Label { Text = "Next", FontFamily = Ui.Fonts, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextOnAccent, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
            var nextBtn = new Border
            {
                Content = _nextLabel,
                BackgroundColor = Theme.AccentNova, Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(20, 15)
            };
            Ui.OnTap(nextBtn, (_, _) => OnNext());

            var grid = new Grid { Padding = new Thickness(20, 16, 20, 28), RowSpacing = 16 };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.Add(skip, 0, 0);
            grid.Add(_carousel, 0, 1);
            grid.Add(_indicator, 0, 2);
            grid.Add(nextBtn, 0, 3);
            Content = grid;
        }

        private View SlideTemplate()
        {
            var glyph = new Label { FontSize = 72, HorizontalTextAlignment = TextAlignment.Center };
            glyph.SetBinding(Label.TextProperty, new Binding(nameof(Slide.Glyph)));

            var ring = new Border
            {
                WidthRequest = 140, HeightRequest = 140,
                Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 70 },
                HorizontalOptions = LayoutOptions.Center, Content = glyph
            };
            ring.SetBinding(Border.BackgroundColorProperty, new Binding(nameof(Slide.Accent), converter: new AlphaConverter()));

            var title = new Label { FontFamily = Ui.Display, FontSize = 27, TextColor = Theme.TextPrimary, HorizontalTextAlignment = TextAlignment.Center };
            title.SetBinding(Label.TextProperty, new Binding(nameof(Slide.Title)));

            var body = new Label { FontFamily = Ui.Fonts, FontSize = 15.5, LineHeight = 1.5, TextColor = Theme.TextSecondary, HorizontalTextAlignment = TextAlignment.Center };
            body.SetBinding(Label.TextProperty, new Binding(nameof(Slide.Body)));

            return new VerticalStackLayout
            {
                Spacing = 22, Padding = new Thickness(16), VerticalOptions = LayoutOptions.Center,
                Children = { ring, title, body }
            };
        }

        private void OnNext()
        {
            AppSettings.Tap();
            int pos = _carousel.Position;
            if (pos >= _slides.Count - 1) { Finish(); return; }
            _carousel.Position = pos + 1;
        }

        private void SyncButton()
            => _nextLabel.Text = _carousel.Position >= _slides.Count - 1 ? "Get started" : "Next";

        private void Finish()
        {
            AppSettings.Onboarded = true;
            if (Application.Current?.Windows.Count > 0)
                Application.Current.Windows[0].Page = new AppShell();
        }

        // Tints an accent to a faint background fill for the glyph ring.
        private class AlphaConverter : IValueConverter
        {
            public object? Convert(object? value, System.Type t, object? p, System.Globalization.CultureInfo c)
                => value is Color col ? Ui.MultiplyAlpha(col, 0.16f) : Colors.Transparent;
            public object? ConvertBack(object? value, System.Type t, object? p, System.Globalization.CultureInfo c) => null;
        }
    }
}
