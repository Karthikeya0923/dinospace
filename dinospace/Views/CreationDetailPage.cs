using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using dinospace.Models;
using dinospace.Services;

namespace dinospace.Views
{
    // Detail page for one of the user's creations. Looks just like a built-in
    // entry — hero, stats, sections — but with Edit / Battle actions and NO
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

            var stack = new VerticalStackLayout { Spacing = 18, Padding = new Thickness(18, 16, 18, 30) };

            if (c.Kind == CreationKind.Dinosaur)
            {
                var d = c.ToDinosaur();
                string meta = string.Join("  ·  ", new[] { Quote(c.Meaning), c.Diet, c.Era }.Where(s => !string.IsNullOrWhiteSpace(s)));
                stack.Add(DetailUi.TitleBlock(c.Name, c.Pronunciation, meta));
                stack.Add(DinoStatBars(d));
                stack.Add(DetailUi.Section("About", d.AboutText, Accent));
                stack.Add(DetailUi.Section("Key features", d.KeyFeaturesText, Accent));
                stack.Add(DetailUi.Section("Habitat & environment", d.LifeEnvironmentText, Accent));
                stack.Add(DetailUi.Section("Behaviour", d.BehaviourText, Accent));
                stack.Add(DetailUi.FunFacts(d.FunFactsText, Accent));
            }
            else
            {
                var s = c.ToSpaceObject();
                string meta = string.Join("  ·  ", new[] { c.TypeLabel, c.Subtitle }.Where(x => !string.IsNullOrWhiteSpace(x)));
                stack.Add(DetailUi.TitleBlock(c.Name, c.Pronunciation, meta));
                var chips = new[]
                {
                    (s.Stat1Label, s.Stat1Value, Accent), (s.Stat2Label, s.Stat2Value, Accent),
                    (s.Stat3Label, s.Stat3Value, Accent), (s.Stat4Label, s.Stat4Value, Accent),
                }.Where(x => !string.IsNullOrWhiteSpace(x.Item2)).ToList();
                if (chips.Count > 0) stack.Add(DetailUi.StatChipRow(chips));
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

            var scrollContent = new VerticalStackLayout { Spacing = 0 };
            scrollContent.Add(Hero(c));
            scrollContent.Add(stack);

            var scroll = new ScrollView { Content = scrollContent, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            var root = Ui.PageRoot(scroll);
            root.Add(BackBar());
            Content = root;
        }

        private static string Quote(string s) => string.IsNullOrWhiteSpace(s) ? "" : "“" + s + "”";

        private View Hero(UserCreation c)
        {
            var grid = new Grid { HeightRequest = 300, BackgroundColor = Colors.White };
            grid.Add(EntryCards.ArtFallback(c.Name, 56));
            if (!string.IsNullOrEmpty(c.ImagePath) && System.IO.File.Exists(c.ImagePath))
                grid.Add(EntryCards.Drawing(c.ImagePath, c.CanvasColor));
            return grid;
        }

        private View BackBar()
        {
            var back = new Border
            {
                Content = Ui.Icon(Ui.IconBack, 22),
                WidthRequest = 42, HeightRequest = 42,
                BackgroundColor = Color.FromArgb("#8A000000"), Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 21 },
                HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(12, 10)
            };
            Ui.OnTap(back, async (_, _) => { try { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); } catch { } });
            Ui.Describe(back, "Go back");
            return back;
        }

        private View DinoStatBars(Dinosaur d)
        {
            double maxLen = DinoData.All.Max(x => Num(x.Length));
            double maxH = DinoData.All.Max(x => Num(x.Height));
            double maxW = DinoData.All.Max(x => Num(x.Weight));
            double maxS = DinoData.All.Max(x => Num(x.Speed));
            double maxBite = DinoData.All.Max(x => Num(x.BiteForce));

            var col = new VerticalStackLayout { Spacing = 14 };
            col.Add(Ui.SectionHeader("How it measures up"));
            if (Num(d.Length) > 0) col.Add(Ui.StatBar("Length", d.Length, Clamp(Num(d.Length) / maxLen), Accent));
            if (Num(d.Height) > 0) col.Add(Ui.StatBar("Height", d.Height, Clamp(Num(d.Height) / maxH), Accent));
            if (Num(d.Weight) > 0) col.Add(Ui.StatBar("Weight", d.Weight, Clamp(Num(d.Weight) / maxW), Accent));
            if (Num(d.Speed) > 0) col.Add(Ui.StatBar("Top speed", d.Speed, Clamp(Num(d.Speed) / maxS), Accent));
            if (Num(d.BiteForce) > 0 && maxBite > 0) col.Add(Ui.StatBar("Bite force", d.BiteForce, Clamp(Num(d.BiteForce) / maxBite), Accent));
            return col;
        }

        private static double Clamp(double v) => Math.Clamp(v, 0.04, 1.0);

        private static double Num(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            var sb = new StringBuilder(); bool started = false;
            foreach (char ch in s.Replace(",", ""))
            {
                if (char.IsDigit(ch) || (ch == '.' && started)) { sb.Append(ch); started = true; }
                else if (started) break;
            }
            return sb.Length > 0 && double.TryParse(sb.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
        }
    }
}
