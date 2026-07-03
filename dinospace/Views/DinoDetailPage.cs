using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Rich profile for one prehistoric creature: hero, quick stats, animated
    // stat bars, an "Ask Nova" hook, a battle launcher, deep sections, and
    // related creatures.
    public class DinoDetailPage : ContentPage
    {
        private readonly Dinosaur _d;
        private Label _saveIcon = null!;
        private static readonly Color Accent = Theme.AccentDino;

        public DinoDetailPage(Dinosaur d)
        {
            _d = d;
            StatsStore.RecordView(d.Name);
            Build();
            SwipeBack.Attach(this);
        }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(16, 16, 16, 28) };

            // meaning line
            var meaning = new Label
            {
                Text = $"“{_d.Meaning}” · {_d.Era}",
                FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), TextColor = Theme.TextSecondary
            };
            stack.Add(meaning);

            // stat chips
            stack.Add(DetailUi.StatChipRow(new (string, string, Color)[]
            {
                ("Length", _d.Length, Accent),
                ("Height", _d.Height, Accent),
                ("Weight", _d.Weight, Accent),
                ("Top speed", _d.Speed, Accent),
                ("Bite force", _d.BiteForce, Accent),
            }));

            // stat bars
            stack.Add(StatBars());

            // actions
            stack.Add(DetailUi.AskNovaButton(_d.Name));
            stack.Add(BattleButton());

            // sections
            stack.Add(DetailUi.Section("About", _d.AboutText, Accent));
            stack.Add(DetailUi.Section("Key Features", _d.KeyFeaturesText, Accent));
            stack.Add(DetailUi.Section("Habitat & Environment", _d.LifeEnvironmentText, Accent));
            stack.Add(DetailUi.Section("Behaviour", _d.BehaviourText, Accent));
            stack.Add(DetailUi.FunFacts(_d.FunFactsText, Accent));

            // related (same category)
            var related = DinoData.All.Where(x => x.Name != _d.Name && x.Category == _d.Category).Take(6)
                .Select(x => (x.ImageFile, x.Name, (object)x)).ToList();
            if (related.Count == 0)
                related = DinoData.All.Where(x => x.Name != _d.Name).Take(6).Select(x => (x.ImageFile, x.Name, (object)x)).ToList();
            stack.Add(DetailUi.Related(related, Accent));

            var scrollContent = new VerticalStackLayout { Spacing = 0 };
            var chips = new List<(string, Color)>
            {
                (_d.Diet, Accent),
                (_d.Group, Theme.AccentSpace),
                (_d.Category, Theme.AccentNova),
            };
            scrollContent.Add(DetailUi.Hero(_d.ImageFile, _d.Name, _d.Pronunciation, chips, Accent));
            scrollContent.Add(stack);

            var scroll = new ScrollView { Content = scrollContent };
            var topBar = DetailUi.TopBar(SavedStore.IsDinoSaved(_d.Name), OnBack, OnSave, OnShare, out _saveIcon);
            ((View)topBar).VerticalOptions = LayoutOptions.Start;

            var root = new Grid { BackgroundColor = Theme.Bg };
            root.Add(scroll);
            root.Add(topBar);
            Content = root;
        }

        private View StatBars()
        {
            double maxLen = DinoData.All.Max(x => Num(x.Length));
            double maxH = DinoData.All.Max(x => Num(x.Height));
            double maxW = DinoData.All.Max(x => Num(x.Weight));
            double maxS = DinoData.All.Max(x => Num(x.Speed));

            var col = new VerticalStackLayout { Spacing = 14 };
            col.Add(DetailUi.TitleRow("Stats", Accent));
            if (Num(_d.Length) > 0) col.Add(Ui.StatBar("Length", _d.Length, Num(_d.Length) / maxLen, Accent));
            if (Num(_d.Height) > 0) col.Add(Ui.StatBar("Height", _d.Height, Num(_d.Height) / maxH, Accent));
            if (Num(_d.Weight) > 0) col.Add(Ui.StatBar("Weight", _d.Weight, Num(_d.Weight) / maxW, Accent));
            if (Num(_d.Speed) > 0) col.Add(Ui.StatBar("Top speed", _d.Speed, Num(_d.Speed) / maxS, Accent));
            col.Add(Ui.StatBar("Danger rating", $"{_d.Strength}/100", _d.Strength / 100.0, Theme.Danger));
            return DetailUi.Card(col);
        }

        private View BattleButton()
        {
            var label = new Label
            {
                Text = "⚔  Battle this creature",
                FontFamily = Ui.Fonts, FontSize = Ui.S(15), FontAttributes = FontAttributes.Bold,
                TextColor = Accent, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center
            };
            var btn = new Border
            {
                Content = label,
                BackgroundColor = Ui.MultiplyAlpha(Accent, 0.16f),
                Stroke = Ui.MultiplyAlpha(Accent, 0.5f), StrokeThickness = 1,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(16, 13)
            };
            Ui.OnTap(btn, async (_, _) => await Nav.Push(new BattlePage(_d)));
            return btn;
        }

        private async void OnBack()
        {
            try { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); } catch { }
        }

        private void OnSave()
        {
            bool nowSaved = SavedStore.ToggleDino(_d.Name);
            AppSettings.LongPress();
            _saveIcon.Text = nowSaved ? "★" : "☆";
            _saveIcon.TextColor = nowSaved ? Accent : Theme.TextPrimary;
        }

        private async void OnShare()
        {
            string fact = _d.FunFactsText.Split('\n').FirstOrDefault()?.TrimStart('•', ' ').Trim() ?? _d.ShortDescription;
            await DetailUi.ShareText($"🦖 {_d.Name} — {_d.ShortDescription}\n\n{fact}\n\nDiscover more in DinoSpace!");
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
            return sb.Length > 0 && double.TryParse(sb.ToString(), out var v) ? v : 0;
        }
    }
}
