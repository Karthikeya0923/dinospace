using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Editorial profile for one space object.
    public class SpaceDetailPage : ContentPage
    {
        private readonly SpaceObject _s;
        private Microsoft.Maui.Controls.Shapes.Path _saveIcon = null!;
        private Color Accent => AppLayout.Playful ? PlayfulKit.HueFor(_s.Name) : Theme.Accent;

        public SpaceDetailPage(SpaceObject s)
        {
            _s = s;
            StatsStore.RecordView(s.Name);
            Build();
            SwipeBack.Attach(this);
        }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 18, Padding = new Thickness(18, 16, 18, 30) };

            stack.Add(DetailUi.TitleBlock(_s.Name, _s.Pronunciation,
                $"{_s.Subtitle}  ·  {_s.TypeLabel}"));

            stack.Add(DetailUi.StatChipRow(new (string, string, Color)[]
            {
                (_s.Stat1Label, _s.Stat1Value, Accent),
                (_s.Stat2Label, _s.Stat2Value, Accent),
                (_s.Stat3Label, _s.Stat3Value, Accent),
                (_s.Stat4Label, _s.Stat4Value, Accent),
            }));

            stack.Add(DetailUi.Section("About", _s.AboutText, Accent));
            stack.Add(DetailUi.Section("Key features", _s.KeyFeaturesText, Accent));
            stack.Add(DetailUi.Section("Orbit & movement", _s.OrbitMovementText, Accent));
            stack.Add(DetailUi.Section("Surface & composition", _s.SurfaceCompositionText, Accent));
            stack.Add(DetailUi.Section("History", _s.HistoryText, Accent));
            stack.Add(DetailUi.Section("What's inside", _s.WhatsInsideText, Accent));
            stack.Add(DetailUi.FunFacts(_s.FunFactsText, Accent));

            var related = SpaceData.All.Where(x => x.Name != _s.Name && x.Category == _s.Category).Take(6)
                .Select(x => (x.ImageFile, x.Name, (object)x)).ToList();
            if (related.Count == 0)
                related = SpaceData.All.Where(x => x.Name != _s.Name).Take(6).Select(x => (x.ImageFile, x.Name, (object)x)).ToList();
            stack.Add(DetailUi.Related(related, Accent));

            stack.Add(DetailUi.AskNovaButton(_s.Name));

            var scrollContent = new VerticalStackLayout { Spacing = 0 };
            scrollContent.Add(DetailUi.Hero(_s.ImageFile, _s.Name));
            scrollContent.Add(stack);

            var scroll = new ScrollView { Content = scrollContent, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            var topBar = DetailUi.TopBar(SavedStore.IsSpaceSaved(_s.Name), OnBack, OnSave, out _saveIcon);
            ((View)topBar).VerticalOptions = LayoutOptions.Start;

            var root = Ui.PageRoot(scroll);
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
            Ui.SetIcon(_saveIcon, nowSaved ? Ui.IconSavedFill : Ui.IconSaved, nowSaved ? Theme.Accent : Microsoft.Maui.Graphics.Colors.White);
        }
    }
}
