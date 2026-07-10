using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The storybook cover, straight from the design sheet: the lowercase
    // "dinospace" wordmark up top with little stars and a doodle planet
    // around it, the mascot in the middle of the page, and two outlined
    // pills — scan sky and ask novasaur.
    public class HomeView : ContentView, ITabView
    {
        public HomeView(Action<int> goTab) => Build();

        public void OnSelected() => StatsStore.UpdateStreak();

        private void Build()
        {
            // Pills sit at the very bottom of the page, right above the tab
            // bar, exactly like the cover sheet — the mascot owns everything
            // between the wordmark and them.
            var grid = new Grid { Padding = new Thickness(28, 6, 28, 18), RowSpacing = 0 };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });          // wordmark
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });          // mascot
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });          // pills

            // ---- wordmark block, high on the page like the cover art ----
            var mast = new Grid { Margin = new Thickness(0, 26, 0, 0) };

            var wordmark = new Label
            {
                Text = "dinospace",
                FontFamily = Ui.Display,
                FontSize = 44,
                TextColor = Theme.TextPrimary,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            Ui.Describe(wordmark, "dinospace");

            // sticker-sheet doodles scattered around the title
            var planet = Ui.Sticker("st_saturn_small.png", 44);
            planet.HorizontalOptions = LayoutOptions.End;
            planet.VerticalOptions = LayoutOptions.Start;
            planet.Margin = new Thickness(0, -14, -4, 0);

            var starL = Ui.Sticker("st_icon_star.png", 22);
            starL.HorizontalOptions = LayoutOptions.Start;
            starL.VerticalOptions = LayoutOptions.End;
            starL.Margin = new Thickness(6, 0, 0, -10);

            var sparkR = Ui.Sticker("st_icon_sparkle.png", 18);
            sparkR.HorizontalOptions = LayoutOptions.Start;
            sparkR.VerticalOptions = LayoutOptions.Start;
            sparkR.Margin = new Thickness(24, -10, 0, 0);

            mast.Add(wordmark);
            mast.Add(planet);
            mast.Add(starL);
            mast.Add(sparkR);
            grid.Add(mast, 0, 0);

            // ---- the mascot's spot ----
            // Reserved for the hand-drawn hero (mascot_home.png). Until the
            // art lands, the starred paper itself carries the middle of the
            // cover — with a few quiet stickers keeping the space alive.
            var heroArea = new Grid { VerticalOptions = LayoutOptions.Center };
            var hero = Ui.Mascot("mascot_home", 290);
            heroArea.Add(hero);
            if (!Ui.HasImage("mascot_home"))
            {
                var moon = Ui.Sticker("st_crescent.png", 64);
                moon.HorizontalOptions = LayoutOptions.End;
                moon.VerticalOptions = LayoutOptions.Start;
                moon.Margin = new Thickness(0, 8, 8, 0);
                var comet = Ui.Sticker("st_comet_star.png", 46);
                comet.HorizontalOptions = LayoutOptions.Start;
                comet.VerticalOptions = LayoutOptions.End;
                comet.Margin = new Thickness(4, 0, 0, 30);
                heroArea.Add(moon);
                heroArea.Add(comet);
            }
            grid.Add(heroArea, 0, 1);

            // ---- the two cover pills ----
            var buttons = new VerticalStackLayout { Spacing = 14, VerticalOptions = LayoutOptions.End };
            buttons.Add(HomePill(Ui.Icon(Ui.IconScanSky, 26), "scan sky",
                async () => await Nav.Push(() => new SkyViewPage())));
            buttons.Add(HomePill(Ui.Mascot("mascot_ask", 26, Ui.IconAsk), "ask novasaur",
                async () => await Nav.Push(() => new NovaPage())));
            grid.Add(buttons, 0, 2);

            Content = grid;
        }

        // An outlined storybook pill: soft sage fill, fine olive stroke, a
        // little sticker beside lowercase Baloo text.
        private View HomePill(View icon, string text, Func<System.Threading.Tasks.Task> onTap)
        {
            var row = new HorizontalStackLayout
            {
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            icon.VerticalOptions = LayoutOptions.Center;
            row.Add(icon);
            row.Add(new Label
            {
                Text = text,
                FontFamily = Ui.Display,
                FontSize = 19,
                TextColor = Theme.TextPrimary,
                VerticalOptions = LayoutOptions.Center
            });

            var pill = new Border
            {
                Content = row,
                BackgroundColor = Theme.AccentSoft,
                Stroke = Theme.TextPrimary.WithAlpha(0.55f),
                StrokeThickness = 1.6,
                StrokeShape = new RoundRectangle { CornerRadius = 100 },
                HeightRequest = 58,
                Padding = new Thickness(20, 0)
            };
            Ui.OnTap(pill, async (_, _) => await onTap());
            Ui.Describe(pill, text);
            return pill;
        }
    }
}
