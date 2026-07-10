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
    // Scan Sky — hold your phone up and the live camera view fills the screen
    // with the whole naked-eye sky drawn over it exactly where it really is:
    // 1,700 catalogue stars, the Milky Way band, constellation figures, the
    // full Messier + Caldwell deep-sky catalogues, planets with their real
    // looks, a phase-correct moon, and shooting stars that follow tonight's
    // active meteor shower. A target card at the top names whatever's under
    // the crosshair; a time slider scrubs the sky up to 12 hours either way.
    // No camera? A painted twilight-aware sky stands in.
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
        private Label _hint = null!, _timeLabel = null!;
        private Slider _timeSlider = null!;
        private GraphicsView _compass = null!;
        private readonly CompassRoseDrawable _rose = new();
        private SpaceObject? _learnTarget;

        private double _az = 180, _alt = 30;
        private double _timeOffsetHours;
        private bool _sensorMode, _cameraOn;
        private bool _starting;
        private int _tick;
        private IDispatcherTimer? _timer;
        private CancellationTokenSource? _camCts;

        private DateTime SkyUtc => DateTime.UtcNow.AddHours(_timeOffsetHours);

        public SkyViewPage()
        {
            var where = SkyService.Cached;
            _lat = where.Lat; _lon = where.Lon;
            _drawable = new SkyViewDrawable { Lat = _lat, Lon = _lon };
            // Always the full dark-site sky: every catalogue star, the Milky
            // Way, all the deep-sky objects. The old city/suburb/dark toggle
            // only ever hid stars and confused people.
            _drawable.LimitMag = 5.6f;
            _drawable.DsoLimitMag = 8.2f;
            _drawable.MilkyWayStrength = 0.85f;
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
            var close = ChromeButton(Ui.Icon(Ui.IconClose, 22, Colors.White));
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
                Text = "Ask NovaSaur", FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), FontAttributes = FontAttributes.Bold,
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
            Ui.Describe(_askBtn, "Ask NovaSaur about this object");

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

            // ----- time-travel slider (bottom-centre) -----
            _timeLabel = new Label
            {
                Text = "Now", FontFamily = Ui.Fonts, FontSize = Ui.S(12), FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White, VerticalOptions = LayoutOptions.Center, WidthRequest = 58,
                HorizontalTextAlignment = TextAlignment.Center
            };
            _timeSlider = new Slider
            {
                Minimum = -12, Maximum = 12, Value = 0, WidthRequest = 190,
                MinimumTrackColor = Color.FromArgb("#E7BC4F"), MaximumTrackColor = Color.FromArgb("#4A4636"),
                ThumbColor = Colors.White, VerticalOptions = LayoutOptions.Center
            };
            _timeSlider.ValueChanged += (_, e) =>
            {
                // snap to half hours so the label reads cleanly
                double v = Math.Round(e.NewValue * 2) / 2.0;
                _timeOffsetHours = v;
                _drawable.TimeOffsetHours = v;
                _timeLabel.Text = v == 0 ? "Now" : $"{(v > 0 ? "+" : "")}{v:0.#} h";
            };
            Ui.Describe(_timeSlider, "Time travel: scrub the sky up to 12 hours forward or back");
            var timeRow = new HorizontalStackLayout
            {
                Spacing = 4,
                Children = { _timeSlider, _timeLabel }
            };
            var timeCard = new Border
            {
                Content = timeRow,
                BackgroundColor = Color.FromArgb("#8A161A10"),
                Stroke = Color.FromArgb("#44A89B6E"), StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                Padding = new Thickness(12, 2),
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 0, 0, 34)
            };

            // ----- bottom hint -----
            _hint = new Label
            {
                Text = "…",
                FontFamily = Ui.Fonts, FontSize = Ui.S(12.5),
                TextColor = Color.FromArgb("#C9C8B4"),
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(60, 0, 60, 8),
                HorizontalTextAlignment = TextAlignment.Center
            };

            _root = new Grid { BackgroundColor = Color.FromArgb("#070B14") };
            // camera slot is index 0 (inserted on demand); overlay stack above it
            _root.Add(_view);
            _root.Add(topBar);
            _root.Add(_targetCard);
            _root.Add(_compass);
            _root.Add(timeCard);
            _root.Add(_hint);
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

                    // A drifting or offset overlay is nearly always a compass
                    // that wants calibrating — tell the user how to fix it.
                    // Until a real location fix lands, say that instead of
                    // confidently naming the wrong sky.
                    if (_tick % 20 == 0 && _sensorMode)
                        _hint.Text = !_locationReal
                            ? "Finding your location — names sharpen in a moment"
                            : _pointing?.NeedsCalibration == true
                            ? "Compass needs calibrating — wave your phone in a big figure-8"
                            : "Point your phone at the sky — names appear as you aim";
                };
            }
            _timer.Start();

            await StartCameraAsync();
            _hint.Text = (_cameraOn, _sensorMode) switch
            {
                (true, true) => "Point your phone at the sky — names appear as you aim",
                (false, true) => "Move your phone to explore (camera off — overlay only)",
                _ => "Drag to look around the sky",
            };
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

        private static double[]? _dsoVec;
        private static double[] DsoVectors
        {
            get
            {
                if (_dsoVec == null)
                {
                    var all = SkyDeepSkyCatalog.All;
                    var v = new double[all.Length * 3];
                    for (int i = 0; i < all.Length; i++)
                    {
                        var (x, y, z) = SkyMap.UnitVectorOf(all[i].RaHours * 15.0, all[i].DecDeg);
                        v[i * 3] = x; v[i * 3 + 1] = y; v[i * 3 + 2] = z;
                    }
                    _dsoVec = v;
                }
                return _dsoVec;
            }
        }

        private void UpdateTarget()
        {
            var utc = SkyUtc;
            double jd = SkyCalc.JulianDay(utc);
            var frame = new SkyMap.LocalFrame(_lat, _lon, utc);

            // the pointing direction as a horizon-frame unit vector
            double altR = _alt * Math.PI / 180.0, azR = _az * Math.PI / 180.0;
            double pn = Math.Cos(altR) * Math.Cos(azR), pe = Math.Cos(altR) * Math.Sin(azR), pu = Math.Sin(altR);

            double SepTo(double n, double e, double u)
                => Math.Acos(Math.Clamp(n * pn + e * pe + u * pu, -1, 1)) * 180.0 / Math.PI;

            string? name = null, kind = null, blurb = null;
            SpaceObject? entry = null;

            // Candidates compete on angular distance minus a priority bonus:
            // the moon and planets are what people actually point phones at,
            // so they win unless something else is clearly closer. Anonymous
            // faint stars used to claim an 8° radius each, which made half
            // the sky read "A distant sun" — now they only speak up when the
            // crosshair is basically on top of one.
            double bestScore = double.MaxValue;
            void Consider(double sep, double maxSep, double bonus, string n, string k, string? b, SpaceObject? e)
            {
                if (sep > maxSep) return;
                double score = sep - bonus;
                if (score >= bestScore) return;
                bestScore = score;
                name = n; kind = k; blurb = b; entry = e;
            }

            // the moon — by far the most prominent thing in the night sky, and
            // the thing people actually point phones at: give it a wide net so
            // ordinary compass slop still names it correctly
            var (mra, mdec) = SkyCalc.MoonRaDec(jd);
            var (mAlt, mAz) = SkyCalc.AltAz(mra, mdec, _lat, _lon, utc);
            if (mAlt > -5)
            {
                var e = SpaceData.ByName("Moon");
                Consider(SkyMap.Separation(mAlt, mAz, _alt, _az), 10, 4.0,
                         "Moon", "Earth's moon", e?.ShortDescription, e);
            }

            // planets
            foreach (var b in Enum.GetValues<SkyCalc.Body>())
            {
                var (ra, dec, _) = SkyCalc.PlanetRaDec(b, jd);
                var (alt, az) = SkyCalc.AltAz(ra, dec, _lat, _lon, utc);
                if (alt <= -5) continue;
                var e = SpaceData.ByName(b.ToString());
                Consider(SkyMap.Separation(alt, az, _alt, _az), 6, 1.5,
                         b.ToString(), "Planet", e?.ShortDescription, e);
            }

            // the sun (matters when the time slider drags the view into day)
            var (sra, sdec) = SkyCalc.SunRaDec(jd);
            var (sAlt, sAz) = SkyCalc.AltAz(sra, sdec, _lat, _lon, utc);
            if (sAlt > -2)
            {
                var e = SpaceData.ByName("Sun");
                Consider(SkyMap.Separation(sAlt, sAz, _alt, _az), 8, 3.0,
                         "Sun", "Our star", e?.ShortDescription, e);
            }

            // catalogue stars: named ones within 3°, anonymous only when the
            // crosshair is right on them (1.2°) and they're reasonably bright
            var stars = SkyCatalog.Stars;
            var sv = SkyMap.StarVectors;
            for (int i = 0; i < stars.Length; i++)
            {
                var s = stars[i];
                if (s.Mag > _drawable.LimitMag) break;      // sorted by brightness
                bool named = s.Name.Length > 0;
                if (!named && s.Mag > 4.0) continue;
                var (n, e, u) = frame.Horizon(sv[i * 3], sv[i * 3 + 1], sv[i * 3 + 2]);
                if (u < -0.09) continue;
                double sep = SepTo(n, e, u);
                Consider(sep, named ? 3.0 : 1.2, named ? -s.Mag * 0.15 : -0.6,
                         named ? s.Name : "A distant sun",
                         $"Star · {s.Colour()}",
                         named
                             ? $"Magnitude {s.Mag:0.0#} — one of the stars bright enough to carry a name."
                             : $"Magnitude {s.Mag:0.0#} — a sun many light-years away.",
                         null);
            }

            // deep sky: the full Messier + Caldwell catalogues
            var dsos = SkyDeepSkyCatalog.All;
            var dv = DsoVectors;
            for (int i = 0; i < dsos.Length; i++)
            {
                var d = dsos[i];
                if (d.Mag > _drawable.DsoLimitMag && d.Mag < 90) continue;
                if (d.Mag >= 90 && _drawable.DsoLimitMag < 7) continue;   // dark nebulae need dark skies
                var (n, e, u) = frame.Horizon(dv[i * 3], dv[i * 3 + 1], dv[i * 3 + 2]);
                if (u < 0.03) continue;
                Consider(SepTo(n, e, u), 2.5, -0.3,
                         d.Name, d.Kind, d.Blurb, SpaceData.ByName(d.Name.Split(" (")[0]));
            }

            // fall back to the constellation region (all 88, not just figures).
            // Kept tight: naming a constellation 25° away is how the moon got
            // called "Aries" — better to say nothing than the wrong thing.
            if (name == null)
            {
                Constellation? nearest = null; double bestSep = 15;
                foreach (var c in SkyData.All)
                {
                    var (alt, az) = SkyCalc.AltAz(c.RaHours * 15.0, c.DecDeg, _lat, _lon, utc);
                    if (alt < -10) continue;
                    double sep = SkyMap.Separation(alt, az, _alt, _az);
                    if (sep < bestSep) { bestSep = sep; nearest = c; }
                }
                if (nearest != null)
                {
                    name = nearest.Name; kind = "Constellation"; blurb = nearest.Blurb + ".";
                    entry = nearest.LinkEntry is string link ? SpaceData.ByName(link) : null;
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

    // One frame of the pointed-at sky: the Milky Way, 1,700 catalogue stars
    // with real colours and twinkle, constellation figures, the Messier and
    // Caldwell deep sky, planets drawn with their signature looks, a
    // phase-correct textured moon, the sun, shooting stars, a horizon line
    // and compass letters. Transparent when the camera shows behind; a
    // twilight-aware painted sky otherwise.
    public class SkyViewDrawable : IDrawable
    {
        public double Lat, Lon;
        public double CenterAz = 180, CenterAlt = 30;
        public double FovDeg = 95;
        public double TimeOffsetHours;
        public bool CameraBehind;
        public float LimitMag = 5.05f;
        public float DsoLimitMag = 6.6f;
        public float MilkyWayStrength = 0.4f;

        private readonly Random _rng = new();
        private readonly List<(long bornMs, float x, float y, float dx, float dy)> _meteors = new();
        private long _nextMeteorMs;

        public void Draw(ICanvas canvas, RectF rect)
        {
            var utc = DateTime.UtcNow.AddHours(TimeOffsetHours);
            var v = new SkyMap.View(Lat, Lon, utc, CenterAz, CenterAlt, FovDeg, Math.Max(rect.Width, rect.Height));
            var frame = new SkyMap.LocalFrame(Lat, Lon, utc);
            long nowMs = Environment.TickCount64;
            canvas.Antialias = true;

            Color lineC = Colors.White;
            Color labelC = Color.FromArgb("#EEF1F8");
            Color dsoC = Color.FromArgb("#9AE8C8");

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

            // The overlay ALWAYS draws the full sky — stars, figures, deep
            // sky, the lot. Hiding them in daylight made the page look broken
            // and empty; pointing the phone up should always show what's
            // there, day or night. Only meteors keep the darkness rule.
            bool skyDark = sunAlt < -6;

            // ---- the Milky Way: a soft band of overlapping glows ----
            if (MilkyWayStrength > 0.01f)
            {
                var band = SkyMap.MilkyWayBand;
                float glowScale = (float)(v.SizePx / v.MaxR) / 2f;
                foreach (var p in band)
                {
                    var (n, e, u) = frame.Horizon(p.X, p.Y, p.Z);
                    if (u < -0.05) continue;
                    var (x, y, vis) = SkyMap.ProjectVector(n, e, u, v);
                    if (!vis) continue;
                    float r = p.WidthDeg * (float)Math.PI / 180f * glowScale;
                    canvas.FillColor = Color.FromArgb("#D9E4FF").WithAlpha(0.055f * p.Brightness * MilkyWayStrength / 0.4f * 0.4f + 0.028f * p.Brightness * MilkyWayStrength);
                    canvas.FillCircle(x, y, r);
                }
            }

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
                        pts[i] = (x, y, vis && alt > -8);
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

            // ---- the whole catalogue: soft halo + core, colour by temperature ----
            {
                var stars = SkyCatalog.Stars;
                var sv = SkyMap.StarVectors;
                float t = nowMs % 100000 / 1000f;
                for (int i = 0; i < stars.Length; i++)
                {
                    var s = stars[i];
                    if (s.Mag > LimitMag) break;   // catalogue is sorted brightest-first
                    var (n, e, u) = frame.Horizon(sv[i * 3], sv[i * 3 + 1], sv[i * 3 + 2]);
                    if (u < -0.05) continue;
                    var (x, y, vis) = SkyMap.ProjectVector(n, e, u, v);
                    if (!vis) continue;

                    float r = (float)Math.Max(0.7, 5.6 - s.Mag * 1.35);
                    var core = s.Tint();
                    float alpha = s.Mag < 2 ? 1f : Math.Max(0.35f, 1f - (s.Mag - 2f) * 0.16f);
                    // the brightest stars twinkle very slightly
                    if (s.Mag < 1.5) alpha *= 0.88f + 0.12f * (float)Math.Sin(t * 5 + i * 1.7);

                    if (r > 1.6f)
                    {
                        canvas.FillColor = core.WithAlpha(0.20f * alpha);
                        canvas.FillCircle(x, y, r * 2.5f);
                    }
                    canvas.FillColor = core.WithAlpha(alpha);
                    canvas.FillCircle(x, y, r);
                    if (s.Name.Length > 0 && s.Mag < 1.6)
                    {
                        canvas.FontColor = labelC;
                        canvas.FontSize = 12;
                        canvas.DrawString(s.Name, x + 8, y + 4, HorizontalAlignment.Left);
                    }
                }
            }

            // ---- deep sky: the full Messier + Caldwell catalogues ----
            {
                var dsos = SkyDeepSkyCatalog.All;
                for (int i = 0; i < dsos.Length; i++)
                {
                    var d = dsos[i];
                    if (d.Mag > DsoLimitMag) continue;         // dark nebulae (mag 99) only at dark sites via card
                    var (alt, az) = SkyCalc.AltAz(d.RaHours * 15.0, d.DecDeg, Lat, Lon, utc);
                    if (alt < 3) continue;
                    var (x, y, vis) = SkyMap.Project(alt, az, v);
                    if (!vis) continue;
                    var dia = new PathF();
                    dia.MoveTo(x, y - 5); dia.LineTo(x + 5, y); dia.LineTo(x, y + 5); dia.LineTo(x - 5, y); dia.Close();
                    canvas.StrokeColor = dsoC.WithAlpha(0.85f); canvas.StrokeSize = 1.3f;
                    canvas.DrawPath(dia);
                    if (d.Mag < 5.5)
                    {
                        canvas.FontColor = dsoC.WithAlpha(0.9f);
                        canvas.FontSize = 11;
                        canvas.DrawString(d.Name.Split(" (")[0], x + 8, y + 4, HorizontalAlignment.Left);
                    }
                }
            }

            // ---- planets with their signature looks ----
            foreach (var b in Enum.GetValues<SkyCalc.Body>())
            {
                var (ra, dec, _) = SkyCalc.PlanetRaDec(b, jd);
                var (alt, az) = SkyCalc.AltAz(ra, dec, Lat, Lon, utc);
                if (alt < -5) continue;
                var (x, y, vis) = SkyMap.Project(alt, az, v);
                if (!vis) continue;
                DrawPlanet(canvas, b, x, y, labelC);
            }

            // ---- the moon: phase-correct, with maria ----
            var (mra2, mdec2) = SkyCalc.MoonRaDec(jd);
            var (moonAlt, moonAz) = SkyCalc.AltAz(mra2, mdec2, Lat, Lon, utc);
            if (moonAlt > -5)
            {
                var (mx, my, mvis) = SkyMap.Project(moonAlt, moonAz, v);
                if (mvis) DrawMoon(canvas, mx, my, 13, SkyCalc.MoonElongation(jd), labelC);
            }

            // ---- the sun (time travel can bring it into view) ----
            if (sunAlt > -3)
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

            // ---- shooting stars (radiant-aware during active showers) ----
            if (skyDark) DrawMeteors(canvas, rect, v, utc, nowMs);

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

        // Shooting stars: sporadics on a quiet night; during an active shower
        // they stream away from the real radiant, just like the real thing.
        private void DrawMeteors(ICanvas canvas, RectF rect, SkyMap.View v, DateTime utc, long nowMs)
        {
            if (_nextMeteorMs == 0) _nextMeteorMs = nowMs + 3000;
            var shower = MeteorShowers.ActiveOn(utc).OrderByDescending(s => s.Zhr).FirstOrDefault();

            if (nowMs >= _nextMeteorMs && _meteors.Count < 3)
            {
                float x0 = rect.Width * (0.15f + 0.7f * (float)_rng.NextDouble());
                float y0 = rect.Height * (0.12f + 0.6f * (float)_rng.NextDouble());
                double ang;
                if (shower != null)
                {
                    var (rAlt, rAz) = SkyCalc.AltAz(shower.RadiantRaHours * 15.0, shower.RadiantDecDeg, Lat, Lon, utc);
                    var (rx, ry, _) = SkyMap.Project(Math.Max(rAlt, -10), rAz, v);
                    ang = Math.Atan2(y0 - ry, x0 - rx);          // away from the radiant
                }
                else ang = _rng.NextDouble() * Math.PI * 2;
                _meteors.Add((nowMs, x0, y0, (float)Math.Cos(ang), (float)Math.Sin(ang)));
                // showers spit more often than quiet skies
                int gap = shower != null ? 2500 + _rng.Next(5000) : 6000 + _rng.Next(12000);
                _nextMeteorMs = nowMs + gap;
            }

            for (int i = _meteors.Count - 1; i >= 0; i--)
            {
                var mtr = _meteors[i];
                float age = (nowMs - mtr.bornMs) / 650f;
                if (age >= 1f) { _meteors.RemoveAt(i); continue; }
                float speed = 260;
                float hx = mtr.x + mtr.dx * speed * age;
                float hy = mtr.y + mtr.dy * speed * age;
                float fade = 1f - age;
                // three-segment tail, brightest at the head
                for (int s = 0; s < 3; s++)
                {
                    float t0 = Math.Max(0, age - 0.09f * (s + 1)), t1 = Math.Max(0, age - 0.09f * s);
                    canvas.StrokeColor = Colors.White.WithAlpha(fade * (0.85f - s * 0.28f));
                    canvas.StrokeSize = 2.2f - s * 0.6f;
                    canvas.DrawLine(mtr.x + mtr.dx * speed * t0, mtr.y + mtr.dy * speed * t0,
                                    mtr.x + mtr.dx * speed * t1, mtr.y + mtr.dy * speed * t1);
                }
                canvas.FillColor = Colors.White.WithAlpha(fade);
                canvas.FillCircle(hx, hy, 1.8f);
            }
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
