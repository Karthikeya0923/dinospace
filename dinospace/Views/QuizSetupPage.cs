using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Quiz setup: pick a topic and slide to any length from 5 to 100
    // questions — no more being stuck with 5/10/25 presets.
    public class QuizSetupPage : ContentPage
    {
        private string _mode = "Mixed";
        private int _count = 10;
        private Label _countLabel = null!;
        private HorizontalStackLayout _modeChips = null!;

        public QuizSetupPage()
        {
            Build();
            SwipeBack.Attach(this);
        }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(18, 4, 18, 28) };

            stack.Add(new Label { Text = "quiz time", FontFamily = Ui.Display, FontSize = Ui.S(32), TextColor = Theme.TextPrimary });
            stack.Add(new Label
            {
                Text = "Pick a topic and how many questions you're up for.",
                FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), LineHeight = 1.4, TextColor = Theme.TextSecondary
            });

            stack.Add(Ui.SectionHeader("Topic"));
            _modeChips = new HorizontalStackLayout { Spacing = 8 };
            BuildModeChips();
            stack.Add(_modeChips);

            stack.Add(Ui.SectionHeader("Questions"));
            _countLabel = new Label
            {
                Text = "10",
                FontFamily = Ui.Display, FontSize = Ui.S(52),
                TextColor = Theme.Accent,
                HorizontalOptions = LayoutOptions.Center
            };
            var slider = new Slider
            {
                Minimum = 5, Maximum = 100, Value = _count,
                MinimumTrackColor = Theme.Accent,
                MaximumTrackColor = Theme.SurfaceAlt,
                ThumbColor = Theme.Accent
            };
            slider.ValueChanged += (_, e) =>
            {
                // snap to 5s so the number feels intentional, not jittery
                int snapped = (int)Math.Round(e.NewValue / 5.0) * 5;
                snapped = Math.Clamp(snapped, 5, 100);
                if (snapped != _count)
                {
                    _count = snapped;
                    _countLabel.Text = _count.ToString();
                    AppSettings.Tap();
                }
            };
            var range = new Grid();
            range.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            range.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            range.Add(Ui.Muted("5 — a quick warm-up", 12), 0, 0);
            var top = Ui.Muted("100 — the full gauntlet", 12);
            top.HorizontalOptions = LayoutOptions.End;
            range.Add(top, 1, 0);

            var sliderCard = new VerticalStackLayout { Spacing = 10 };
            sliderCard.Add(_countLabel);
            sliderCard.Add(slider);
            sliderCard.Add(range);
            stack.Add(Ui.Card(sliderCard, 18, new Thickness(18, 16)));

            stack.Add(Ui.PrimaryButton("START QUIZ", async (_, _) =>
                await Nav.Push(() => new QuizPage(_mode, _count))));

            var body = Nav.DetailScaffoldFixed("", new ScrollView { Content = stack });
            Content = Ui.PageRoot(body);
        }

        private void BuildModeChips()
        {
            _modeChips.Children.Clear();
            foreach (var m in new[] { "Dinosaurs", "Space", "Mixed" })
            {
                bool active = _mode == m;
                // "Dinosaurs" stays the internal mode key; the chip reads
                // "prehistoric creatures" like the rest of the app.
                string display = m == "Dinosaurs" ? "Prehistoric creatures" : m;
                var chip = new Border
                {
                    Content = new Label
                    {
                        Text = Ui.T(display), FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), FontAttributes = FontAttributes.Bold,
                        TextColor = active ? Theme.TextOnAccent : Theme.ChipText
                    },
                    BackgroundColor = active ? Theme.Accent : Theme.ChipBg,
                    Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 100 },
                    Padding = new Thickness(16, 9)
                };
                string mode = m;
                Ui.OnTap(chip, (_, _) => { _mode = mode; BuildModeChips(); });
                _modeChips.Add(chip);
            }
        }
    }
}
