using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Editorial profile for one prehistoric creature: clean hero, serif
    // headline, quick stats, stat bars, deep sections, related, and the
    // Nova/battle actions at the end.
    public class DinoDetailPage : ContentPage
    {
        private readonly Dinosaur _d;
        private Label _saveIcon = null!;
        private static Color Accent => Theme.Accent;

        public DinoDetailPage(Dinosaur d)
        {
            _d = d;
            StatsStore.RecordView(d.Name);
            Build();
            SwipeBack.Attach(this);
        }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 18, Padding = new Thickness(18, 16, 18, 30) };

            stack.Add(DetailUi.TitleBlock(_d.Name, _d.Pronunciation,
                $"“{_d.Meaning}”  ·  {_d.Diet}  ·  {_d.Era}"));

            stack.Add(StatBars());

            stack.Add(DetailUi.Section("About", _d.AboutText, Accent));
            stack.Add(DetailUi.Section("Key features", _d.KeyFeaturesText, Accent));
            stack.Add(DetailUi.Section("Habitat & environment", _d.LifeEnvironmentText, Accent));
            stack.Add(DetailUi.Section("Behaviour", _d.BehaviourText, Accent));
            stack.Add(DetailUi.FunFacts(_d.FunFactsText, Accent));

            var related = DinoData.All.Where(x => x.Name != _d.Name && x.Category == _d.Category).Take(6)
                .Select(x => (x.ImageFile, x.Name, (object)x)).ToList();
            if (related.Count == 0)
                related = DinoData.All.Where(x => x.Name != _d.Name).Take(6).Select(x => (x.ImageFile, x.Name, (object)x)).ToList();
            stack.Add(DetailUi.Related(related, Accent));

            stack.Add(DetailUi.AskNovaButton(_d.Name));
            stack.Add(Ui.GhostButton("Battle this creature", async (_, _) => await Nav.Push(new BattlePage(_d))));

            var scrollContent = new VerticalStackLayout { Spacing = 0 };
            scrollContent.Add(DetailUi.Hero(_d.ImageFile, _d.Name));
            scrollContent.Add(stack);

            var scroll = new ScrollView { Content = scrollContent, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            var topBar = DetailUi.TopBar(SavedStore.IsDinoSaved(_d.Name), OnBack, OnSave, out _saveIcon);
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
            double maxBite = DinoData.All.Max(x => Num(x.BiteForce));

            var col = new VerticalStackLayout { Spacing = 14 };
            col.Add(Ui.SectionHeader("How it measures up"));
            if (Num(_d.Length) > 0) col.Add(Ui.StatBar("Length", _d.Length, Num(_d.Length) / maxLen, Accent));
            if (Num(_d.Height) > 0) col.Add(Ui.StatBar("Height", _d.Height, Num(_d.Height) / maxH, Accent));
            if (Num(_d.Weight) > 0) col.Add(Ui.StatBar("Weight", _d.Weight, Num(_d.Weight) / maxW, Accent));
            if (Num(_d.Speed) > 0) col.Add(Ui.StatBar("Top speed", _d.Speed, Num(_d.Speed) / maxS, Accent));
            if (Num(_d.BiteForce) > 0 && maxBite > 0) col.Add(Ui.StatBar("Bite force", _d.BiteForce, Num(_d.BiteForce) / maxBite, Accent));
            return col;
        }

        private async void OnBack()
        {
            try { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); } catch { }
        }

        private void OnSave()
        {
            bool nowSaved = SavedStore.ToggleDino(_d.Name);
            AppSettings.LongPress();
            _saveIcon.Text = nowSaved ? Ui.IconSavedFill : Ui.IconSaved;
            _saveIcon.TextColor = nowSaved ? Theme.Accent : Microsoft.Maui.Graphics.Colors.White;
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
