using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Hosts the battle as a pushed page (from detail pages and the Native
    // layout); the Playful layout embeds the same BattleView as its own tab.
    public class BattlePage : ContentPage
    {
        public BattlePage(Dinosaur? preselect)
        {
            var content = Nav.DetailScaffoldFixed("", new BattleView(preselect));
            Content = Ui.PageRoot(content);
            SwipeBack.Attach(this);
        }
    }

    // Dino Battle: pick two creatures (via a searchable list) and reveal a
    // winner from a composite of size, weight, bite force, and speed. Filled
    // slots are just images; use Reset to choose again.
    public class BattleView : ContentView, ITabView
    {
        private Dinosaur? _a;
        private Dinosaur? _b;
        private Grid _arena = null!;
        private Border _fightBtn = null!;
        private View _resetBtn = null!;
        private VerticalStackLayout _resultArea = null!;
        private bool _includeMine;

        public BattleView(Dinosaur? preselect = null)
        {
            _a = preselect;
            // If you launched the battle from one of your own creations, that
            // side is already picked — so let the other slot pick yours too.
            if (preselect != null && preselect.Group == "Your creation") _includeMine = true;
            Build();
        }

        public void OnSelected() { }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(16, 4, 16, 24) };
            stack.Add(new Label { Text = Ui.T("Dino Battle"), FontFamily = Ui.Display, FontSize = Ui.S(26), TextColor = Theme.TextPrimary });
            stack.Add(new Label { Text = "Choose two creatures and see who would come out on top.", FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), TextColor = Theme.TextSecondary });

            _arena = new Grid { ColumnSpacing = 10, Margin = new Thickness(0, 6) };
            _arena.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            _arena.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _arena.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            stack.Add(_arena);

            // Let players throw their own drawn creatures into the ring.
            if (CreationStore.Dinos().Count > 0)
                stack.Add(MyCreaturesToggle());

            var fightLabel = new Label { Text = "battle!", FontFamily = Ui.Display, FontSize = Ui.S(16), TextColor = Theme.TextOnAccent, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
            _fightBtn = new Border
            {
                Content = fightLabel,
                BackgroundColor = Theme.AccentDino, Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(16, 14)
            };
            Ui.OnTap(_fightBtn, (_, _) => Fight());
            stack.Add(_fightBtn);

            var resetLabel = new Label { Text = "reset picks", FontFamily = Ui.Display, FontSize = Ui.S(14), TextColor = Theme.TextSecondary, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
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

            Content = new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            RefreshArena();
        }

        // A tappable checkbox row: "Include my creatures". When on, the picker
        // adds the dinosaurs you drew yourself alongside the built-in ones.
        private View MyCreaturesToggle()
        {
            var box = new Border
            {
                WidthRequest = 26, HeightRequest = 26,
                BackgroundColor = _includeMine ? Theme.AccentDino : Colors.Transparent,
                Stroke = _includeMine ? Colors.Transparent : Theme.Hairline, StrokeThickness = 1.5,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                VerticalOptions = LayoutOptions.Center,
                Content = _includeMine
                    ? new Label { Text = "✓", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextOnAccent, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }
                    : null
            };

            var label = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };
            label.Add(new Label { Text = "Include my creatures", FontFamily = Ui.Display, FontSize = Ui.S(15.5), TextColor = Theme.TextPrimary });
            label.Add(new Label { Text = "Add the dinosaurs you drew yourself", FontFamily = Ui.Fonts, FontSize = Ui.S(11.5), TextColor = Theme.TextSecondary });

            var row = new Grid { ColumnSpacing = 12 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            row.Add(box, 0, 0);
            row.Add(label, 1, 0);

            var card = Ui.Card(row, 14, new Thickness(12, 10));
            Ui.OnTap(card, (_, _) => { _includeMine = !_includeMine; AppSettings.Tap(); Build(); });
            return card;
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
                // A drawn creature shows whole on its canvas colour; built-in
                // art can safely fill and crop.
                View img = string.IsNullOrEmpty(d!.CreationBg)
                    ? new Image { Source = d.ImageFile, Aspect = Aspect.AspectFill, HeightRequest = 100 }
                    : EntryCards.Drawing(d.ImageFile, d.CreationBg, 100);
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
            // No push animation - the picker should feel instant. Once one
            // fighter is chosen, the other must come from the same arena —
            // land fights land, sea fights sea, flyers fight flyers.
            if (empty)
            {
                string? arena = (isA ? _b : _a)?.Category;
                Ui.OnTap(card, async (_, _) => await Nav.Push(() => new CreaturePickerPage(picked => Set(isA, picked), arena, _includeMine), animated: false));
            }
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

            // Winner banner — a gold sticker star beside the name, exactly the
            // storybook treatment (no emoji).
            var bannerRow = new HorizontalStackLayout
            {
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    Ui.Sticker("st_icon_star.png", 24),
                    new Label
                    {
                        Text = $"{winner.Name} wins!",
                        FontFamily = Ui.Display, FontSize = Ui.S(20), TextColor = Theme.AccentDino,
                        HorizontalTextAlignment = TextAlignment.Center, VerticalOptions = LayoutOptions.Center
                    }
                }
            };
            _resultArea.Add(new Border
            {
                Content = bannerRow,
                BackgroundColor = Ui.MultiplyAlpha(Theme.AccentDino, 0.14f),
                Stroke = Ui.MultiplyAlpha(Theme.AccentDino, 0.5f), StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(16, 14)
            });

            _resultArea.Add(DetailUi.Card(new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    DetailUi.TitleRow("the stats", Theme.AccentDino),
                    CompareRow("Length", _a.Length, _b.Length),
                    CompareRow("Weight", _a.Weight, _b.Weight),
                    CompareRow("Top speed", _a.Speed, _b.Speed),
                    CompareRow("Bite force", string.IsNullOrEmpty(_a.BiteForce) ? "—" : _a.BiteForce, string.IsNullOrEmpty(_b.BiteForce) ? "—" : _b.BiteForce),
                }
            }));

            var loser = winner == _a ? _b : _a;

            // How many of 100 match-ups the winner takes.
            int odds = Odds(winner, loser);
            _resultArea.Add(new Label
            {
                Text = $"{winner.Name} wins {odds} of 100 match-ups",
                FontFamily = Ui.Display, FontSize = Ui.S(18), TextColor = Theme.TextPrimary,
                HorizontalTextAlignment = TextAlignment.Center
            });

            // How the fight actually plays out (the setting line says it all —
            // no separate verdict paragraph repeating the result).
            _resultArea.Add(Ui.Card(new Label
            {
                Text = Scenario(winner, loser),
                FontFamily = Ui.Fonts, FontSize = Ui.S(14), LineHeight = 1.5, TextColor = Theme.TextPrimary
            }, 16, new Thickness(16, 14)));

            // The referee's corner — reserved for the hand-drawn mascot.
            if (Ui.HasImage("mascot_battle"))
                _resultArea.Add(Ui.Mascot("mascot_battle", 110));
        }

        // Win probability out of 100, from the power gap — amplified so a real
        // advantage reads like one, but never a guaranteed 100 (nature loves
        // an upset) and never below 52 (they did win this simulation).
        private static int Odds(Dinosaur w, Dinosaur l)
        {
            double pw = Power(w), pl = Power(l);
            double share = pw + pl > 0 ? pw / (pw + pl) : 0.5;
            return Math.Clamp((int)Math.Round(50 + (share - 0.5) * 220), 52, 97);
        }

        // A short blow-by-blow, seeded per pairing: same fight reads the same,
        // different fights read differently. Built from their real stats.
        private static string Scenario(Dinosaur w, Dinosaur l)
        {
            int seed = 0;
            foreach (char c in w.Name + "#" + l.Name) seed = seed * 31 + c;
            seed = Math.Abs(seed);

            string arena = w.Category switch
            {
                "Sea" => new[] { "open water, nowhere to hide", "a shallow coastal hunting ground", "deep water at dusk" }[seed % 3],
                "Flying" => new[] { "high thermals over the cliffs", "a windswept shoreline", "the air above a river delta" }[seed % 3],
                _ => new[] { "a dusty floodplain", "a fern-choked forest clearing", "the muddy edge of a river" }[seed % 3],
            };

            string wWeapon = WeaponPhrase(w);
            string lMove = Num(l.Speed) > Num(w.Speed) && Num(l.Speed) > 0
                ? $"{l.Name} is quicker and lands the first strike"
                : $"{l.Name} charges first, trying to end it early";

            string[] middles =
            {
                $"{w.Name} takes the hit, turns, and answers with {wWeapon}.",
                $"But {w.Name} was waiting for exactly that, countering with {wWeapon}.",
                $"{w.Name} shrugs it off — then {wWeapon} changes the fight in one move.",
            };
            string[] enders =
            {
                $"One clean connection is all it takes; {l.Name} backs off, beaten.",
                $"After that, {l.Name} wants no part of round two.",
                $"The fight is over in minutes — {w.Name} stands over the field.",
            };

            return $"The setting: {arena}. {lMove}. {middles[seed / 3 % middles.Length]} {enders[seed / 9 % enders.Length]}";
        }

        // The winner's signature weapon, with its real number where we have one.
        private static string WeaponPhrase(Dinosaur d)
        {
            string armour = ArmourWord(d);
            if (Num(d.BiteForce) > 0) return $"a bone-rattling bite of about {d.BiteForce}";
            if (armour.Length > 0) return $"those {armour}";
            if (Num(d.Weight) > 0) return $"the full {d.Weight} of its body";
            return "raw speed and aggression";
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
