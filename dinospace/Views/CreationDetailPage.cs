using System;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using dinospace.Models;
using dinospace.Services;

namespace dinospace.Views
{
    // One of the user's own creations, laid out EXACTLY like a real entry:
    // centred section header, the name, tags, the drawing floating on the
    // page (transparent — only what they painted shows), plain label/value
    // stats, then the deeper sections. Plus Edit, Battle and Delete — and NO
    // Ask-NovaSaur button (the AI can't know a creature you invented).
    public class CreationDetailPage : ContentPage
    {
        private readonly string _id;
        private static Color Accent => Theme.Accent;

        public CreationDetailPage(string id)
        {
            _id = id;
            Build();
            SwipeBack.Attach(this);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Build();   // reflect edits made on the editor page
        }

        private void Build()
        {
            var c = CreationStore.Get(_id);
            if (c == null)
            {
                Content = Ui.PageRoot(Nav.DetailScaffoldFixed("", Ui.Card(Ui.Muted("This creation was deleted."), 16, new Thickness(16, 16))));
                return;
            }

            var stack = new VerticalStackLayout { Spacing = 18, Padding = new Thickness(20, 4, 20, 30) };

            stack.Add(new Label
            {
                Text = c.Name,
                FontFamily = Ui.Display, FontSize = Ui.S(30), LineHeight = 1.05,
                TextColor = Theme.TextPrimary
            });
            if (!string.IsNullOrWhiteSpace(c.Pronunciation))
                stack.Add(new Label
                {
                    Text = c.Pronunciation, FontFamily = Ui.Fonts, FontSize = Ui.S(13.5),
                    TextColor = Theme.TextSecondary, Margin = new Thickness(0, -12, 0, 0)
                });

            if (c.Kind == CreationKind.Dinosaur)
                stack.Add(DetailUi.TagChips(c.Diet, c.Era));
            else
                stack.Add(DetailUi.TagChips(c.TypeLabel));

            stack.Add(ArtOnThePage(c));

            if (c.Kind == CreationKind.Dinosaur)
            {
                var d = c.ToDinosaur();
                stack.Add(DetailUi.StatRows(new[]
                {
                    ("Length", d.Length),
                    ("Height", d.Height),
                    ("Weight", d.Weight),
                    ("Top speed", d.Speed),
                    ("Bite force", d.BiteForce),
                    ("Diet", d.Diet),
                }));
                stack.Add(DetailUi.Section("About", d.AboutText, Accent));
                stack.Add(DetailUi.Section("Key features", d.KeyFeaturesText, Accent));
                stack.Add(DetailUi.Section("Habitat & environment", d.LifeEnvironmentText, Accent));
                stack.Add(DetailUi.Section("Behaviour", d.BehaviourText, Accent));
                stack.Add(DetailUi.FunFacts(d.FunFactsText, Accent));
            }
            else
            {
                var s = c.ToSpaceObject();
                var rows = new System.Collections.Generic.List<(string, string)> { ("Type", s.TypeLabel) };
                rows.Add((s.Stat1Label, s.Stat1Value));
                rows.Add((s.Stat2Label, s.Stat2Value));
                rows.Add((s.Stat3Label, s.Stat3Value));
                rows.Add((s.Stat4Label, s.Stat4Value));
                stack.Add(DetailUi.StatRows(rows));
                stack.Add(DetailUi.Section("About", s.AboutText, Accent));
                stack.Add(DetailUi.Section("Key features", s.KeyFeaturesText, Accent));
                stack.Add(DetailUi.Section("Orbit & movement", s.OrbitMovementText, Accent));
                stack.Add(DetailUi.Section("Surface & composition", s.SurfaceCompositionText, Accent));
                stack.Add(DetailUi.Section("History", s.HistoryText, Accent));
                stack.Add(DetailUi.Section("What's inside", s.WhatsInsideText, Accent));
                stack.Add(DetailUi.FunFacts(s.FunFactsText, Accent));
            }

            var badge = new Border
            {
                BackgroundColor = Ui.MultiplyAlpha(Accent, 0.14f), Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = new Thickness(14, 10),
                Content = new Label { Text = "You made this creation", FontFamily = Ui.Fonts, FontSize = Ui.S(13), FontAttributes = FontAttributes.Bold, TextColor = Accent, HorizontalTextAlignment = TextAlignment.Center }
            };
            stack.Add(badge);

            // Actions — NO Ask NovaSaur here, on purpose.
            stack.Add(Ui.PrimaryButton("EDIT THIS CREATION", async (_, _) => await Nav.Push(() => new CreationEditorPage(c))));
            if (c.Kind == CreationKind.Dinosaur)
                stack.Add(Ui.GhostButton("Battle this creature", async (_, _) => await Nav.Push(() => new BattlePage(c.ToDinosaur()))));
            stack.Add(DeleteButton(c));

            var header = DetailUi.HeaderBar(c.Kind == CreationKind.Dinosaur ? "dinosaurs" : "space",
                false, OnBack, () => { }, out _, showSave: false);

            var main = new Grid { RowSpacing = 0 };
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            main.Add(header, 0, 0);
            main.Add(new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never }, 0, 1);

            Content = Ui.PageRoot(main);
        }

        // The drawing floats on the page exactly like built-in entry art —
        // transparent PNG, so only what they painted is there.
        private static View ArtOnThePage(UserCreation c)
        {
            if (string.IsNullOrEmpty(c.ImagePath) || !System.IO.File.Exists(c.ImagePath))
                return new Border
                {
                    Content = EntryCards.PlayfulArt(c.Name, 54),
                    HeightRequest = 210, Stroke = Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = 24 }
                };

            var img = new Image
            {
                Source = ImageSource.FromFile(c.ImagePath), Aspect = Aspect.AspectFit,
                HeightRequest = 230, HorizontalOptions = LayoutOptions.Center
            };
            Ui.Describe(img, c.Name);
            var g = new Grid { HeightRequest = 244 };
            g.Add(img);
            return g;
        }

        private View DeleteButton(UserCreation c)
        {
            var label = new Label
            {
                Text = Ui.T("Delete this creation"),
                FontFamily = Ui.Display, FontSize = Ui.S(15),
                TextColor = Theme.Danger,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            var btn = new Border
            {
                Content = label,
                BackgroundColor = Colors.Transparent,
                Stroke = Theme.Danger.WithAlpha(0.55f), StrokeThickness = 1.4,
                StrokeShape = new RoundRectangle { CornerRadius = 100 },
                HeightRequest = 50, Padding = new Thickness(20, 0)
            };
            Ui.OnTap(btn, async (_, _) =>
            {
                bool sure = await DisplayAlertAsync("Delete this creation?",
                    $"{c.Name} will be gone for good — the drawing too. This can't be undone.",
                    "Delete", "Keep it");
                if (!sure) return;
                CreationStore.Delete(c.Id);
                AppSettings.LongPress();
                try { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); } catch { }
            });
            Ui.Describe(btn, "Delete this creation");
            return btn;
        }

        private async void OnBack()
        {
            try { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); } catch { }
        }
    }
}
