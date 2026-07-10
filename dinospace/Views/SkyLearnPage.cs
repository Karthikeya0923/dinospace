using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // "Learn the sky" — what everything on Sky Tonight actually means.
    // Written to hit the sweet spot: real science, explained so a kid gets it.
    public class SkyLearnPage : ContentPage
    {
        public SkyLearnPage()
        {
            var stack = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(18, 4, 18, 28) };

            stack.Add(new Label { Text = "Learn the sky", FontFamily = Ui.Display, FontSize = Ui.S(30), TextColor = Theme.TextPrimary });
            stack.Add(new Label
            {
                Text = "What you're actually seeing when you look up — and what everything on Sky Tonight means.",
                FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), LineHeight = 1.4, TextColor = Theme.TextSecondary,
                Margin = new Thickness(0, 0, 0, 4)
            });

            // ----- the moon -----
            stack.Add(Ui.SectionHeader("Why the moon changes shape"));
            stack.Add(Ui.Card(Ui.Body(
                "The moon doesn't actually change — it's always a ball, and the sun always lights up half of it. " +
                "What changes is how much of that sunlit half faces us as the moon circles Earth every 29½ days. " +
                "Each shape we see is called a phase. \"Waxing\" means growing, \"waning\" means shrinking, and " +
                "\"gibbous\" is the bulging shape between half and full.", size: 14), 16, new Thickness(16, 14)));

            stack.Add(PhaseRow(0, "New Moon",
                "The moon sits between Earth and the sun, so its lit side faces away from us. You can't see it at all — which makes this the best night for spotting faint stars."));
            stack.Add(PhaseRow(45, "Waxing Crescent",
                "A thin sliver appears on the right and grows a little each night. Look for it low in the west just after sunset."));
            stack.Add(PhaseRow(90, "First Quarter",
                "The right half is lit — the moon is a quarter of the way through its cycle. It's high in the sky at sunset."));
            stack.Add(PhaseRow(135, "Waxing Gibbous",
                "More than half lit and still growing. Almost there!"));
            stack.Add(PhaseRow(180, "Full Moon",
                "Earth is between the sun and the moon, so we see the whole lit face. It rises as the sun sets and shines all night."));
            stack.Add(PhaseRow(225, "Waning Gibbous",
                "Past full and starting to shrink. It rises later in the evening now."));
            stack.Add(PhaseRow(270, "Last Quarter",
                "The left half is lit — three quarters of the way through. You'll mostly catch it after midnight or in the morning sky."));
            stack.Add(PhaseRow(315, "Waning Crescent",
                "A thin sliver on the left, fading each night until it disappears — and the whole cycle starts again."));

            // ----- constellations -----
            stack.Add(Ui.SectionHeader("What is a constellation?"));
            stack.Add(Ui.Card(Ui.Body(
                "A constellation is a pattern people drew between stars, like a giant connect-the-dots — a hunter, a bear, a scorpion. " +
                "The stars in a pattern aren't really neighbours; some are hundreds of light-years behind the others. " +
                "They only look grouped from where Earth sits. Astronomers use 88 official constellations as a map of the whole sky.", size: 14), 16, new Thickness(16, 14)));

            // ----- why things move -----
            stack.Add(Ui.SectionHeader("Why the sky moves"));
            stack.Add(Ui.Card(Ui.Body(
                "Earth spins once a day, so the whole sky seems to slide from east to west — stars rise in the east and set in the west, " +
                "just like the sun. That's why Sky Tonight gives you directions: a constellation \"in the southeast\" will drift toward the " +
                "south and west as the night goes on. One star barely moves: Polaris, the North Star, because it sits almost straight " +
                "above Earth's north pole. And because Earth also orbits the sun, the evening sky slowly changes through the year — " +
                "winter constellations like Orion trade places with summer ones like Cygnus.", size: 14), 16, new Thickness(16, 14)));

            // ----- planets vs stars -----
            stack.Add(Ui.SectionHeader("Spotting a planet"));
            stack.Add(Ui.Card(Ui.Body(
                "Planets look like bright stars, but there are two giveaways. First, planets shine with a steady light while stars twinkle — " +
                "starlight comes from so far away that our air makes it flicker. Second, planets wander: watch for a few weeks and they slowly " +
                "drift across the star patterns. That's actually what \"planet\" means in Greek — wanderer. Venus and Jupiter are so bright " +
                "you can't miss them; Mars gives itself away with its rusty orange colour.", size: 14), 16, new Thickness(16, 14)));

            // ----- how Sky Tonight works -----
            stack.Add(Ui.SectionHeader("How Sky Tonight knows"));
            stack.Add(Ui.Card(Ui.Body(
                "No magic, just math! The moon, sun, and planets all follow paths we can calculate precisely. DinoSpace does that " +
                "right on your phone — no internet needed — using the same kind of orbital math NASA publishes. Your location " +
                "(if you share it) just tells the app which part of the sky is over your head.", size: 14), 16, new Thickness(16, 14)));

            var body = Nav.DetailScaffold("", stack, Theme.Accent, out _);
            Content = Ui.PageRoot(body);
            SwipeBack.Attach(this);
        }

        // One moon phase: Karthik's drawing of it on a night-sky tile +
        // name + meaning. The slot is the phase name (fullmoon.png, ...).
        private static View PhaseRow(double elongation, string name, string meaning)
        {
            var moon = Ui.Icon(Ui.MoonSlot(name), 44);
            var tile = new Border
            {
                Content = moon,
                WidthRequest = 64, HeightRequest = 64,
                BackgroundColor = Color.FromArgb("#111527"),
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                VerticalOptions = LayoutOptions.Start
            };

            var info = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
            info.Add(new Label { Text = name, FontFamily = Ui.Display, FontSize = Ui.S(18), TextColor = Theme.TextPrimary });
            info.Add(new Label { Text = meaning, FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), LineHeight = 1.35, TextColor = Theme.TextSecondary });

            var grid = new Grid { ColumnSpacing = 14 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.Add(tile, 0, 0);
            grid.Add(info, 1, 0);
            return Ui.Card(grid, 16, new Thickness(14, 12));
        }
    }
}
