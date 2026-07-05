using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Dino Battle: pick two creatures (via a searchable list) and reveal a
    // winner from a composite of size, weight, bite force, and speed. Filled
    // slots are just images; use Reset to choose again.
    public class BattlePage : ContentPage
    {
        private Dinosaur? _a;
        private Dinosaur? _b;
        private Grid _arena = null!;
        private Border _fightBtn = null!;
        private View _resetBtn = null!;
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

            var resetLabel = new Label { Text = "↺  Reset picks", FontFamily = Ui.Fonts, FontSize = Ui.S(14), FontAttributes = FontAttributes.Bold, TextColor = Theme.TextSecondary, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
            _resetBtn = new Border
            {
                Content = resetLabel,
                BackgroundColor = Colors.Transparent, Stroke = Theme.Hairline, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(16, 12)
            };
            Ui.OnTap(_resetBtn, (_, _) => Reset());
            stack.Add(_resetBtn);

            _resultArea = new VerticalStackLayout { Spacing = 12 };
            stack.Add(_resultArea);

            var content = Nav.DetailScaffold("", new ScrollView { Content = stack }, Theme.AccentDino, out _);
            Content = Ui.PageRoot(content);
            RefreshArena();
            SwipeBack.Attach(this);
        }

        private void RefreshArena()
        {
            _arena.Children.Clear();
            _arena.Add(Slot(_a, true), 0, 0);
            _arena.Add(new Label { Text = "VS", FontFamily = Ui.Display, FontSize = Ui.S(22), TextColor = Theme.AccentDino, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center }, 1, 0);
            _arena.Add(Slot(_b, false), 2, 0);

            bool ready = _a != null && _b != null && _a.Name != _b.Name;
            _fightBtn.IsEnabled = ready;
            _fightBtn.Opacity = ready ? 1 : 0.5;
            _resetBtn.IsVisible = _a != null || _b != null;
        }

        private View Slot(Dinosaur? d, bool isA)
        {
            View inner;
            bool empty = d == null;
            if (empty)
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
                var img = new Image { Source = d!.ImageFile, Aspect = Aspect.AspectFill, HeightRequest = 100 };
                var imgWrap = new Border { Content = img, HeightRequest = 100, BackgroundColor = Colors.Transparent, Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 12 } };
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
                StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(12), HeightRequest = 210
            };
            // Only empty slots are tappable; filled slots are just images.
            // No push animation - the picker should feel instant.
            if (empty) Ui.OnTap(card, async (_, _) => await Nav.Push(() => new CreaturePickerPage(picked => Set(isA, picked)), animated: false));
            return card;
        }

        private void Set(bool isA, Dinosaur picked)
        {
            if (isA) _a = picked; else _b = picked;
            _resultArea.Children.Clear();
            RefreshArena();
        }

        private void Reset()
        {
            _a = null; _b = null;
            _resultArea.Children.Clear();
            RefreshArena();
        }

        private void Fight()
        {
            if (_a == null || _b == null) return;
            AppSettings.LongPress();

            var winner = Power(_a) >= Power(_b) ? _a : _b;

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
                }
            }));

            _resultArea.Add(new Label
            {
                Text = Verdict(winner, winner == _a ? _b : _a),
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

        // Composite from measurable stats only (no subjective danger score).
        private static double Power(Dinosaur d)
        {
            double maxLen = DinoData.All.Max(x => Num(x.Length));
            double maxW = DinoData.All.Max(x => Num(x.Weight));
            double maxBite = DinoData.All.Max(x => Num(x.BiteForce));
            double maxSpd = DinoData.All.Max(x => Num(x.Speed));
            double score = 0;
            if (maxLen > 0) score += Num(d.Length) / maxLen * 30;
            if (maxW > 0) score += Num(d.Weight) / maxW * 30;
            if (maxBite > 0) score += Num(d.BiteForce) / maxBite * 30;
            if (maxSpd > 0) score += Num(d.Speed) / maxSpd * 10;
            return score;
        }

        // Writes an actual argument for this specific matchup: how close it is,
        // which stats decide it (with the numbers), and what the underdog's
        // best shot would be. Same pairing always reads the same, but every
        // pairing reads differently.
        private static string Verdict(Dinosaur w, Dinosaur l)
        {
            // stable "randomness" per matchup, so re-running the fight doesn't reshuffle the text
            int seed = 0;
            foreach (char c in w.Name + "|" + l.Name) seed = seed * 31 + c;
            seed = Math.Abs(seed);

            double pw = Power(w), pl = Power(l);
            bool blowout = pl <= 0 || pw / Math.Max(pl, 0.01) > 1.6;
            bool close = !blowout && pw / Math.Max(pl, 0.01) < 1.2;

            var sb = new StringBuilder();
            string[] openers = close
                ? new[]
                {
                    $"This one could go either way, but {w.Name} takes it by a claw.",
                    $"An incredibly even matchup — {w.Name} just barely comes out on top.",
                    $"Almost a coin flip. {w.Name} wins it on the fine details.",
                }
                : blowout
                ? new[]
                {
                    $"Not much of a contest — {w.Name} dominates this one.",
                    $"{w.Name} wins this convincingly.",
                    $"On paper this is one-sided: {w.Name} all the way.",
                }
                : new[]
                {
                    $"{w.Name} has the edge here.",
                    $"Most rounds of this fight go to {w.Name}.",
                    $"{w.Name} is the favourite in this matchup.",
                };
            sb.Append(openers[seed % openers.Length]).Append(' ');

            // gather the real advantages, biggest weapons first
            var reasons = new List<string>();
            double wb = Num(w.BiteForce), lb = Num(l.BiteForce);
            double ww = Num(w.Weight), lw = Num(l.Weight);
            double ws = Num(w.Speed), ls = Num(l.Speed);
            double wl = Num(w.Length), ll = Num(l.Length);

            if (wb > 0 && wb > lb * 1.2)
                reasons.Add(lb > 0
                    ? $"its {w.BiteForce} bite hits far harder than {l.Name}'s {l.BiteForce}"
                    : $"its {w.BiteForce} bite is a weapon {l.Name} simply doesn't have");
            if (ww > 0 && lw > 0 && ww > lw * 1.5)
                reasons.Add(ww / lw >= 3
                    ? $"at {w.Weight} it's roughly {Math.Round(ww / lw)} times heavier"
                    : $"it clearly outweighs {l.Name} — {w.Weight} against {l.Weight}");
            if (ws > 0 && ws > ls * 1.25)
                reasons.Add($"it's quicker too ({w.Speed} vs {l.Speed}), so it picks when the fight happens");
            if (wl > 0 && ll > 0 && wl > ll * 1.3)
                reasons.Add($"its {w.Length} frame gives it a big reach advantage");

            if (reasons.Count > 0)
            {
                sb.Append(char.ToUpper(reasons[0][0])).Append(reasons[0][1..]);
                if (reasons.Count > 1) sb.Append(", and ").Append(reasons[1]);
                sb.Append(". ");
            }

            // give the underdog its due — makes the verdict feel fair, not scripted
            string armour = ArmourWord(l);
            if (armour.Length > 0)
                sb.Append($"{l.Name} isn't helpless though: one good hit from those {armour} could change everything. ");
            else if (ls > ws && ls > 0)
                sb.Append($"{l.Name}'s best hope is its speed — staying out of reach and waiting for a mistake. ");
            else if (lb > wb && lb > 0)
                sb.Append($"If {l.Name} lands its {l.BiteForce} bite first, this ends very differently. ");

            string[] closers =
            {
                "In a real Cretaceous showdown, terrain and surprise would matter as much as size.",
                "Of course, real animals avoid fair fights — the smart ones walk away.",
                "That's the paper verdict; nature loved an upset.",
                "Luck, terrain, and who strikes first could still flip it.",
            };
            sb.Append(closers[seed / 7 % closers.Length]);
            return sb.ToString();
        }

        // What the underdog fights back with, if its entry mentions any classic
        // defence. Keeps verdicts honest for armoured herbivores.
        private static string ArmourWord(Dinosaur d)
        {
            string f = (d.KeyFeaturesText + " " + d.AboutText).ToLowerInvariant();
            if (f.Contains("club")) return "tail clubs";
            if (f.Contains("horn")) return "horns";
            if (f.Contains("spike")) return "spikes";
            if (f.Contains("armour") || f.Contains("armor")) return "armoured plates";
            if (f.Contains("claw") && d.Diet.Contains("Herb", StringComparison.OrdinalIgnoreCase)) return "giant claws";
            return "";
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
