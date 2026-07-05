using System;
using System.Linq;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Sky Tonight — a live, personal sky report: the moon's phase drawn as it
    // actually looks, the planets and constellations above you, and sun times.
    // Everything is computed on-device; location is optional.
    public class SkyPage : ContentPage
    {
        private SkyReport _report;
        private VerticalStackLayout _stack = null!;

        public SkyPage()
        {
            _report = SkyService.BuildReport(SkyService.Cached);
            Build();
            SwipeBack.Attach(this);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Refresh silently if location permission was granted before.
            var loc = await SkyService.GetQuietlyAsync();
            if (Math.Abs(loc.Lat - _report.Where.Lat) > 0.05 || Math.Abs(loc.Lon - _report.Where.Lon) > 0.05)
            {
                _report = SkyService.BuildReport(loc);
                Build();
            }
        }

        private void Build()
        {
            _stack = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(18, 4, 18, 28) };

            _stack.Add(new Label { Text = "Sky Tonight", FontFamily = Ui.Display, FontSize = Ui.S(30), TextColor = Theme.TextPrimary });
            string when = _report.IsNight ? "your sky right now" : "your sky after dark tonight";
            _stack.Add(new Label
            {
                Text = $"{DateTime.Now:dddd, MMMM d} · {when}",
                FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), TextColor = Theme.TextSecondary,
                Margin = new Thickness(0, 0, 0, 4)
            });

            _stack.Add(MoonHero());
            _stack.Add(MoonDetailCard());

            _stack.Add(Ui.SectionHeader("Planets above you"));
            if (_report.Planets.Count == 0)
                _stack.Add(Ui.Card(Ui.Muted("No planets are up this evening — they're hiding in the daytime sky. Check back in a few weeks; the sky never stops moving."), 16, new Thickness(16, 14)));
            foreach (var p in _report.Planets)
                _stack.Add(PlanetRow(p));

            _stack.Add(Ui.SectionHeader("Constellations above you"));
            foreach (var c in _report.Constellations)
                _stack.Add(ConstellationRow(c));

            _stack.Add(Ui.SectionHeader("The sun"));
            _stack.Add(SunCard());

            _stack.Add(LocationRow());

            var body = Nav.DetailScaffold("", new ScrollView { Content = _stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never }, Theme.Accent, out _);
            Content = new Grid { BackgroundColor = Theme.Bg, Children = { body } };
        }

        // ----- the moon, drawn as it looks tonight -----

        private View MoonHero()
        {
            var stars = new GraphicsView
            {
                Drawable = new StarFieldDrawable { Seed = DateTime.Now.DayOfYear },
                InputTransparent = true
            };
            var moon = new GraphicsView
            {
                Drawable = new MoonPhaseDrawable { ElongationDeg = _report.Moon.ElongationDeg },
                WidthRequest = 150, HeightRequest = 150,
                HorizontalOptions = LayoutOptions.Center,
                InputTransparent = true
            };

            var name = new Label
            {
                Text = _report.Moon.PhaseName,
                FontFamily = Ui.Display, FontSize = Ui.S(26),
                TextColor = Color.FromArgb("#F5F1E4"),
                HorizontalOptions = LayoutOptions.Center
            };
            var sub = new Label
            {
                Text = $"{_report.Moon.Illumination * 100:0}% lit · {MoonCountdown(_report.Moon)}",
                FontFamily = Ui.Fonts, FontSize = Ui.S(13.5),
                TextColor = Color.FromArgb("#B9BDD1"),
                HorizontalOptions = LayoutOptions.Center
            };

            var inner = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(16, 26, 16, 22) };
            inner.Add(moon);
            inner.Add(new BoxView { HeightRequest = 14, Color = Colors.Transparent });
            inner.Add(name);
            inner.Add(sub);

            var grid = new Grid();
            grid.Add(stars);
            grid.Add(inner);

            var hero = new Border
            {
                Content = grid,
                BackgroundColor = Color.FromArgb("#111527"),   // night sky, in both themes
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Padding = 0,
                Shadow = Theme.CardShadow()
            };
            var moonEntry = SpaceData.ByName("Moon");
            if (moonEntry != null)
                Ui.OnTap(hero, async (_, _) => await Nav.OpenSpace(moonEntry));
            Ui.Describe(hero, $"The moon tonight: {_report.Moon.PhaseName}, {_report.Moon.Illumination * 100:0} percent lit");
            return hero;
        }

        private View MoonDetailCard()
        {
            var col = new VerticalStackLayout { Spacing = 10 };
            if (_report.MoonUp)
                col.Add(Ui.Body($"The moon is {SkyService.Describe(_report.MoonAltDeg, _report.MoonAzDeg)} — day {_report.Moon.AgeDays:0} of its 29½-day cycle.", size: 14));
            else
                col.Add(Ui.Body($"The moon is below the horizon {(_report.IsNight ? "right now" : "this evening")} — day {_report.Moon.AgeDays:0} of its 29½-day cycle.", size: 14));

            col.Add(InfoRow("Next full moon", $"{_report.Moon.NextFullUtc.ToLocalTime():dddd, MMMM d}"));
            col.Add(InfoRow("Next new moon", $"{_report.Moon.NextNewUtc.ToLocalTime():dddd, MMMM d}"));
            return Ui.Card(col, 16, new Thickness(16, 14));
        }

        private static string MoonCountdown(SkyCalc.MoonInfo m)
        {
            if (m.Phase == SkyCalc.MoonPhaseKind.Full) return "full moon tonight";
            if (m.Phase == SkyCalc.MoonPhaseKind.New) return "new moon — darkest skies tonight";
            var next = m.Waxing ? m.NextFullUtc : m.NextNewUtc;
            int days = Math.Max(1, (int)Math.Round((next - DateTime.UtcNow).TotalDays));
            return m.Waxing ? $"full moon in {days} day{(days == 1 ? "" : "s")}"
                            : $"new moon in {days} day{(days == 1 ? "" : "s")}";
        }

        // ----- rows -----

        private View PlanetRow(PlanetSighting p)
        {
            var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            info.Add(new Label { Text = p.Name, FontFamily = Ui.Display, FontSize = Ui.S(18), TextColor = Theme.TextPrimary });
            string sub = p.Note.Length > 0 ? char.ToUpper(p.Note[0]) + p.Note[1..] : "";
            info.Add(new Label { Text = sub, FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), LineHeight = 1.3, TextColor = Theme.TextSecondary });

            var card = SightingCard(info, SkyService.Describe(p.AltDeg, p.AzDeg));
            var entry = SpaceData.ByName(p.Name);
            if (entry != null)
                Ui.OnTap(card, async (_, _) => await Nav.OpenSpace(entry));
            return card;
        }

        private View ConstellationRow(ConstellationSighting s)
        {
            var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            info.Add(new Label { Text = s.Constellation.Name, FontFamily = Ui.Display, FontSize = Ui.S(18), TextColor = Theme.TextPrimary });
            info.Add(new Label { Text = s.Constellation.Blurb, FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), LineHeight = 1.3, TextColor = Theme.TextSecondary });

            var card = SightingCard(info, SkyService.Describe(s.AltDeg, s.AzDeg));
            if (s.Constellation.LinkEntry is string link && SpaceData.ByName(link) is SpaceObject entry)
                Ui.OnTap(card, async (_, _) => await Nav.OpenSpace(entry));
            return card;
        }

        // Shared shell: info on the left, a direction chip on the right.
        private static Border SightingCard(View info, string direction)
        {
            var chip = Ui.Chip(ShortDirection(direction), Theme.AccentSoft, Theme.Accent);
            chip.VerticalOptions = LayoutOptions.Center;

            var grid = new Grid { ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(info, 0, 0);
            grid.Add(chip, 1, 0);
            return Ui.Card(grid, 16, new Thickness(16, 13));
        }

        // "high in the southeast" → "High · SE" for the chip.
        private static string ShortDirection(string longForm)
        {
            if (longForm == "almost straight overhead") return "Overhead";
            string dir = longForm[(longForm.LastIndexOf(' ') + 1)..];
            string abbr = dir switch
            {
                "north" => "N", "northeast" => "NE", "east" => "E", "southeast" => "SE",
                "south" => "S", "southwest" => "SW", "west" => "W", "northwest" => "NW", _ => dir
            };
            if (longForm.StartsWith("high")) return "High · " + abbr;
            if (longForm.StartsWith("low")) return "Low · " + abbr;
            return abbr;
        }

        private View SunCard()
        {
            var col = new VerticalStackLayout { Spacing = 10 };
            if (_report.NextSunsetLocal is DateTime set)
                col.Add(InfoRow("Sunset", SkyService.FormatTime(set)));
            if (_report.NextSunriseLocal is DateTime rise)
                col.Add(InfoRow("Sunrise", SkyService.FormatTime(rise)));
            if (_report.NextSunsetLocal is DateTime s2)
                col.Add(Ui.Muted($"Best stargazing starts around {SkyService.FormatTime(s2.AddMinutes(90))}, once the sky is properly dark.", 12.5));

            var card = Ui.Card(col, 16, new Thickness(16, 14));
            var sun = SpaceData.ByName("Sun");
            if (sun != null) Ui.OnTap(card, async (_, _) => await Nav.OpenSpace(sun));
            return card;
        }

        private static View InfoRow(string label, string value)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(Ui.Muted(label, 13.5), 0, 0);
            grid.Add(new Label
            {
                Text = value, FontFamily = Ui.Fonts, FontSize = Ui.S(14),
                FontAttributes = FontAttributes.Bold, TextColor = Theme.TextPrimary,
                HorizontalOptions = LayoutOptions.End
            }, 1, 0);
            return grid;
        }

        // ----- location -----

        private View LocationRow()
        {
            string text = _report.Where.FromDevice
                ? "Calculated for your area. Your location never leaves this phone."
                : "Showing a general Northern-sky view.";
            var col = new VerticalStackLayout { Spacing = 8, Margin = new Thickness(2, 6, 2, 0) };
            col.Add(Ui.Muted(text, 12.5));
            if (!_report.Where.FromDevice)
                col.Add(Ui.GhostButton("USE MY LOCATION", async (_, _) => await OnUseLocation()));
            return col;
        }

        private async System.Threading.Tasks.Task OnUseLocation()
        {
            var loc = await SkyService.RequestDeviceLocationAsync();
            if (loc == null)
            {
                await DisplayAlertAsync("No location", "DinoSpace couldn't get your location, so it will keep showing the general view. You can allow location for DinoSpace in your phone's settings any time.", "OK");
                return;
            }
            _report = SkyService.BuildReport(loc);
            Build();
        }
    }

    // Draws the moon's disc with the correct lit shape for a given sun-moon
    // elongation: crescent, quarter, gibbous or full, waxing lit on the right.
    public class MoonPhaseDrawable : IDrawable
    {
        public double ElongationDeg;
        public Color LitColor = Color.FromArgb("#F2ECD8");
        public Color DarkColor = Color.FromArgb("#272E42");

        public void Draw(ICanvas canvas, RectF rect)
        {
            float r = Math.Min(rect.Width, rect.Height) / 2f * 0.96f;
            float cx = rect.Center.X, cy = rect.Center.Y;

            canvas.Antialias = true;
            canvas.FillColor = DarkColor;
            canvas.FillCircle(cx, cy, r);

            double e = ElongationDeg * Math.PI / 180.0;
            double illum = (1 - Math.Cos(e)) / 2.0;
            int side = ElongationDeg < 180 ? 1 : -1;      // waxing lights the right edge

            if (illum > 0.005)
            {
                var path = new PathF();
                const int N = 44;
                // Down the lit limb (a half-circle), back up the terminator
                // (a half-ellipse whose width follows the phase).
                for (int i = 0; i <= N; i++)
                {
                    double th = -Math.PI / 2 + Math.PI * i / N;
                    float x = cx + side * r * (float)Math.Cos(th);
                    float y = cy + r * (float)Math.Sin(th);
                    if (i == 0) path.MoveTo(x, y); else path.LineTo(x, y);
                }
                double b = r * Math.Cos(e);               // signed terminator half-width
                for (int i = N; i >= 0; i--)
                {
                    double th = -Math.PI / 2 + Math.PI * i / N;
                    float x = cx + side * (float)(b * Math.Cos(th));
                    float y = cy + r * (float)Math.Sin(th);
                    path.LineTo(x, y);
                }
                path.Close();
                canvas.FillColor = LitColor;
                canvas.FillPath(path);
            }

            // Faint craters give the disc some character.
            canvas.FillColor = Colors.Black.WithAlpha(0.07f);
            canvas.FillCircle(cx - r * 0.30f, cy - r * 0.25f, r * 0.16f);
            canvas.FillCircle(cx + r * 0.22f, cy + r * 0.30f, r * 0.11f);
            canvas.FillCircle(cx + r * 0.35f, cy - r * 0.35f, r * 0.08f);
            canvas.FillCircle(cx - r * 0.15f, cy + r * 0.42f, r * 0.07f);

            canvas.StrokeColor = Colors.White.WithAlpha(0.10f);
            canvas.StrokeSize = 1.5f;
            canvas.DrawCircle(cx, cy, r);
        }
    }

    // A calm, deterministic star field (same sky all day, new sky tomorrow).
    public class StarFieldDrawable : IDrawable
    {
        public int Seed;

        public void Draw(ICanvas canvas, RectF rect)
        {
            var rng = new Random(Seed);
            canvas.Antialias = true;
            for (int i = 0; i < 60; i++)
            {
                float x = (float)(rng.NextDouble() * rect.Width);
                float y = (float)(rng.NextDouble() * rect.Height);
                float size = 0.6f + (float)rng.NextDouble() * 1.1f;
                canvas.FillColor = Colors.White.WithAlpha(0.12f + (float)rng.NextDouble() * 0.55f);
                canvas.FillCircle(x, y, size);
            }
        }
    }
}
