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

            // The header carries a delete slot where real entries keep their
            // save star — same spot, same size, Karthik's icon_delete.png.
            var header = HeaderWithDelete(c);

            var main = new Grid { RowSpacing = 0 };
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            main.Add(header, 0, 0);
            main.Add(new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never }, 0, 1);

            Content = Ui.PageRoot(main);
        }

        // The drawing shown on the paper it was drawn on — white unless the
        // background was painted — inside the same rounded band real entry
        // art gets, so nothing from the page shows through behind it.
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
                HorizontalOptions = LayoutOptions.Center
            };
            Ui.Describe(img, c.Name);
            return new Border
            {
                Content = img,
                HeightRequest = 244,
                BackgroundColor = EntryCards.CanvasColor(c.CanvasColor),
                Stroke = Theme.CardStroke, StrokeThickness = 1.4,
                StrokeShape = new RoundRectangle { CornerRadius = 24 }
            };
        }

        // Back arrow left, section name centred, the hand-drawn delete slot
        // right — exactly where real entries keep their save star.
        private View HeaderWithDelete(UserCreation c)
        {
            var back = Ui.Icon(Ui.IconBack, 24);
            var backWrap = new Border
            {
                Content = back, WidthRequest = 44, HeightRequest = 44,
                BackgroundColor = Colors.Transparent, Stroke = Colors.Transparent
            };
            Ui.OnTap(backWrap, (_, _) => OnBack());
            Ui.Describe(backWrap, "Go back");

            var title = new Label
            {
                Text = Ui.T(c.Kind == CreationKind.Dinosaur ? "dinosaurs" : "space"),
                FontFamily = Ui.Display, FontSize = Ui.S(22), TextColor = Theme.TextPrimary,
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            };

            var del = new Border
            {
                Content = Ui.Icon(Ui.IconDelete, 26),
                WidthRequest = 44, HeightRequest = 44,
                BackgroundColor = Colors.Transparent, Stroke = Colors.Transparent
            };
            Ui.OnTap(del, async (_, _) =>
            {
                bool sure = await DisplayAlertAsync("Delete this creation?",
                    $"{c.Name} will be gone for good — the drawing too. This can't be undone.",
                    "Delete", "Keep it");
                if (!sure) return;
                CreationStore.Delete(c.Id);
                AppSettings.LongPress();
                try { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); } catch { }
            });
            Ui.Describe(del, "Delete this creation");

            var grid = new Grid { Padding = new Thickness(8, 10) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
            grid.Add(backWrap, 0, 0);
            grid.Add(title, 1, 0);
            grid.Add(del, 2, 0);
            return grid;
        }

        private async void OnBack()
        {
            try { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); } catch { }
        }
    }
}
