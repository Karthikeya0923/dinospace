using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Dino Battle: pick two creatures and reveal a winner from a composite of
    // size, weight, bite force, speed, and danger rating.
    public class BattlePage : ContentPage
    {
        private Dinosaur? _a;
        private Dinosaur? _b;
        private Grid _arena = null!;
        private Border _fightBtn = null!;
        private VerticalStackLayout _resultArea = null!;

        public BattlePage(Dinosaur? preselect)
        {
            _a = preselect;
            Build();
        }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(16, 4, 16, 24) };
            stack.Add(new Label { Text = "Dino Battle", FontFamily = Ui.Display, FontSize = Ui.S(26), TextColor = Theme.TextPrimary });
            stack.Add(new Label { Text = "Choose two creatures and see who would come out on top.", FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), TextColor = Theme.TextSecondary });

            _arena = new Grid { ColumnSpacing = 10, Margin = new Thickness(0, 6) };
            _arena.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            _arena.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _arena.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            stack.Add(_arena);

            var fightLabel = new Label { Text = "⚔  Battle!", FontFamily = Ui.Fonts, FontSize = Ui.S(16), FontAttributes = FontAttributes.Bold, TextColor = Theme.TextOnAccent, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
            _fightBtn = new Border
            {
                Content = fightLabel,
                BackgroundColor = Theme.AccentDino, Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(16, 14)
            };
            Ui.OnTap(_fightBtn, (_, _) => Fight());
            stack.Add(_fightBtn);

            _resultArea = new VerticalStackLayout { Spacing = 12 };
            stack.Add(_resultArea);

            var content = Nav.DetailScaffold("Dino Battle", new ScrollView { Content = stack }, Theme.AccentDino, out _);
            Content = new Grid { BackgroundColor = Theme.Bg, Children = { content } };
            RefreshArena();
            SwipeBack.Attach(this);
        }

        private void RefreshArena()
        {
            _arena.Children.Clear();
            _arena.Add(Slot(_a, true), 0, 0);
            var vs = new Label { Text = "VS", FontFamily = Ui.Display, FontSize = Ui.S(22), TextColor = Theme.AccentDino, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
            _arena.Add(vs, 1, 0);
            _arena.Add(Slot(_b, false), 2, 0);
            _fightBtn.IsEnabled = _a != null && _b != null && _a.Name != _b.Name;
            _fightBtn.Opacity = _fightBtn.IsEnabled ? 1 : 0.5;
        }

        private View Slot(Dinosaur? d, bool isA)
        {
            View inner;
            if (d == null)
            {
                inner = new VerticalStackLayout
                {
                    Spacing = 8, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label { Text = "+", FontFamily = Ui.Display, FontSize = 34, TextColor = Theme.TextHint, HorizontalTextAlignment = TextAlignment.Center },
                        new Label { Text = "Choose", FontFamily = Ui.Fonts, FontSize = 13, TextColor = Theme.TextSecondary, HorizontalTextAlignment = TextAlignment.Center }
                    }
                };
            }
            else
            {
                var img = new Image { Source = d.ImageFile, Aspect = Aspect.AspectFill, HeightRequest = 96 };
                var imgWrap = new Border { Content = img, HeightRequest = 96, BackgroundColor = Theme.ImgPlaceholder, Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 12 } };
                inner = new VerticalStackLayout
                {
                    Spacing = 6,
                    Children =
                    {
                        imgWrap,
                        new Label { Text = d.Name, FontFamily = Ui.Display, FontSize = Ui.S(15), TextColor = Theme.TextPrimary, HorizontalTextAlignment = TextAlignment.Center, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation },
                        new Label { Text = d.ShortDescription, FontFamily = Ui.Fonts, FontSize = Ui.S(11), TextColor = Theme.TextSecondary, HorizontalTextAlignment = TextAlignment.Center, MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation }
                    }
                };
            }

            var card = new Border
            {
                Content = inner,
                BackgroundColor = Theme.Surface, Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(12), HeightRequest = 200
            };
            Ui.OnTap(card, async (_, _) => await Pick(isA));
            return card;
        }

        private async System.Threading.Tasks.Task Pick(bool isA)
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return;
            var names = DinoData.All.Select(x => x.Name).ToArray();
            string choice = await page.DisplayActionSheet("Choose a creature", "Cancel", null, names);
            var picked = DinoData.ByName(choice);
            if (picked == null) return;
            if (isA) _a = picked; else _b = picked;
            _resultArea.Children.Clear();
            RefreshArena();
        }

        private void Fight()
        {
            if (_a == null || _b == null) return;
            AppSettings.LongPress();

            double sa = Power(_a), sb = Power(_b);
            var winner = sa >= sb ? _a : _b;
            var loser = sa >= sb ? _b : _a;

            _resultArea.Children.Clear();
            _resultArea.Add(new Border
            {
                Content = new Label
                {
                    Text = $"🏆  {winner.Name} wins!",
                    FontFamily = Ui.Display, FontSize = Ui.S(20), TextColor = Theme.AccentDino,
                    HorizontalTextAlignment = TextAlignment.Center
                },
                BackgroundColor = Ui.MultiplyAlpha(Theme.AccentDino, 0.14f),
                Stroke = Ui.MultiplyAlpha(Theme.AccentDino, 0.5f), StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(16, 14)
            });

            _resultArea.Add(DetailUi.Card(new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    DetailUi.TitleRow("Tale of the tape", Theme.AccentDino),
                    CompareRow("Length", _a.Length, _b.Length),
                    CompareRow("Weight", _a.Weight, _b.Weight),
                    CompareRow("Top speed", _a.Speed, _b.Speed),
                    CompareRow("Bite force", string.IsNullOrEmpty(_a.BiteForce) ? "—" : _a.BiteForce, string.IsNullOrEmpty(_b.BiteForce) ? "—" : _b.BiteForce),
                    CompareRow("Danger", $"{_a.Strength}/100", $"{_b.Strength}/100"),
                }
            }));

            _resultArea.Add(new Label
            {
                Text = Verdict(winner, loser),
                FontFamily = Ui.Fonts, FontSize = Ui.S(14), LineHeight = 1.45, TextColor = Theme.TextSecondary
            });
        }

        private View CompareRow(string label, string a, string b)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.Add(new Label { Text = a, FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), FontAttributes = FontAttributes.Bold, TextColor = Theme.TextPrimary, HorizontalTextAlignment = TextAlignment.Start }, 0, 0);
            grid.Add(new Label { Text = label, FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextHint, HorizontalTextAlignment = TextAlignment.Center }, 1, 0);
            grid.Add(new Label { Text = b, FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), FontAttributes = FontAttributes.Bold, TextColor = Theme.TextPrimary, HorizontalTextAlignment = TextAlignment.End }, 2, 0);
            return grid;
        }

        // Composite score: danger rating dominates, with size, weight, bite,
        // and speed contributing.
        private static double Power(Dinosaur d)
        {
            double maxLen = DinoData.All.Max(x => Num(x.Length));
            double maxW = DinoData.All.Max(x => Num(x.Weight));
            double maxBite = DinoData.All.Max(x => Num(x.BiteForce));
            double maxSpd = DinoData.All.Max(x => Num(x.Speed));
            double score = d.Strength * 1.0;
            if (maxLen > 0) score += Num(d.Length) / maxLen * 20;
            if (maxW > 0) score += Num(d.Weight) / maxW * 15;
            if (maxBite > 0) score += Num(d.BiteForce) / maxBite * 15;
            if (maxSpd > 0) score += Num(d.Speed) / maxSpd * 8;
            return score;
        }

        private static string Verdict(Dinosaur w, Dinosaur l)
        {
            var sb = new StringBuilder();
            sb.Append($"{w.Name} has the edge here. ");
            if (Num(w.BiteForce) > Num(l.BiteForce) && !string.IsNullOrEmpty(w.BiteForce))
                sb.Append($"Its bite force of {w.BiteForce} is a serious weapon. ");
            else if (Num(w.Weight) > Num(l.Weight))
                sb.Append($"At {w.Weight}, sheer size and power make the difference. ");
            else if (Num(w.Speed) > Num(l.Speed))
                sb.Append($"Its speed of {w.Speed} lets it control the fight. ");
            sb.Append("In real life, though, the outcome would depend on terrain, surprise, and a good deal of luck!");
            return sb.ToString();
        }

        private static double Num(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            var sb = new StringBuilder(); bool started = false;
            foreach (char c in s.Replace(",", ""))
            {
                if (char.IsDigit(c) || (c == '.' && started)) { sb.Append(c); started = true; }
                else if (started) break;
            }
            return sb.Length > 0 && double.TryParse(sb.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
        }
    }
}
