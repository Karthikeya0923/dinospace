using System;
using System.Linq;
using System.Text;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // One prehistoric creature, laid out exactly like the design sheet:
    // centred section header with a save star, the name, two coloured tags,
    // the art floating on the page with grass at its feet, plain label/value
    // stats, then About and the deeper sections.
    public class DinoDetailPage : ContentPage
    {
        private readonly Dinosaur _d;
        private Ui.IconToggle _saveIcon = null!;
        private Color Accent => Theme.Accent;

        public DinoDetailPage(Dinosaur d)
        {
            _d = d;
            StatsStore.RecordView(d.Name);
            Build();
            SwipeBack.Attach(this);
        }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 18, Padding = new Thickness(20, 4, 20, 30) };

            stack.Add(new Label
            {
                Text = _d.Name,
                FontFamily = Ui.Display, FontSize = Ui.S(30), LineHeight = 1.05,
                TextColor = Theme.TextPrimary
            });
            if (!string.IsNullOrWhiteSpace(_d.Pronunciation))
                stack.Add(new Label
                {
                    Text = _d.Pronunciation, FontFamily = Ui.Fonts, FontSize = Ui.S(13.5),
                    TextColor = Theme.TextSecondary, Margin = new Thickness(0, -12, 0, 0)
                });

            stack.Add(DetailUi.TagChips(_d.Diet, _d.Era));

            stack.Add(DetailUi.EntryImage(_d.ImageFile, _d.Name));

            stack.Add(DetailUi.StatRows(new[]
            {
                ("Length", _d.Length),
                ("Height", _d.Height),
                ("Weight", _d.Weight),
                ("Top speed", _d.Speed),
                ("Bite force", _d.BiteForce),
                ("Diet", _d.Diet),
                ("Habitat", _d.Category),
            }));

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
            stack.Add(Ui.GhostButton("Battle this creature", async (_, _) => await Nav.Push(() => new BattlePage(_d))));

            var header = DetailUi.HeaderBar("dinosaurs",
                SavedStore.IsDinoSaved(_d.Name), OnBack, OnSave, out _saveIcon);

            var main = new Grid { RowSpacing = 0 };
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            main.Add(header, 0, 0);
            main.Add(new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never }, 0, 1);

            Content = Ui.PageRoot(main);
        }

        private async void OnBack()
        {
            try { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); } catch { }
        }

        private void OnSave()
        {
            bool nowSaved = SavedStore.ToggleDino(_d.Name);
            AppSettings.LongPress();
            _saveIcon.Show(nowSaved);
        }
    }
}
