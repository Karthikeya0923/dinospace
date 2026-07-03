using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Rich profile for one space object: hero, quick stats, an "Ask Nova"
    // hook, deep sections, and related objects from the same category.
    public class SpaceDetailPage : ContentPage
    {
        private readonly SpaceObject _s;
        private Label _saveIcon = null!;
        private static readonly Color Accent = Theme.AccentSpace;

        public SpaceDetailPage(SpaceObject s)
        {
            _s = s;
            StatsStore.RecordView(s.Name);
            Build();
            SwipeBack.Attach(this);
        }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(16, 16, 16, 28) };

            stack.Add(new Label
            {
                Text = _s.Subtitle,
                FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), TextColor = Theme.TextSecondary
            });

            stack.Add(DetailUi.StatChipRow(new (string, string, Color)[]
            {
                (_s.Stat1Label, _s.Stat1Value, Accent),
                (_s.Stat2Label, _s.Stat2Value, Accent),
                (_s.Stat3Label, _s.Stat3Value, Accent),
                (_s.Stat4Label, _s.Stat4Value, Accent),
            }));

            stack.Add(DetailUi.AskNovaButton(_s.Name));

            stack.Add(DetailUi.Section("About", _s.AboutText, Accent));
            stack.Add(DetailUi.Section("Key Features", _s.KeyFeaturesText, Accent));
            stack.Add(DetailUi.Section("Orbit & Movement", _s.OrbitMovementText, Accent));
            stack.Add(DetailUi.Section("Surface & Composition", _s.SurfaceCompositionText, Accent));
            stack.Add(DetailUi.Section("History", _s.HistoryText, Accent));
            stack.Add(DetailUi.Section("What's Inside", _s.WhatsInsideText, Accent));
            stack.Add(DetailUi.FunFacts(_s.FunFactsText, Accent));

            var related = SpaceData.All.Where(x => x.Name != _s.Name && x.Category == _s.Category).Take(6)
                .Select(x => (x.ImageFile, x.Name, (object)x)).ToList();
            if (related.Count == 0)
                related = SpaceData.All.Where(x => x.Name != _s.Name).Take(6).Select(x => (x.ImageFile, x.Name, (object)x)).ToList();
            stack.Add(DetailUi.Related(related, Accent));

            var scrollContent = new VerticalStackLayout { Spacing = 0 };
            var chips = new List<(string, Color)>
            {
                (_s.TypeLabel, Accent),
                (_s.Category, Theme.AccentNova),
            };
            scrollContent.Add(DetailUi.Hero(_s.ImageFile, _s.Name, _s.Pronunciation, chips, Accent));
            scrollContent.Add(stack);

            var scroll = new ScrollView { Content = scrollContent };
            var topBar = DetailUi.TopBar(SavedStore.IsSpaceSaved(_s.Name), OnBack, OnSave, OnShare, out _saveIcon);
            ((View)topBar).VerticalOptions = LayoutOptions.Start;

            var root = new Grid { BackgroundColor = Theme.Bg };
            root.Add(scroll);
            root.Add(topBar);
            Content = root;
        }

        private async void OnBack()
        {
            try { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); } catch { }
        }

        private void OnSave()
        {
            bool nowSaved = SavedStore.ToggleSpace(_s.Name);
            AppSettings.LongPress();
            _saveIcon.Text = nowSaved ? "★" : "☆";
            _saveIcon.TextColor = nowSaved ? Accent : Theme.TextPrimary;
        }

        private async void OnShare()
        {
            string fact = _s.FunFactsText.Split('\n').FirstOrDefault()?.TrimStart('•', ' ').Trim() ?? _s.ShortDescription;
            await DetailUi.ShareText($"🪐 {_s.Name} — {_s.ShortDescription}\n\n{fact}\n\nDiscover more in DinoSpace!");
        }
    }
}
