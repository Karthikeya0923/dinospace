using System;
using System.Linq;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Scan Sky's landing page — a live, personal sky report: a big "scan
    // your sky" button into the AR view, then the moon's phase drawn as it
    // actually looks tonight, the next meteor shower, the planets and
    // constellations above you, and sun times. All computed on-device.
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

            string when = _report.IsNight ? "your sky right now" : "your sky after dark tonight";
            _stack.Add(new Label
            {
                Text = $"{DateTime.Now:dddd, MMMM d} · {when}",
                FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), TextColor = Theme.TextSecondary,
                HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 2)
            });

            _stack.Add(ScanButton());
            _stack.Add(MoonHero());
            _stack.Add(MoonDetailCard());
            _stack.Add(AskNovaCard());
            _stack.Add(TelescopeCard());
            _stack.Add(ShowerCard());

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

            // Learn the Sky lives at the very bottom — it's the "what does all
            // this mean?" reference, reached after tonight's live sky.
            _stack.Add(LearnRow());

            _stack.Add(LocationRow());

            var body = Nav.DetailScaffoldFixed("scan sky", new ScrollView { Content = _stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never });
            Content = Ui.PageRoot(body);
        }

        // The star of the page: a storybook pill that opens the live AR view.
        private static View ScanButton()
        {
            var row = new HorizontalStackLayout
            {
                Spacing = 10, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
            };
            var ic = Ui.Icon(Ui.IconScanSky, 26);
            ic.VerticalOptions = LayoutOptions.Center;
            row.Add(ic);
            row.Add(new Label
            {
                Text = "scan your sky",
                FontFamily = Ui.Display, FontSize = 19,
                TextColor = Theme.TextPrimary, VerticalOptions = LayoutOptions.Center
            });

            var pill = new Border
            {
                Content = row,
                BackgroundColor = Theme.AccentSoft,
                Stroke = Theme.TextPrimary.WithAlpha(0.55f), StrokeThickness = 1.6,
                StrokeShape = new RoundRectangle { CornerRadius = 100 },
                HeightRequest = 58, Padding = new Thickness(20, 0),
                Margin = new Thickness(0, 2, 0, 4)
            };
            Ui.OnTap(pill, async (_, _) => await Nav.Push(() => new SkyViewPage()));
            Ui.Describe(pill, "Scan your sky with the camera");
            return pill;
        }

        // ----- the moon, drawn as it looks tonight -----

        private View MoonHero()
        {
            var stars = new GraphicsView
            {
                Drawable = new StarFieldDrawable { Seed = DateTime.Now.DayOfYear },
                InputTransparent = true
            };
            // Tonight's phase as one of Karthik's eight moon drawings
            // (fullmoon.png, waxinggibbous.png, ...) — blank until it lands.
            var moon = Ui.Icon(Ui.MoonSlot(_report.Moon.PhaseName), 150);

            var name = new Label
            {
                Text = _report.Moon.PhaseName,
                FontFamily = Ui.Display, FontSize = Ui.S(32),
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

            var rs = RiseSet.MoonRiseSet(_report.Where.Lat, _report.Where.Lon, DateTime.UtcNow.Date);
            if (rs.RiseUtc is DateTime mr)
                col.Add(InfoRow("Moonrise", SkyService.FormatTime(mr.ToLocalTime())));
            if (rs.SetUtc is DateTime ms)
                col.Add(InfoRow("Moonset", SkyService.FormatTime(ms.ToLocalTime())));
            col.Add(InfoRow("Next full moon", $"{_report.Moon.NextFullUtc.ToLocalTime():dddd, MMMM d}"));
            col.Add(InfoRow("Next new moon", $"{_report.Moon.NextNewUtc.ToLocalTime():dddd, MMMM d}"));
            return Ui.Card(col, 16, new Thickness(16, 14));
        }

        // Two or three showpiece deep-sky objects that are actually up now —
        // the "worth pointing binoculars at" card.
        private View TelescopeCard()
        {
            var utc = DateTime.UtcNow.AddHours(_report.IsNight ? 0 : 3);   // roughly after dark
            var up = SkyMap.DeepSky
                .Select(d =>
                {
                    var (alt, az) = SkyCalc.AltAz(d.RaHours * 15.0, d.DecDeg, _report.Where.Lat, _report.Where.Lon, utc);
                    return (d, alt, az);
                })
                .Where(x => x.alt > 25)
                .OrderByDescending(x => x.alt)
                .Take(3)
                .ToList();
            if (up.Count == 0) return new BoxView { HeightRequest = 0 };

            var col = new VerticalStackLayout { Spacing = 10 };
            col.Add(new Label
            {
                Text = "With binoculars tonight",
                FontFamily = Ui.Display, FontSize = Ui.S(18), TextColor = Theme.TextPrimary
            });
            foreach (var (d, alt, az) in up)
                col.Add(Ui.Muted($"• {d.Name} — {d.Blurb}. Look {SkyService.Describe(alt, az)}.", 12.5));
            return Ui.Card(col, 16, new Thickness(16, 14));
        }

        // One tap drops into the NovaSaur chat with tonight's sky already
        // asked — the AI reads the same live report this page is built from.
        private static View AskNovaCard()
        {
            var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            info.Add(new Label { Text = "Ask NovaSaur about tonight", FontFamily = Ui.Display, FontSize = Ui.S(18), TextColor = Theme.TextPrimary });
            info.Add(new Label
            {
                Text = "Your AI guide reads tonight's sky and tells you where to look",
                FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), LineHeight = 1.3, TextColor = Theme.TextSecondary
            });

            // NovaSaur's own face once mascot_ask.png lands (the icon_ask
            // slot covers until then) — never a random star.
            var dot = new Border
            {
                WidthRequest = 38, HeightRequest = 38,
                BackgroundColor = Ui.MultiplyAlpha(Theme.AccentNova, 0.18f),
                Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 19 },
                VerticalOptions = LayoutOptions.Center,
                Content = Ui.Mascot("mascot_ask", 22, Ui.IconAsk)
            };

            var grid = new Grid { ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.Add(dot, 0, 0);
            grid.Add(info, 1, 0);

            var card = Ui.Card(grid, 16, new Thickness(16, 13));
            Ui.OnTap(card, async (_, _) =>
            {
                NovaView.Ask("What's in the sky tonight?");
                await Nav.Push(() => new NovaPage());
            });
            Ui.Describe(card, "Ask NovaSaur what's in the sky tonight");
            return card;
        }

        // The next meteor shower (and any active one) with a moonlight verdict.
        private View ShowerCard()
        {
            var now = DateTime.UtcNow;
            var col = new VerticalStackLayout { Spacing = 10 };
            col.Add(new Label { Text = "Meteor showers", FontFamily = Ui.Display, FontSize = Ui.S(18), TextColor = Theme.TextPrimary });

            var active = MeteorShowers.ActiveOn(now).OrderByDescending(s => s.Zhr).ToList();
            if (active.Count > 0)
            {
                var s = active[0];
                var (alt, az) = SkyCalc.AltAz(s.RadiantRaHours * 15.0, s.RadiantDecDeg, _report.Where.Lat, _report.Where.Lon, now);
                string from = alt > 10 ? $"They streak away from the {SkyCalc.Compass(az)} — but can appear anywhere overhead."
                                       : "Best after midnight, once the radiant climbs higher.";
                col.Add(Ui.Muted($"The {s.Name} are active now — {char.ToLower(s.Blurb[0]) + s.Blurb[1..]}. {from}", 12.5));
            }

            var (next, peak) = MeteorShowers.Next(now);
            string verdict = MeteorShowers.MoonVerdict(MeteorShowers.MoonInterference(next, peak.Year));
            col.Add(InfoRow("Next peak", $"{next.Name} · {peak:MMMM d}"));
            col.Add(Ui.Muted($"Up to {next.Zhr} meteors an hour under perfect skies — {verdict}.", 12.5));
            return Ui.Card(col, 16, new Thickness(16, 14));
        }

        // Compact link into the "what does all this mean?" page.
        private static View LearnRow()
        {
            var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            info.Add(new Label { Text = "Learn the sky", FontFamily = Ui.Display, FontSize = Ui.S(18), TextColor = Theme.TextPrimary });
            info.Add(new Label
            {
                Text = "What the phases mean, why stars move, how to spot a planet",
                FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), LineHeight = 1.3, TextColor = Theme.TextSecondary
            });

            var chevron = Ui.Icon(Ui.IconChevron, 24);
            chevron.VerticalOptions = LayoutOptions.Center;

            var grid = new Grid { ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(info, 0, 0);
            grid.Add(chevron, 1, 0);

            var card = Ui.Card(grid, 16, new Thickness(16, 13));
            Ui.OnTap(card, async (_, _) => await Nav.Push(() => new SkyLearnPage()));
            Ui.Describe(card, "Learn the sky: what moon phases mean, why stars move, how to spot a planet");
            return card;
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
            // Open an entry only when the encyclopedia truly has THIS
            // constellation (Orion today, more as their art lands) — tapping
            // Cygnus must not open the Milky Way.
            if (SpaceData.ByName(s.Constellation.Name) is SpaceObject entry)
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
            // The real end of twilight — sun 18° down — not a rule of thumb.
            var (dusk, _) = RiseSet.Twilights(_report.Where.Lat, _report.Where.Lon, DateTime.UtcNow.Date);
            if (dusk is DateTime dk)
                col.Add(Ui.Muted($"Fully dark from {SkyService.FormatTime(dk.ToLocalTime())} — that's when the faintest stars come out.", 12.5));
            else if (_report.NextSunsetLocal is DateTime s2)
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
