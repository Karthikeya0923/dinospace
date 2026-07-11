using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Scan Sky — hold your phone up and the live camera view fills the
    // screen with ONLY what is genuinely above you right now: the sun, the
    // phase-correct moon, the planets, and — after dark — the bright named
    // stars and the constellation figures they belong to. Nothing invented,
    // nothing you couldn't really see. The target card names something only
    // when the crosshair is truly on it. No camera? A painted twilight-aware
    // sky stands in.
    public class SkyViewPage : ContentPage
    {
        private double _lat, _lon;
        private readonly SkyViewDrawable _drawable;
        private GraphicsView _view = null!;
        private CameraView? _camera;
        private Grid _root = null!;

        // chrome
        private Label _targetName = null!, _targetKind = null!, _targetBlurb = null!;
        private Border _targetCard = null!, _learnBtn = null!, _askBtn = null!;
        private GraphicsView _compass = null!;
        private Label _viewAllLabel = null!;
        private readonly CompassRoseDrawable _rose = new();
        private SpaceObject? _learnTarget;

        private double _az = 180, _alt = 30;
        private bool _sensorMode, _cameraOn;
        private bool _starting;
        private int _tick;
        private IDispatcherTimer? _timer;
        private CancellationTokenSource? _camCts;

        private DateTime SkyUtc => DateTime.UtcNow;

        public SkyViewPage()
        {
            var where = SkyService.Cached;
            _lat = where.Lat; _lon = where.Lon;
            _drawable = new SkyViewDrawable { Lat = _lat, Lon = _lon };
            Build();
        }

        private void Build()
        {
            _view = new GraphicsView { Drawable = _drawable, BackgroundColor = Colors.Transparent };

            var pan = new PanGestureRecognizer();
            double startAz = 0, startAlt = 0;
            pan.PanUpdated += (_, e) =>
            {
                if (_sensorMode) return;
                if (e.StatusType == GestureStatus.Started) { startAz = _az; startAlt = _alt; }
                else if (e.StatusType == GestureStatus.Running)
                {
                    double scale = _drawable.FovDeg / Math.Max(1, _view.Width);
                    _az = (startAz - e.TotalX * scale + 360) % 360;
                    _alt = Math.Clamp(startAlt + e.TotalY * scale, -20, 89);
                }
            };
            _view.GestureRecognizers.Add(pan);

            // ----- top bar: close · title · darkness toggle -----
            var close = ChromeButton(Ui.Icon(Ui.IconClose, 22));
            Ui.OnTap(close, async (_, _) =>
            {
                try { if (Shell.Current.Navigation.NavigationStack.Count > 1) await Shell.Current.Navigation.PopAsync(); } catch { }
            });
            Ui.Describe(close, "Close Scan Sky");

            var title = new Label
            {
                Text = "Scan Sky",
                FontFamily = Ui.Display, FontSize = 22, TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Center
            };

            var topBar = new Grid { Padding = new Thickness(14, 14, 14, 0), ColumnSpacing = 10, VerticalOptions = LayoutOptions.Start };
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            topBar.Add(close, 0, 0);
            topBar.Add(title, 1, 0);

            // ----- target card (top-right, under the bar) -----
            _targetName = new Label { FontFamily = Ui.Display, FontSize = Ui.S(19), TextColor = Colors.White };
            _targetKind = new Label { FontFamily = Ui.Fonts, FontSize = Ui.S(11.5), TextColor = Color.FromArgb("#E8CD8C") };
            _targetBlurb = new Label { FontFamily = Ui.Fonts, FontSize = Ui.S(12), LineHeight = 1.35, TextColor = Color.FromArgb("#E4E2D2"), MaxLines = 3, LineBreakMode = LineBreakMode.TailTruncation };
            var learnLabel = new Label
            {
                Text = "Learn more", FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#F2E8C8"), HorizontalTextAlignment = TextAlignment.Center
            };
            _learnBtn = new Border
            {
                Content = learnLabel,
                BackgroundColor = Color.FromArgb("#33FFFFFF"), Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = new Thickness(12, 7),
                Margin = new Thickness(0, 6, 0, 0), IsVisible = false
            };
            Ui.OnTap(_learnBtn, async (_, _) => { if (_learnTarget != null) await Nav.OpenSpace(_learnTarget); });

            // Whatever's under the crosshair, NovaSaur can talk about it —
            // stars, nebulae and galaxies included, not just encyclopedia entries.
            var askLabel = new Label
            {
                Text = "Ask Nova", FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#F2E8C8"), HorizontalTextAlignment = TextAlignment.Center
            };
            _askBtn = new Border
            {
                Content = askLabel,
                BackgroundColor = Color.FromArgb("#33FFFFFF"), Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = new Thickness(12, 7),
                Margin = new Thickness(0, 6, 0, 0), IsVisible = false
            };
            Ui.OnTap(_askBtn, async (_, _) =>
            {
                string name = _targetName.Text;
                if (string.IsNullOrWhiteSpace(name)) return;
                string what = _targetKind.Text.StartsWith("Constellation") ? $"the constellation {name}" : name;
                NovaView.Ask($"Tell me about {what}.");
                await Nav.Push(() => new NovaPage());
            });
            Ui.Describe(_askBtn, "Ask Nova about this object");

            // The card tucks into the top-right corner, out of the sky's way —
            // the crosshair and the stars stay unblocked in the middle.
            var targetBtns = new HorizontalStackLayout { Spacing = 8, Children = { _learnBtn, _askBtn } };
            var targetCol = new VerticalStackLayout { Spacing = 3, Children = { _targetName, _targetKind, _targetBlurb, targetBtns } };
            _targetCard = new Border
            {
                Content = targetCol,
                BackgroundColor = Color.FromArgb("#B3161A10"),
                Stroke = Color.FromArgb("#44A89B6E"), StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(14, 12),
                MaximumWidthRequest = 280,
                HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(14, 12, 14, 0),
                IsVisible = false
            };

            // ----- compass rose (bottom-left) -----
            _compass = new GraphicsView { Drawable = _rose, WidthRequest = 76, HeightRequest = 76, InputTransparent = true, HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.End, Margin = new Thickness(16, 0, 0, 86) };

            // ----- "view all" (bottom-left): stars & their names on demand -----
            _viewAllLabel = new Label
            {
                Text = "view all",
                FontFamily = Ui.Display, FontSize = 14,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            var viewAll = new Border
            {
                Content = _viewAllLabel,
                BackgroundColor = Color.FromArgb("#4D000000"),
                Stroke = Color.FromArgb("#66FFFFFF"), StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 100 },
                Padding = new Thickness(18, 9),
                HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(16, 0, 0, 14)
            };
            Ui.OnTap(viewAll, (_, _) =>
            {
                _drawable.ShowAll = !_drawable.ShowAll;
                _viewAllLabel.Text = _drawable.ShowAll ? "less" : "view all";
            });
            Ui.Describe(viewAll, "Show everything in the sky");

            _root = new Grid { BackgroundColor = Color.FromArgb("#070B14") };
            // camera slot is index 0 (inserted on demand); overlay stack above it
            _root.Add(_view);
            _root.Add(topBar);
            _root.Add(_targetCard);
            _root.Add(_compass);
            _root.Add(viewAll);
            Content = _root;
            Shell.SetNavBarIsVisible(this, false);
        }

        private static Border ChromeButton(View inner) => new()
        {
            Content = inner,
            BackgroundColor = Color.FromArgb("#4D000000"),
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            WidthRequest = 44, HeightRequest = 44
        };

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            SetLandscape(true);
            StartSensor();

            // The sky HAS to be computed for where the phone really is. The
            // cached fallback (latitude 45°, longitude guessed from the time
            // zone) can be thousands of kilometres off — far enough to put
            // the moon on the wrong side of the sky and name the wrong star
            // under the crosshair. Ask once, then re-anchor everything.
            _ = RefreshLocationAsync();

            // The timer gets exactly one Tick handler for the page's lifetime.
            // OnAppearing runs again every time a pushed page (Learn More) pops
            // back to us — resubscribing here is what used to pile up handlers
            // until the overlay ground to a halt.
            if (_timer == null)
            {
                _timer = Dispatcher.CreateTimer();
                _timer.Interval = TimeSpan.FromMilliseconds(50);
                _timer.Tick += (_, _) =>
                {
                    _drawable.CenterAz = _az;
                    _drawable.CenterAlt = _alt;
                    _rose.HeadingDeg = _az;
                    _view.Invalidate();          // continuous: twinkle + meteors
                    _compass.Invalidate();
                    if (++_tick % 6 == 0) UpdateTarget();   // naming can be lazier
                };
            }
            _timer.Start();

            await StartCameraAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _timer?.Stop();
            StopSensor();
            StopCamera();
            SetLandscape(false);
        }

        private bool _locationTried;
        private bool _locationReal;
        private async System.Threading.Tasks.Task RefreshLocationAsync()
        {
            // Keeps retrying on every appearance until a real fix lands — a
            // one-shot attempt used to leave the whole sky anchored to the
            // fallback guess if the first try raced the permission dialog.
            if (_locationTried) return;
            _locationTried = true;
            try
            {
                var loc = await SkyService.RequestDeviceLocationAsync();
                if (loc == null) { _locationTried = false; return; }
                _locationReal = true;
                if (Math.Abs(loc.Lat - _lat) < 0.05 && Math.Abs(loc.Lon - _lon) < 0.05) return;
                _lat = loc.Lat; _lon = loc.Lon;
                _drawable.Lat = _lat; _drawable.Lon = _lon;
                // Magnetic declination depends on where you are — restart the
                // pointing sensor so azimuths are true-north for the new spot.
                if (_sensorMode) { StopSensor(); StartSensor(); }
            }
            catch { _locationTried = false; }
        }

        // Scanning the sky is a two-hands, phone-up activity — landscape gives
        // the widest view and matches how people naturally hold the phone up.
        // Restored to the system default the moment the page goes away.
        private static void SetLandscape(bool on)
        {
#if ANDROID
            try
            {
                if (Platform.CurrentActivity is Android.App.Activity a)
                    a.RequestedOrientation = on
                        ? Android.Content.PM.ScreenOrientation.SensorLandscape
                        : Android.Content.PM.ScreenOrientation.Unspecified;
            }
            catch { }
#endif
        }

        // ----- camera passthrough -----

        private async System.Threading.Tasks.Task StartCameraAsync()
        {
            if (_camera != null || _starting) return;   // already running or mid-start
            _starting = true;
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted) { _drawable.CameraBehind = false; return; }

                _camera = new CameraView { InputTransparent = true };
                _root.Insert(0, _camera);
                _camCts = new CancellationTokenSource();

                // The flaky part: StartCameraPreview only works once the native
                // view exists, and the landscape rotation recreates it a beat
                // later. Calling too early silently no-ops into a blank frame —
                // that was the "navy background instead of the camera" bug. So
                // we wait for the handler to attach, then start, and retry a
                // few times because the first call right after a rotation can
                // still miss. Simple loop, but it makes the camera reliable.
                for (int attempt = 0; attempt < 6 && !_cameraOn; attempt++)
                {
                    for (int i = 0; i < 20 && _camera?.Handler == null; i++)
                        await System.Threading.Tasks.Task.Delay(50);
                    if (_camera == null) return;               // page left mid-start
                    try
                    {
                        await _camera.StartCameraPreview(_camCts.Token);
                        _cameraOn = true;
                        _drawable.CameraBehind = true;         // overlay goes transparent, no painted sky
                        MakeCameraFill(_camera.Handler?.PlatformView);
                    }
                    catch
                    {
                        await System.Threading.Tasks.Task.Delay(250);   // let rotation/layout settle, then retry
                    }
                }

                if (!_cameraOn) StopCamera();                  // give up gracefully -> painted sky
            }
            catch
            {
                // any camera trouble -> quietly fall back to the rendered sky
                StopCamera();
            }
            finally { _starting = false; }
        }

        // The native preview letterboxes by default — two black pillars
        // beside a 4:3 frame. Centre-crop instead so the camera really fills
        // the whole screen, like every AR viewfinder.
        private static void MakeCameraFill(object? platformView)
        {
#if ANDROID
            try
            {
                if (platformView is AndroidX.Camera.View.PreviewView pv)
                {
                    pv.SetScaleType(AndroidX.Camera.View.PreviewView.ScaleType.FillCenter);
                    return;
                }
                if (platformView is Android.Views.ViewGroup vg)
                    for (int i = 0; i < vg.ChildCount; i++)
                        MakeCameraFill(vg.GetChildAt(i));
            }
            catch { }
#endif
        }

        private void StopCamera()
        {
            try { _camCts?.Cancel(); } catch { }
            if (_camera != null)
            {
                try { _camera.StopCameraPreview(); } catch { }
                try { _root.Remove(_camera); } catch { }
                // Without this the native camera stays claimed by the dead
                // preview, and the next visit gets a frozen black frame.
                try { _camera.Handler?.DisconnectHandler(); } catch { }
            }
            try { _camCts?.Dispose(); } catch { }
            _camCts = null;
            _camera = null;
            _cameraOn = false;
            _drawable.CameraBehind = false;
        }

        // ----- orientation sensor -----
        // SkyPointing wraps Android's north-referenced rotation vector and
        // corrects magnetic to true north. The old MAUI OrientationSensor used
        // the *game* rotation vector, whose yaw is arbitrary — the overlay was
        // internally consistent but swung to a random heading each session,
        // which is why the target card named things far from where you aimed.
        private Services.SkyPointing? _pointing;

        private void StartSensor()
        {
            try
            {
                _pointing = new Services.SkyPointing();
                _pointing.Reading += OnReading;
                _sensorMode = _pointing.Start(_lat, _lon);
                if (!_sensorMode) { _pointing.Reading -= OnReading; _pointing = null; }
            }
            catch { _sensorMode = false; _pointing = null; }
        }

        private void StopSensor()
        {
            try
            {
                if (_pointing != null)
                {
                    _pointing.Reading -= OnReading;
                    _pointing.Stop();
                }
            }
            catch { }
            _pointing = null;
            _sensorMode = false;
        }

        private void OnReading(double alt, double az)
        {
            double dAz = ((az - _az + 540) % 360) - 180;
            _az = (_az + dAz * 0.25 + 360) % 360;
            _alt += (alt - _alt) * 0.25;
        }

        // ----- what's under the crosshair -----

        private void UpdateTarget()
        {
            var utc = SkyUtc;
            double jd = SkyCalc.JulianDay(utc);
            var frame = new SkyMap.LocalFrame(_lat, _lon, utc);

            // THE fix for "the crosshair is on X but the card says Y": the
            // card now measures distance in SCREEN PIXELS using the exact
            // same projection that draws the overlay. Whatever is drawn
            // nearest the crosshair is what gets named — they can never
            // disagree again.
            float vw = (float)_view.Width, vh = (float)_view.Height;
            if (vw < 10 || vh < 10) return;
            var view = new SkyMap.View(_lat, _lon, utc, _az, _alt, _drawable.FovDeg, Math.Max(vw, vh), vw / 2f, vh / 2f);
            float cxPx = vw / 2f, cyPx = vh / 2f;

            double SepTo(double alt, double az)
            {
                var (x, y, vis) = SkyMap.Project(alt, az, view);
                if (!vis) return double.MaxValue;
                return Math.Sqrt((x - cxPx) * (x - cxPx) + (y - cyPx) * (y - cyPx));
            }
            double SepToVec(double n, double e, double u)
            {
                var (x, y, vis) = SkyMap.ProjectVector(n, e, u, view);
                if (!vis) return double.MaxValue;
                return Math.Sqrt((x - cxPx) * (x - cxPx) + (y - cyPx) * (y - cyPx));
            }
            // Everything inside the crosshair ring (~34 px radius, drawn at
            // 50) is a legitimate aim; a small bonus keeps the moon easy to
            // hit without letting it steal a neighbour.
            const double Ring = 50;

            string? name = null, kind = null, blurb = null;
            SpaceObject? entry = null;

            // Whether stars can actually be seen right now — daylight names
            // only the sun, moon and planets.
            var (sunRa0, sunDec0) = SkyCalc.SunRaDec(jd);
            var (sunAltNow, _) = SkyCalc.AltAz(sunRa0, sunDec0, _lat, _lon, utc);
            bool skyDark = sunAltNow < -6;

            // Candidates compete on angular distance minus a small priority
            // bonus, and every radius is tight: the card only ever names
            // something the crosshair is really on. Nothing close enough?
            // The card stays hidden.
            double bestScore = double.MaxValue;
            void Consider(double sep, double maxSep, double bonus, string n, string k, string? b, SpaceObject? e)
            {
                if (sep > maxSep) return;
                double score = sep - bonus;
                if (score >= bestScore) return;
                bestScore = score;
                name = n; kind = k; blurb = b; entry = e;
            }

            // the moon — a little extra reach because it is big. If it is
            // drawn near the horizon, it can be named there too.
            var (mra, mdec) = SkyCalc.MoonRaDec(jd);
            var (mAlt, mAz) = SkyCalc.AltAz(mra, mdec, _lat, _lon, utc);
            {
                var e = SpaceData.ByName("Moon");
                Consider(SepTo(mAlt, mAz), Ring, 12,
                         "Moon", mAlt < 0 ? "Earth's moon · below the horizon right now" : "Earth's moon",
                         e?.ShortDescription, e);
            }

            // planets
            foreach (var b in Enum.GetValues<SkyCalc.Body>())
            {
                var (ra, dec, _) = SkyCalc.PlanetRaDec(b, jd);
                var (alt, az) = SkyCalc.AltAz(ra, dec, _lat, _lon, utc);
                var e = SpaceData.ByName(b.ToString());
                Consider(SepTo(alt, az), Ring, 6,
                         b.ToString(), alt < 0 ? "Planet · below the horizon right now" : "Planet",
                         e?.ShortDescription, e);
            }

            // the sun, during the day
            var (sra, sdec) = SkyCalc.SunRaDec(jd);
            var (sAlt, sAz) = SkyCalc.AltAz(sra, sdec, _lat, _lon, utc);
            {
                var e = SpaceData.ByName("Sun");
                Consider(SepTo(sAlt, sAz), Ring, 8,
                         "Sun", sAlt < 0 ? "Our star · below the horizon right now" : "Our star",
                         e?.ShortDescription, e);
            }

            // stars — only the bright NAMED ones, only while "view all"
            // shows them (any hour: view-all is the "show me everything
            // anyway" switch), and only when the crosshair is right on one.
            if (_drawable.ShowAll)
            {
                var stars = SkyCatalog.Stars;
                var sv = SkyMap.StarVectors;
                for (int i = 0; i < stars.Length; i++)
                {
                    var s = stars[i];
                    if (s.Mag > SkyViewDrawable.BrightStarMag) break;   // sorted by brightness
                    if (s.Name.Length == 0) continue;
                    var (n, e, u) = frame.Horizon(sv[i * 3], sv[i * 3 + 1], sv[i * 3 + 2]);
                    if (u < 0) continue;   // only stars genuinely above the horizon
                    double sep = SepToVec(n, e, u);
                    Consider(sep, Ring * 0.8, 0,
                             s.Name,
                             $"Star · {s.Colour()}",
                             $"Magnitude {s.Mag:0.0#} — one of the stars bright enough to carry a name.",
                             null);
                }
            }

            // deep sky: only in "view all", only right under the crosshair
            if (_drawable.ShowAll)
            {
                var dsos = SkyDeepSkyCatalog.All;
                for (int i = 0; i < dsos.Length; i++)
                {
                    var d = dsos[i];
                    if (d.Mag > SkyViewDrawable.DsoShowMag) continue;
                    var (alt, az) = SkyCalc.AltAz(d.RaHours * 15.0, d.DecDeg, _lat, _lon, utc);
                    if (alt < 1) continue;   // only what is genuinely up
                    Consider(SepTo(alt, az), Ring * 0.8, 0,
                             d.Name, d.Kind, d.Blurb, SpaceData.ByName(d.Name.Split(" (")[0]));
                }
            }

            // fall back to a constellation whose stick-figure is on screen,
            // measured to the centre of its drawn stars.
            if (name == null)
            {
                string? bestFig = null; double bestPx = 170;
                foreach (var f in SkyMap.Figures)
                {
                    double sx = 0, sy = 0; int n = 0;
                    foreach (var st in f.Stars)
                    {
                        var (alt, az) = SkyCalc.AltAz(st.ra * 15.0, st.dec, _lat, _lon, utc);
                        if (alt < 0) continue;   // a figure counts only where it is really up
                        var (x, y, vis) = SkyMap.Project(alt, az, view);
                        if (!vis) continue;
                        sx += x; sy += y; n++;
                    }
                    if (n < 3) continue;   // not meaningfully on screen
                    double d = Math.Sqrt((sx / n - cxPx) * (sx / n - cxPx) + (sy / n - cyPx) * (sy / n - cyPx));
                    if (d < bestPx) { bestPx = d; bestFig = f.Name; }
                }
                if (bestFig != null)
                {
                    name = bestFig; kind = "Constellation";
                    var c = SkyData.All.FirstOrDefault(x => x.Name == bestFig);
                    blurb = c != null ? c.Blurb + "." : "A constellation above you right now.";
                    // Only link when the encyclopedia truly has THIS
                    // constellation (like Orion) — never a lookalike entry.
                    entry = SpaceData.ByName(bestFig);
                }
            }

            // view-all labels the other constellations at their hearts —
            // those drawn labels are nameable when the crosshair is on them.
            // Pointing at genuinely empty sky names NOTHING: no ghosts.
            if (name == null && _drawable.ShowAll)
            {
                Constellation? nearest = null; double bestPx2 = 130;
                foreach (var c in SkyData.All)
                {
                    var (alt, az) = SkyCalc.AltAz(c.RaHours * 15.0, c.DecDeg, _lat, _lon, utc);
                    if (alt < 0) continue;   // label isn't drawn -> not nameable
                    var (x, y, vis) = SkyMap.Project(alt, az, view);
                    if (!vis) continue;
                    double dpx = Math.Sqrt((x - cxPx) * (x - cxPx) + (y - cyPx) * (y - cyPx));
                    if (dpx < bestPx2) { bestPx2 = dpx; nearest = c; }
                }
                if (nearest != null)
                {
                    name = nearest.Name; kind = "Constellation";
                    blurb = nearest.Blurb + ".";
                    entry = SpaceData.ByName(nearest.Name);
                }
            }

            if (name == null)
            {
                _targetCard.IsVisible = false;
                return;
            }

            _learnTarget = entry;
            _targetName.Text = name;
            _targetKind.Text = kind ?? "";
            _targetBlurb.Text = blurb ?? "";
            _learnBtn.IsVisible = entry != null;
            _askBtn.IsVisible = true;
            _targetCard.IsVisible = true;
        }
    }

    // Colour helper shared by the drawable and the target card.
    internal static class StarLook
    {
        public static string Colour(this CatStar s) => s.TempK switch
        {
            0 => "white",
            < 3700 => "red-orange",
            < 5000 => "orange",
            < 6000 => "yellow",
            < 7500 => "yellow-white",
            < 10000 => "white",
            _ => "blue-white"
        };

        public static Color Tint(this CatStar s) => s.TempK switch
        {
            0 => Color.FromArgb("#F2F5FA"),
            < 3700 => Color.FromArgb("#FFAA80"),
            < 5000 => Color.FromArgb("#FFC888"),
            < 6000 => Color.FromArgb("#FFE9B0"),
            < 7500 => Color.FromArgb("#FFF6DC"),
            < 10000 => Color.FromArgb("#F2F5FA"),
            _ => Color.FromArgb("#CFE2FF")
        };
    }

    // One frame of the pointed-at sky: the sun, a phase-correct moon, the
    // planets drawn with their signature looks, and — after dark — the
    // bright named stars and their constellation figures. A horizon line and
    // compass letters anchor it. Transparent when the camera shows behind; a
    // twilight-aware painted sky otherwise. Nothing fake, nothing invisible.
    public class SkyViewDrawable : IDrawable
    {
        public double Lat, Lon;
        public double CenterAz = 180, CenterAlt = 30;
        public double FovDeg = 95;
        public bool CameraBehind;

        // Off: just the solar system and the constellations — clean, like a
        // picture book. On ("view all"): the bright named stars, all the
        // constellation figures and the famous deep-sky objects join in — at
        // any hour, because view-all means "show me where they are anyway".
        public bool ShowAll;

        // Naked-eye-famous deep sky only (view-all): the showpieces.
        public const float DsoShowMag = 6.5f;

        // The naked-eye cut for "a star worth drawing": every star this
        // bright has a proper name and really is visible from a backyard.
        public const float BrightStarMag = 2.2f;

        public void Draw(ICanvas canvas, RectF rect)
        {
            var utc = DateTime.UtcNow;
            var v = new SkyMap.View(Lat, Lon, utc, CenterAz, CenterAlt, FovDeg, Math.Max(rect.Width, rect.Height), rect.Width / 2f, rect.Height / 2f);
            var frame = new SkyMap.LocalFrame(Lat, Lon, utc);
            long nowMs = Environment.TickCount64;
            canvas.Antialias = true;

            Color lineC = Colors.White;
            Color labelC = Color.FromArgb("#EEF1F8");

            double jd = SkyCalc.JulianDay(utc);
            var (sunRa, sunDec) = SkyCalc.SunRaDec(jd);
            var (sunAlt, sunAz) = SkyCalc.AltAz(sunRa, sunDec, Lat, Lon, utc);

            // painted sky only when there's no camera behind us — and it knows
            // what time it is: day blue, twilight ember, astronomical night.
            if (!CameraBehind)
            {
                (Color top, Color bottom) = sunAlt switch
                {
                    > 0 => (Color.FromArgb("#2E6BB8"), Color.FromArgb("#7FB2E8")),
                    > -6 => (Color.FromArgb("#101A3C"), Color.FromArgb("#B85E3A")),
                    > -12 => (Color.FromArgb("#080D22"), Color.FromArgb("#28356B")),
                    _ => (Color.FromArgb("#05070F"), Color.FromArgb("#16203C")),
                };
                var paint = new LinearGradientPaint
                {
                    StartColor = top, EndColor = bottom,
                    StartPoint = new Point(0, 0), EndPoint = new Point(0, 1)
                };
                canvas.SetFillPaint(paint, rect);
                canvas.FillRectangle(rect);
            }

            // Only what is genuinely visible right now: stars, deep sky and
            // the Milky Way appear after dark. The sun, moon, planets and the
            // constellation figures stay day and night — so pointing up
            // always shows where everything is, without pretending you can
            // see stars at noon.
            bool skyDark = sunAlt < -6;

            // ---- horizon line with compass letters ----
            var horizon = new PathF();
            bool started = false;
            for (double az = CenterAz - FovDeg; az <= CenterAz + FovDeg; az += 3)
            {
                var (hx, hy, vis) = SkyMap.Project(0, (az + 360) % 360, v);
                if (!vis) { started = false; continue; }
                if (!started) { horizon.MoveTo(hx, hy); started = true; }
                else horizon.LineTo(hx, hy);
            }
            canvas.StrokeColor = lineC.WithAlpha(0.55f);
            canvas.StrokeSize = 1.6f;
            canvas.DrawPath(horizon);

            // ---- constellation figures with glow lines ----
                foreach (var f in SkyMap.Figures)
                {
                    var pts = new (float x, float y, bool ok)[f.Stars.Length];
                    for (int i = 0; i < f.Stars.Length; i++)
                    {
                        var (alt, az) = SkyCalc.AltAz(f.Stars[i].ra * 15.0, f.Stars[i].dec, Lat, Lon, utc);
                        var (x, y, vis) = SkyMap.Project(alt, az, v);
                        pts[i] = (x, y, vis && alt > -2);
                    }
                    bool any = false;
                    foreach (var (a, b) in f.Lines)
                        if (pts[a].ok && pts[b].ok)
                        {
                            canvas.StrokeColor = lineC.WithAlpha(0.26f); canvas.StrokeSize = 4.5f;   // glow
                            canvas.DrawLine(pts[a].x, pts[a].y, pts[b].x, pts[b].y);
                            canvas.StrokeColor = lineC.WithAlpha(0.92f); canvas.StrokeSize = 1.4f;   // core
                            canvas.DrawLine(pts[a].x, pts[a].y, pts[b].x, pts[b].y);
                            any = true;
                        }
                    if (any)
                    {
                        var anchor = pts.FirstOrDefault(p => p.ok);
                        canvas.FontColor = labelC;
                        canvas.FontSize = 13;
                        canvas.DrawString(f.Name, anchor.x + 10, anchor.y - 10, HorizontalAlignment.Left);
                    }
                }

            // ---- the bright named stars — only in "view all" ----
            if (ShowAll)
            {
                var stars = SkyCatalog.Stars;
                var sv = SkyMap.StarVectors;
                for (int i = 0; i < stars.Length; i++)
                {
                    var s = stars[i];
                    if (s.Mag > BrightStarMag) break;   // catalogue is sorted brightest-first
                    if (s.Name.Length == 0) continue;
                    var (n, e, u) = frame.Horizon(sv[i * 3], sv[i * 3 + 1], sv[i * 3 + 2]);
                    if (u < -0.03) continue;   // only real, above-horizon stars
                    var (x, y, vis) = SkyMap.ProjectVector(n, e, u, v);
                    if (!vis) continue;

                    float r = (float)Math.Max(1.6, 5.6 - s.Mag * 1.35);
                    var core = s.Tint();
                    canvas.FillColor = core.WithAlpha(0.20f);
                    canvas.FillCircle(x, y, r * 2.5f);
                    canvas.FillColor = core;
                    canvas.FillCircle(x, y, r);
                    canvas.FontColor = labelC;
                    canvas.FontSize = 12;
                    canvas.DrawString(s.Name, x + 8, y + 4, HorizontalAlignment.Left);
                }
            }

            // ---- deep sky showpieces — only in "view all" ----
            if (ShowAll)
            {
                var dsos = SkyDeepSkyCatalog.All;
                for (int i = 0; i < dsos.Length; i++)
                {
                    var d = dsos[i];
                    if (d.Mag > DsoShowMag) continue;
                    var (alt, az) = SkyCalc.AltAz(d.RaHours * 15.0, d.DecDeg, Lat, Lon, utc);
                    if (alt < 1) continue;   // only what is genuinely up
                    var (x, y, vis) = SkyMap.Project(alt, az, v);
                    if (!vis) continue;
                    var dia = new PathF();
                    dia.MoveTo(x, y - 5); dia.LineTo(x + 5, y); dia.LineTo(x, y + 5); dia.LineTo(x - 5, y); dia.Close();
                    canvas.StrokeColor = Color.FromArgb("#9AE8C8").WithAlpha(0.85f);
                    canvas.StrokeSize = 1.3f;
                    canvas.DrawPath(dia);
                    canvas.FontColor = Color.FromArgb("#9AE8C8").WithAlpha(0.9f);
                    canvas.FontSize = 11;
                    canvas.DrawString(d.Name.Split(" (")[0], x + 8, y + 4, HorizontalAlignment.Left);
                }
            }

            // ---- every remaining constellation, named at its heart (view-all) ----
            if (ShowAll)
            {
                foreach (var c in SkyData.All)
                {
                    bool hasFigure = false;
                    foreach (var f in SkyMap.Figures)
                        if (f.Name == c.Name) { hasFigure = true; break; }
                    if (hasFigure) continue;
                    var (alt, az) = SkyCalc.AltAz(c.RaHours * 15.0, c.DecDeg, Lat, Lon, utc);
                    if (alt < 0) continue;   // label only constellations that are up
                    var (x, y, vis) = SkyMap.Project(alt, az, v);
                    if (!vis) continue;
                    canvas.FontColor = labelC.WithAlpha(0.55f);
                    canvas.FontSize = 12;
                    canvas.DrawString(c.Name, x, y, HorizontalAlignment.Center);
                }
            }

            // ---- planets with their signature looks ----
            foreach (var b in Enum.GetValues<SkyCalc.Body>())
            {
                var (ra, dec, _) = SkyCalc.PlanetRaDec(b, jd);
                var (alt, az) = SkyCalc.AltAz(ra, dec, Lat, Lon, utc);
                var (x, y, vis) = SkyMap.Project(alt, az, v);
                if (!vis) continue;
                DrawPlanet(canvas, b, x, y, labelC);
            }

            // ---- the moon: phase-correct, with maria ----
            var (mra2, mdec2) = SkyCalc.MoonRaDec(jd);
            var (moonAlt, moonAz) = SkyCalc.AltAz(mra2, mdec2, Lat, Lon, utc);
            {
                var (mx, my, mvis) = SkyMap.Project(moonAlt, moonAz, v);
                if (mvis) DrawMoon(canvas, mx, my, 13, SkyCalc.MoonElongation(jd), labelC);
            }

            // ---- the sun ----
            {
                var (sx, sy, svis) = SkyMap.Project(sunAlt, sunAz, v);
                if (svis)
                {
                    canvas.FillColor = Color.FromArgb("#FFE9A8").WithAlpha(0.25f);
                    canvas.FillCircle(sx, sy, 34);
                    canvas.FillColor = Color.FromArgb("#FFF3C4").WithAlpha(0.5f);
                    canvas.FillCircle(sx, sy, 22);
                    canvas.FillColor = Color.FromArgb("#FFFBEA");
                    canvas.FillCircle(sx, sy, 13);
                    canvas.FontColor = labelC;
                    canvas.FontSize = 13;
                    canvas.DrawString("Sun", sx + 20, sy + 4, HorizontalAlignment.Left);
                }
            }

            // ---- compass letters along the horizon ----
            (string t, double az)[] cardinals = { ("N", 0), ("NE", 45), ("E", 90), ("SE", 135), ("S", 180), ("SW", 225), ("W", 270), ("NW", 315) };
            canvas.FontColor = Color.FromArgb("#BFD2FF");
            canvas.FontSize = 17;
            foreach (var (t, az) in cardinals)
            {
                var (x, y, vis) = SkyMap.Project(0, az, v);
                if (vis) canvas.DrawString(t, x, y - 8, HorizontalAlignment.Center);
            }

            // ---- viewfinder chrome: corner brackets + crosshair ----
            float cx = rect.Width / 2, cy = rect.Height / 2;
            canvas.StrokeColor = Colors.White.WithAlpha(0.4f);
            canvas.StrokeSize = 2.2f;
            float m = 22, len = 26;
            canvas.DrawLine(m, m + len, m, m); canvas.DrawLine(m, m, m + len, m);
            canvas.DrawLine(rect.Width - m - len, m, rect.Width - m, m); canvas.DrawLine(rect.Width - m, m, rect.Width - m, m + len);
            canvas.DrawLine(m, rect.Height - m - len, m, rect.Height - m); canvas.DrawLine(m, rect.Height - m, m + len, rect.Height - m);
            canvas.DrawLine(rect.Width - m - len, rect.Height - m, rect.Width - m, rect.Height - m); canvas.DrawLine(rect.Width - m, rect.Height - m, rect.Width - m, rect.Height - m - len);

            canvas.StrokeColor = Colors.White.WithAlpha(0.5f);
            canvas.StrokeSize = 1.4f;
            canvas.DrawLine(cx - 14, cy, cx - 5, cy);
            canvas.DrawLine(cx + 5, cy, cx + 14, cy);
            canvas.DrawLine(cx, cy - 14, cx, cy - 5);
            canvas.DrawLine(cx, cy + 5, cx, cy + 14);
            canvas.DrawCircle(cx, cy, 22);
        }

        private static void DrawPlanet(ICanvas canvas, SkyCalc.Body b, float x, float y, Color labelC)
        {
            (Color colour, float r) = b switch
            {
                SkyCalc.Body.Mercury => (Color.FromArgb("#C8BFB2"), 4.5f),
                SkyCalc.Body.Venus => (Color.FromArgb("#F5EDD6"), 7f),
                SkyCalc.Body.Mars => (Color.FromArgb("#E8845A"), 5.5f),
                SkyCalc.Body.Jupiter => (Color.FromArgb("#E8C9A0"), 8f),
                SkyCalc.Body.Uranus => (Color.FromArgb("#BFE8E4"), 5f),
                SkyCalc.Body.Neptune => (Color.FromArgb("#8FB4F0"), 5f),
                _ => (Color.FromArgb("#EFD9A7"), 6.5f),   // Saturn
            };

            canvas.FillColor = colour.WithAlpha(0.22f);
            canvas.FillCircle(x, y, r * 2.2f);
            canvas.FillColor = colour;
            canvas.FillCircle(x, y, r);

            if (b == SkyCalc.Body.Jupiter)
            {
                // two faint cloud belts
                canvas.StrokeColor = Color.FromArgb("#B98A5E").WithAlpha(0.8f);
                canvas.StrokeSize = 1.4f;
                canvas.DrawLine(x - r * 0.8f, y - r * 0.35f, x + r * 0.8f, y - r * 0.35f);
                canvas.DrawLine(x - r * 0.85f, y + r * 0.3f, x + r * 0.85f, y + r * 0.3f);
            }
            else if (b == SkyCalc.Body.Saturn)
            {
                // the rings, tilted the way everyone draws them
                canvas.SaveState();
                canvas.Rotate(-18, x, y);
                canvas.StrokeColor = Color.FromArgb("#D9C08A");
                canvas.StrokeSize = 1.6f;
                canvas.DrawEllipse(x - r * 2f, y - r * 0.62f, r * 4f, r * 1.24f);
                canvas.RestoreState();
            }
            else if (b == SkyCalc.Body.Mars)
            {
                // polar cap hint
                canvas.FillColor = Colors.White.WithAlpha(0.75f);
                canvas.FillCircle(x, y - r * 0.55f, r * 0.3f);
            }

            canvas.FontColor = labelC;
            canvas.FontSize = 13;
            canvas.DrawString(b.ToString(), x + r + 6, y + 4, HorizontalAlignment.Left);
        }

        // The moon with its true phase and a hint of the familiar maria.
        private static void DrawMoon(ICanvas canvas, float cx, float cy, float r, double elongationDeg, Color labelC)
        {
            canvas.FillColor = Color.FromArgb("#F2ECD8").WithAlpha(0.18f);
            canvas.FillCircle(cx, cy, r * 1.9f);

            // dark side (earthshine grey)
            canvas.FillColor = Color.FromArgb("#3A3F52");
            canvas.FillCircle(cx, cy, r);

            double e = elongationDeg * Math.PI / 180.0;
            double illum = (1 - Math.Cos(e)) / 2.0;
            int side = elongationDeg < 180 ? 1 : -1;      // waxing lights the evening-sky edge

            if (illum > 0.005)
            {
                var path = new PathF();
                const int N = 40;
                for (int i = 0; i <= N; i++)
                {
                    double th = -Math.PI / 2 + Math.PI * i / N;
                    float x = cx + side * r * (float)Math.Cos(th);
                    float y = cy + r * (float)Math.Sin(th);
                    if (i == 0) path.MoveTo(x, y); else path.LineTo(x, y);
                }
                float termHalf = (float)(r * -Math.Cos(e));  // signed: bulges with the gibbous phases
                for (int i = N; i >= 0; i--)
                {
                    double th = -Math.PI / 2 + Math.PI * i / N;
                    float x = cx + side * termHalf * (float)Math.Cos(th);
                    float y = cy + r * (float)Math.Sin(th);
                    path.LineTo(x, y);
                }
                path.Close();
                canvas.FillColor = Color.FromArgb("#F2ECD8");
                canvas.FillPath(path);

                // maria: the familiar grey seas, only on the lit part
                canvas.FillColor = Color.FromArgb("#C9C2AC").WithAlpha(0.8f);
                if (illum > 0.45)
                {
                    canvas.FillCircle(cx + side * r * 0.30f, cy - r * 0.32f, r * 0.26f);  // Serenitatis
                    canvas.FillCircle(cx + side * r * 0.12f, cy + r * 0.10f, r * 0.20f);  // Tranquillitatis
                }
                if (illum > 0.75)
                    canvas.FillCircle(cx - side * r * 0.32f, cy - r * 0.05f, r * 0.30f);  // Imbrium/Oceanus
            }

            canvas.FontColor = labelC;
            canvas.FontSize = 13;
            canvas.DrawString("Moon", cx + r + 7, cy + 4, HorizontalAlignment.Left);
        }

    }

    // The little compass rose: a ring with tick marks and a needle that keeps
    // pointing at true north while you turn.
    public class CompassRoseDrawable : IDrawable
    {
        public double HeadingDeg;

        public void Draw(ICanvas canvas, RectF rect)
        {
            float cx = rect.Width / 2, cy = rect.Height / 2;
            float r = Math.Min(cx, cy) - 3;
            canvas.Antialias = true;

            Color ring = Color.FromArgb("#C9D3EA");
            Color text = Colors.White;

            canvas.FillColor = Color.FromArgb("#8A0E1526");
            canvas.FillCircle(cx, cy, r);
            canvas.StrokeColor = ring;
            canvas.StrokeSize = 1.6f;
            canvas.DrawCircle(cx, cy, r);

            for (int a = 0; a < 360; a += 45)
            {
                double rad = (a - HeadingDeg) * Math.PI / 180.0;
                float x1 = cx + (float)(Math.Sin(rad) * (r - 5)), y1 = cy - (float)(Math.Cos(rad) * (r - 5));
                float x2 = cx + (float)(Math.Sin(rad) * (r - 10)), y2 = cy - (float)(Math.Cos(rad) * (r - 10));
                canvas.StrokeColor = ring.WithAlpha(0.8f);
                canvas.DrawLine(x1, y1, x2, y2);
            }

            // needle: red half to north, pale half to south
            double n = -HeadingDeg * Math.PI / 180.0;
            float nx = cx + (float)(Math.Sin(n) * (r - 14)), ny = cy - (float)(Math.Cos(n) * (r - 14));
            float sx = cx - (float)(Math.Sin(n) * (r - 18)), sy = cy + (float)(Math.Cos(n) * (r - 18));
            canvas.StrokeSize = 3f;
            canvas.StrokeColor = Color.FromArgb("#E5484D");
            canvas.DrawLine(cx, cy, nx, ny);
            canvas.StrokeColor = text.WithAlpha(0.6f);
            canvas.DrawLine(cx, cy, sx, sy);

            var nRad = -HeadingDeg * Math.PI / 180.0;
            canvas.FontColor = text;
            canvas.FontSize = 12;
            canvas.DrawString("N", cx + (float)(Math.Sin(nRad) * (r + 0)), cy - (float)(Math.Cos(nRad) * (r + 0)) + 4, HorizontalAlignment.Center);
        }
    }
}
