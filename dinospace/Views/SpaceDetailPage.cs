using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // One space object, laid out exactly like the design sheet: centred
    // "space" header with a save star, the name, a type tag, the art on the
    // page, plain label/value stats, then About and the deeper sections.
    public class SpaceDetailPage : ContentPage
    {
        private readonly SpaceObject _s;
        private Ui.IconToggle _saveIcon = null!;
        private Color Accent => Theme.Accent;

        public SpaceDetailPage(SpaceObject s)
        {
            _s = s;
            StatsStore.RecordView(s.Name);
            Build();
            SwipeBack.Attach(this);
        }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 18, Padding = new Thickness(20, 4, 20, 30) };

            stack.Add(new Label
            {
                Text = _s.Name,
                FontFamily = Ui.Display, FontSize = Ui.S(30), LineHeight = 1.05,
                TextColor = Theme.TextPrimary
            });
            if (!string.IsNullOrWhiteSpace(_s.Pronunciation))
                stack.Add(new Label
                {
                    Text = _s.Pronunciation, FontFamily = Ui.Fonts, FontSize = Ui.S(13.5),
                    TextColor = Theme.TextSecondary, Margin = new Thickness(0, -12, 0, 0)
                });

            stack.Add(DetailUi.TagChips(_s.TypeLabel));

            stack.Add(DetailUi.EntryImage(_s.ImageFile, _s.Name));

            var rows = new List<(string, string)> { ("Type", _s.TypeLabel) };
            rows.Add((_s.Stat1Label, _s.Stat1Value));
            rows.Add((_s.Stat2Label, _s.Stat2Value));
            rows.Add((_s.Stat3Label, _s.Stat3Value));
            rows.Add((_s.Stat4Label, _s.Stat4Value));
            stack.Add(DetailUi.StatRows(rows));

            stack.Add(DetailUi.Section("About", _s.AboutText, Accent));
            stack.Add(DetailUi.Section("Key features", _s.KeyFeaturesText, Accent));
            stack.Add(DetailUi.Section("Orbit & movement", _s.OrbitMovementText, Accent));
            stack.Add(DetailUi.Section("Surface & composition", _s.SurfaceCompositionText, Accent));
            stack.Add(DetailUi.Section("History", _s.HistoryText, Accent));
            stack.Add(DetailUi.Section("What's inside", _s.WhatsInsideText, Accent));
            stack.Add(DetailUi.FunFacts(_s.FunFactsText, Accent));

            // "You might also like": siblings from the same category (moons
            // next to moons, stars next to stars); any six others if the
            // category has no one else in it.
            var related = SpaceData.All.Where(x => x.Name != _s.Name && x.Category == _s.Category).Take(6)
                .Select(x => (x.ImageFile, x.Name, (object)x)).ToList();
            if (related.Count == 0)
                related = SpaceData.All.Where(x => x.Name != _s.Name).Take(6).Select(x => (x.ImageFile, x.Name, (object)x)).ToList();
            stack.Add(DetailUi.Related(related, Accent));

            stack.Add(DetailUi.AskNovaButton(_s.Name));

            var header = DetailUi.HeaderBar("space",
                SavedStore.IsSpaceSaved(_s.Name), OnBack, OnSave, out _saveIcon);

            var main = new Grid { RowSpacing = 0 };
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            main.Add(header, 0, 0);
            main.Add(new ScrollView { Content = Ui.CapWidth(stack), VerticalScrollBarVisibility = ScrollBarVisibility.Never }, 0, 1);

            Content = Ui.PageRoot(main);
        }

        private async void OnBack()
        {
            try { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); } catch { }
        }

        // Star tap: flip the bookmark, buzz, and swap the outline/filled
        // star in place — no page reload needed.
        private void OnSave()
        {
            bool nowSaved = SavedStore.ToggleSpace(_s.Name);
            AppSettings.LongPress();
            _saveIcon.Show(nowSaved);
        }
    }
}
