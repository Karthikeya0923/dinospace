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
        private bool _built;
        private bool _short;

        public HomeView(Action<int> goTab)
        {
            Build(false);
            // The cover is built for a tall page; turned sideways it needs a
            // different arrangement entirely, so it is rebuilt whenever the
            // window crosses the short/tall line.
            SizeChanged += (_, _) =>
            {
                bool now = Ui.IsShort(Height);
                if (!_built || now != _short) Build(now);
            };
        }

        public void OnSelected() => StatsStore.UpdateStreak();

        private void Build(bool shortScreen)
        {
            _built = true;
            _short = shortScreen;
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
                FontSize = Ui.S(shortScreen ? 34 : 44),
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
            if (shortScreen) mast.Margin = new Thickness(0, 0, 0, 10);

            // ---- the hero's spot ----
            // The cover art (mascot_home.png) — the app icon's ringed-planet
            // painting, cropped tight so it fills this slot — sits dead
            // centre between the wordmark and the pills. If the art ever
            // goes missing, a few quiet stickers keep the space alive.
            var heroArea = new Grid { VerticalOptions = LayoutOptions.Center };
            var hero = Ui.Mascot("mascot_home", shortScreen ? 210 : 330);
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

            // ---- the two cover pills ----
            var buttons = new VerticalStackLayout { Spacing = 14, VerticalOptions = LayoutOptions.End };
            buttons.Add(HomePill(Ui.Icon(Ui.IconScanSky, 26), "scan sky",
                async () => await Nav.Push(() => new SkyPage())));
            buttons.Add(HomePill(Ui.Mascot("mascot_nova", 26, Ui.IconAsk), "ask nova",
                async () => { if (await ParentMode.GateNova()) await Nav.Push(() => new NovaPage()); }));

            if (shortScreen)
            {
                // Sideways there is no room to stack a wordmark, a tall hero
                // and two pills — the art ended up swallowing the title and
                // running under the buttons. Landscape has width instead, so
                // the cover splits in two: the planet on one side, the
                // wordmark and pills on the other, both fully visible.
                buttons.VerticalOptions = LayoutOptions.Center;

                var side = new VerticalStackLayout
                {
                    Spacing = 10,
                    VerticalOptions = LayoutOptions.Center,
                    Children = { mast, buttons }
                };

                var land = new Grid { Padding = new Thickness(24, 4, 24, 10), ColumnSpacing = 22 };
                land.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                land.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                land.Add(heroArea, 0, 0);
                land.Add(side, 1, 0);
                Content = land;
                return;
            }

            grid.Add(mast, 0, 0);
            grid.Add(heroArea, 0, 1);
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
                FontSize = Ui.S(19),
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
