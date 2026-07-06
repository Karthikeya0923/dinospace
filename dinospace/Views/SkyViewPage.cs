using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Scan Sky — hold your phone up and the live camera view fills the screen
    // with stars, constellation figures, planets and the moon drawn over it,
    // exactly where they really are. A target card names whatever's under the
    // crosshair (with a Learn More link into the encyclopedia), a compass rose
    // shows your heading, and a red night-vision mode protects dark-adapted
    // eyes. No camera? No problem — a rendered night sky stands in.
    public class SkyViewPage : ContentPage
    {
        private readonly double _lat, _lon;
        private readonly SkyViewDrawable _drawable;
        private GraphicsView _view = null!;
        private CameraView? _camera;
        private Grid _root = null!;

        // chrome
        private Label _targetName = null!, _targetKind = null!, _targetBlurb = null!;
        private Border _targetCard = null!, _learnBtn = null!;
        private Label _hint = null!;
        private GraphicsView _compass = null!;
        private readonly CompassRoseDrawable _rose = new();
        private SpaceObject? _learnTarget;

        private double _az = 180, _alt = 30;
        private bool _sensorMode, _cameraOn, _night;
        private bool _dirty = true;
        private IDispatcherTimer? _timer;
        private CancellationTokenSource? _camCts;

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
                    _dirty = true;
                }
            };
            _view.GestureRecognizers.Add(pan);

            // ----- top bar: close · title · night toggle -----
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
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
            };

            var night = ChromeButton(new Label { Text = "☾", FontSize = 20, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center });
            Ui.OnTap(night, (_, _) =>
            {
                _night = !_night;
                _drawable.NightMode = _night;
                _rose.NightMode = _night;
                _hint.TextColor = _night ? Color.FromArgb("#C2503C") : Color.FromArgb("#B9BDD1");
                _dirty = true;
            });
            Ui.Describe(night, "Toggle red night-vision mode");

            var topBar = new Grid { Padding = new Thickness(14, 14, 14, 0), ColumnSpacing = 8, VerticalOptions = LayoutOptions.Start };
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topBar.Add(close, 0, 0);
            topBar.Add(title, 1, 0);
            topBar.Add(night, 2, 0);

            // ----- target card (top-right, under the bar) -----
            _targetName = new Label { FontFamily = Ui.Display, FontSize = Ui.S(19), TextColor = Colors.White };
            _targetKind = new Label { FontFamily = Ui.Fonts, FontSize = Ui.S(11.5), TextColor = Color.FromArgb("#B9A9E8") };
            _targetBlurb = new Label { FontFamily = Ui.Fonts, FontSize = Ui.S(12), LineHeight = 1.35, TextColor = Color.FromArgb("#D5DAE8"), MaxLines = 3, LineBreakMode = LineBreakMode.TailTruncation };
            var learnLabel = new Label
            {
                Text = "Learn more", FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#C9B8F0"), HorizontalTextAlignment = TextAlignment.Center
            };
            _learnBtn = new Border
            {
                Content = learnLabel,
                BackgroundColor = Color.FromArgb("#33FFFFFF"), Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = new Thickness(12, 7),
                Margin = new Thickness(0, 6, 0, 0), IsVisible = false
            };
            Ui.OnTap(_learnBtn, async (_, _) => { if (_learnTarget != null) await Nav.OpenSpace(_learnTarget); });

            var targetCol = new VerticalStackLayout { Spacing = 3, Children = { _targetName, _targetKind, _targetBlurb, _learnBtn } };
            _targetCard = new Border
            {
                Content = targetCol,
                BackgroundColor = Color.FromArgb("#B3141024"),
                Stroke = Color.FromArgb("#443C5C80"), StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(14, 12),
                MaximumWidthRequest = 250,
                HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 74, 14, 0),
                IsVisible = false
            };

            // ----- moon phase card (bottom-right) -----
            var moon = SkyCalc.Moon(DateTime.UtcNow);
            var moonCol = new VerticalStackLayout { Spacing = 2 };
            moonCol.Add(new Label { Text = "Moon Phase", FontFamily = Ui.Display, FontSize = Ui.S(15), TextColor = Colors.White });
            moonCol.Add(new Label { Text = moon.PhaseName, FontFamily = Ui.Fonts, FontSize = Ui.S(11.5), TextColor = Color.FromArgb("#D5DAE8") });
            moonCol.Add(new Label { Text = $"Illumination: {moon.Illumination * 100:0}%", FontFamily = Ui.Fonts, FontSize = Ui.S(11.5), TextColor = Color.FromArgb("#B9BDD1") });
            var moonRow = new HorizontalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new GraphicsView { Drawable = new MoonPhaseDrawable { ElongationDeg = moon.ElongationDeg }, WidthRequest = 44, HeightRequest = 44, InputTransparent = true },
                    moonCol
                }
            };
            var moonCard = new Border
            {
                Content = moonRow,
                BackgroundColor = Color.FromArgb("#B3141024"),
                Stroke = Color.FromArgb("#443C5C80"), StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(12, 10),
                HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 0, 14, 86)
            };
            var moonEntry = SpaceData.ByName("Moon");
            if (moonEntry != null) Ui.OnTap(moonCard, async (_, _) => await Nav.OpenSpace(moonEntry));

            // ----- compass rose (bottom-left) -----
            _compass = new GraphicsView { Drawable = _rose, WidthRequest = 76, HeightRequest = 76, InputTransparent = true, HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.End, Margin = new Thickness(16, 0, 0, 86) };

            // ----- bottom hint -----
            _hint = new Label
            {
                Text = "…",
                FontFamily = Ui.Fonts, FontSize = Ui.S(12.5),
                TextColor = Color.FromArgb("#B9BDD1"),
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(60, 0, 60, 30),
                HorizontalTextAlignment = TextAlignment.Center
            };

            _root = new Grid { BackgroundColor = Color.FromArgb("#070B14") };
            // camera slot is index 0 (inserted on demand); overlay stack above it
            _root.Add(_view);
            _root.Add(topBar);
            _root.Add(_targetCard);
            _root.Add(moonCard);
            _root.Add(_compass);
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
            StartSensor();
            await StartCameraAsync();
            _hint.Text = (_cameraOn, _sensorMode) switch
            {
                (true, true) => "Point your phone at the sky — names appear as you aim",
                (false, true) => "Move your phone to explore (camera off — overlay only)",
                _ => "Drag to look around the sky",
            };

            _timer ??= Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(50);
            _timer.Tick += (_, _) =>
            {
                if (!_dirty) return;
                _dirty = false;
                _drawable.CenterAz = _az;
                _drawable.CenterAlt = _alt;
                _rose.HeadingDeg = _az;
                _view.Invalidate();
                _compass.Invalidate();
                UpdateTarget();
            };
            _timer.Start();
            _dirty = true;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _timer?.Stop();
            StopSensor();
            StopCamera();
        }

        // ----- camera passthrough -----

        private async System.Threading.Tasks.Task StartCameraAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted) { _drawable.CameraBehind = false; return; }

                _camera = new CameraView { InputTransparent = true };
                _root.Insert(0, _camera);
                _camCts = new CancellationTokenSource();
                await _camera.StartCameraPreview(_camCts.Token);
                _cameraOn = true;
                _drawable.CameraBehind = true;   // overlay goes transparent, no painted sky
                _dirty = true;
            }
            catch
            {
                // any camera trouble -> quietly fall back to the rendered sky
                StopCamera();
            }
        }

        private void StopCamera()
        {
            try
            {
                _camCts?.Cancel();
                if (_camera != null)
                {
                    _camera.StopCameraPreview();
                    _root.Remove(_camera);
                }
            }
            catch { }
            _camera = null;
            _cameraOn = false;
            _drawable.CameraBehind = false;
        }

        // ----- orientation sensor -----

        private void StartSensor()
        {
            try
            {
                if (!OrientationSensor.Default.IsSupported) return;
                OrientationSensor.Default.ReadingChanged += OnReading;
                OrientationSensor.Default.Start(SensorSpeed.Game);
                _sensorMode = true;
            }
            catch { _sensorMode = false; }
        }

        private void StopSensor()
        {
            try
            {
                if (!_sensorMode) return;
                OrientationSensor.Default.ReadingChanged -= OnReading;
                OrientationSensor.Default.Stop();
            }
            catch { }
            _sensorMode = false;
        }

        private void OnReading(object? sender, OrientationSensorChangedEventArgs e)
        {
            var q = e.Reading.Orientation;
            var dir = Vector3.Transform(new Vector3(0, 0, -1), q);
            double len = Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y + dir.Z * dir.Z);
            if (len < 1e-6) return;
            double alt = Math.Asin(Math.Clamp(dir.Z / len, -1, 1)) * 180 / Math.PI;
            double az = (Math.Atan2(dir.X, dir.Y) * 180 / Math.PI + 360) % 360;

            double dAz = ((az - _az + 540) % 360) - 180;
            _az = (_az + dAz * 0.25 + 360) % 360;
            _alt += (alt - _alt) * 0.25;
            _dirty = true;
        }

        // ----- what's under the crosshair -----

        private void UpdateTarget()
        {
            var utc = DateTime.UtcNow;
            double jd = SkyCalc.JulianDay(utc);

            string? name = null, kind = null, blurb = null;
            SpaceObject? entry = null;
            double best = 8;

            foreach (var b in Enum.GetValues<SkyCalc.Body>())
            {
                var (ra, dec, _) = SkyCalc.PlanetRaDec(b, jd);
                var (alt, az) = SkyCalc.AltAz(ra, dec, _lat, _lon, utc);
                double sep = SkyMap.Separation(alt, az, _alt, _az);
                if (alt > -5 && sep < best)
                {
                    best = sep; name = b.ToString(); kind = "Planet";
                    entry = SpaceData.ByName(name);
                    blurb = entry?.ShortDescription;
                }
            }
            var (mra, mdec) = SkyCalc.MoonRaDec(jd);
            var (mAlt, mAz) = SkyCalc.AltAz(mra, mdec, _lat, _lon, utc);
            if (mAlt > -5 && SkyMap.Separation(mAlt, mAz, _alt, _az) < best)
            {
                best = SkyMap.Separation(mAlt, mAz, _alt, _az);
                name = "Moon"; kind = "Earth's moon";
                entry = SpaceData.ByName("Moon"); blurb = entry?.ShortDescription;
            }
            foreach (var s in SkyMap.Stars)
            {
                var (alt, az) = SkyCalc.AltAz(s.RaHours * 15.0, s.DecDeg, _lat, _lon, utc);
                double sep = SkyMap.Separation(alt, az, _alt, _az);
                if (alt > -5 && sep < best)
                {
                    best = sep; name = s.Name; kind = $"Star · {s.Colour}";
                    blurb = $"Magnitude {s.Mag:0.0#} — one of the brightest stars in the sky."; entry = null;
                }
            }

            // fall back to the constellation region (all 35, not just figures)
            if (name == null)
            {
                Constellation? nearest = null; double bestSep = 26;
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
            _targetCard.IsVisible = true;
        }
    }

    // One frame of the pointed-at sky: glow stars, constellation figures,
    // deep-sky markers, planets, the moon, a horizon line and corner brackets.
    // Transparent when the camera shows behind; a painted night sky otherwise.
    public class SkyViewDrawable : IDrawable
    {
        public double Lat, Lon;
        public double CenterAz = 180, CenterAlt = 30;
        public double FovDeg = 95;
        public bool CameraBehind;
        public bool NightMode;

        public void Draw(ICanvas canvas, RectF rect)
        {
            var utc = DateTime.UtcNow;
            var v = new SkyMap.View(Lat, Lon, utc, CenterAz, CenterAlt, FovDeg, Math.Max(rect.Width, rect.Height));
            canvas.Antialias = true;

            Color lineC = NightMode ? Color.FromArgb("#8A2A1E") : Color.FromArgb("#7A93CF");
            Color labelC = NightMode ? Color.FromArgb("#C2503C") : Color.FromArgb("#A9B7DB");
            Color starC = NightMode ? Color.FromArgb("#FF6B52") : Colors.White;
            Color bodyC = NightMode ? Color.FromArgb("#FF7A5C") : Color.FromArgb("#FFD98C");
            Color dsoC = NightMode ? Color.FromArgb("#D45540") : Color.FromArgb("#9AE8C8");

            // painted sky only when there's no camera behind us
            if (!CameraBehind)
            {
                var paint = new LinearGradientPaint
                {
                    StartColor = NightMode ? Color.FromArgb("#120303") : Color.FromArgb("#05070F"),
                    EndColor = NightMode ? Color.FromArgb("#2A0A06") : Color.FromArgb("#16203C"),
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1)
                };
                canvas.SetFillPaint(paint, rect);
                canvas.FillRectangle(rect);

                var rng = new Random(7);
                for (int i = 0; i < 110; i++)
                {
                    canvas.FillColor = starC.WithAlpha(0.06f + (float)rng.NextDouble() * 0.28f);
                    canvas.FillCircle((float)(rng.NextDouble() * rect.Width), (float)(rng.NextDouble() * rect.Height), 0.6f + (float)rng.NextDouble() * 1.1f);
                }
            }

            // horizon line with a soft ground shade below it
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

            // constellation figures with glow lines
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
                        canvas.StrokeColor = lineC.WithAlpha(0.28f); canvas.StrokeSize = 4.5f;   // glow
                        canvas.DrawLine(pts[a].x, pts[a].y, pts[b].x, pts[b].y);
                        canvas.StrokeColor = lineC.WithAlpha(0.95f); canvas.StrokeSize = 1.5f;   // core
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

            // stars: soft halo + core, brighter = bigger
            foreach (var s in SkyMap.Stars)
            {
                var (alt, az) = SkyCalc.AltAz(s.RaHours * 15.0, s.DecDeg, Lat, Lon, utc);
                if (alt < -5) continue;
                var (x, y, vis) = SkyMap.Project(alt, az, v);
                if (!vis) continue;
                float r = (float)Math.Max(1.8, 6.5 - s.Mag * 2.0);
                Color core = NightMode ? starC : s.Colour switch
                {
                    "red" or "red-orange" => Color.FromArgb("#FFAA80"),
                    "orange" => Color.FromArgb("#FFC888"),
                    "golden" or "yellow" or "yellow-white" => Color.FromArgb("#FFE9B0"),
                    "blue" or "blue-white" => Color.FromArgb("#CFE2FF"),
                    _ => Color.FromArgb("#F2F5FA")
                };
                canvas.FillColor = core.WithAlpha(0.22f);
                canvas.FillCircle(x, y, r * 2.6f);
                canvas.FillColor = core;
                canvas.FillCircle(x, y, r);
                if (s.Mag < 1.3)
                {
                    canvas.FontColor = labelC;
                    canvas.FontSize = 12;
                    canvas.DrawString(s.Name, x + 8, y + 4, HorizontalAlignment.Left);
                }
            }

            // deep-sky highlights as little diamonds
            foreach (var d in SkyMap.DeepSky)
            {
                var (alt, az) = SkyCalc.AltAz(d.RaHours * 15.0, d.DecDeg, Lat, Lon, utc);
                if (alt < 5) continue;
                var (x, y, vis) = SkyMap.Project(alt, az, v);
                if (!vis) continue;
                var dia = new PathF();
                dia.MoveTo(x, y - 6); dia.LineTo(x + 6, y); dia.LineTo(x, y + 6); dia.LineTo(x - 6, y); dia.Close();
                canvas.StrokeColor = dsoC; canvas.StrokeSize = 1.4f;
                canvas.DrawPath(dia);
                canvas.FontColor = dsoC.WithAlpha(0.9f);
                canvas.FontSize = 11;
                canvas.DrawString(d.Name.Split(' ')[0], x + 9, y + 4, HorizontalAlignment.Left);
            }

            // planets + moon
            double jd = SkyCalc.JulianDay(utc);
            foreach (var b in Enum.GetValues<SkyCalc.Body>())
            {
                var (ra, dec, _) = SkyCalc.PlanetRaDec(b, jd);
                DrawBody(canvas, v, ra, dec, b.ToString(), 5.5f, bodyC, labelC, utc);
            }
            var (mra2, mdec2) = SkyCalc.MoonRaDec(jd);
            DrawBody(canvas, v, mra2, mdec2, "Moon", 11, NightMode ? Color.FromArgb("#FF8A70") : Color.FromArgb("#F2ECD8"), labelC, utc);

            // compass letters along the horizon
            (string t, double az)[] cardinals = { ("N", 0), ("NE", 45), ("E", 90), ("SE", 135), ("S", 180), ("SW", 225), ("W", 270), ("NW", 315) };
            canvas.FontColor = NightMode ? Color.FromArgb("#E0604A") : Color.FromArgb("#7FB4FF");
            canvas.FontSize = 17;
            foreach (var (t, az) in cardinals)
            {
                var (x, y, vis) = SkyMap.Project(0, az, v);
                if (vis) canvas.DrawString(t, x, y - 8, HorizontalAlignment.Center);
            }

            // viewfinder chrome: corner brackets + crosshair
            float cx = rect.Width / 2, cy = rect.Height / 2;
            canvas.StrokeColor = starC.WithAlpha(0.4f);
            canvas.StrokeSize = 2.2f;
            float m = 22, len = 26;
            canvas.DrawLine(m, m + len, m, m); canvas.DrawLine(m, m, m + len, m);
            canvas.DrawLine(rect.Width - m - len, m, rect.Width - m, m); canvas.DrawLine(rect.Width - m, m, rect.Width - m, m + len);
            canvas.DrawLine(m, rect.Height - m - len, m, rect.Height - m); canvas.DrawLine(m, rect.Height - m, m + len, rect.Height - m);
            canvas.DrawLine(rect.Width - m - len, rect.Height - m, rect.Width - m, rect.Height - m); canvas.DrawLine(rect.Width - m, rect.Height - m, rect.Width - m, rect.Height - m - len);

            canvas.StrokeColor = starC.WithAlpha(0.5f);
            canvas.StrokeSize = 1.4f;
            canvas.DrawLine(cx - 14, cy, cx - 5, cy);
            canvas.DrawLine(cx + 5, cy, cx + 14, cy);
            canvas.DrawLine(cx, cy - 14, cx, cy - 5);
            canvas.DrawLine(cx, cy + 5, cx, cy + 14);
            canvas.DrawCircle(cx, cy, 22);
        }

        private void DrawBody(ICanvas canvas, SkyMap.View v, double raDeg, double decDeg, string name, float r, Color colour, Color labelC, DateTime utc)
        {
            var (alt, az) = SkyCalc.AltAz(raDeg, decDeg, Lat, Lon, utc);
            if (alt < -5) return;
            var (x, y, vis) = SkyMap.Project(alt, az, v);
            if (!vis) return;
            canvas.FillColor = colour.WithAlpha(0.25f);
            canvas.FillCircle(x, y, r * 2.2f);
            canvas.FillColor = colour;
            canvas.FillCircle(x, y, r);
            canvas.FontColor = labelC;
            canvas.FontSize = 13;
            canvas.DrawString(name, x + r + 5, y + 4, HorizontalAlignment.Left);
        }
    }

    // The little compass rose: a ring with tick marks and a needle that keeps
    // pointing at true north while you turn.
    public class CompassRoseDrawable : IDrawable
    {
        public double HeadingDeg;
        public bool NightMode;

        public void Draw(ICanvas canvas, RectF rect)
        {
            float cx = rect.Width / 2, cy = rect.Height / 2;
            float r = Math.Min(cx, cy) - 3;
            canvas.Antialias = true;

            Color ring = NightMode ? Color.FromArgb("#8A2A1E") : Color.FromArgb("#6C7FA8");
            Color text = NightMode ? Color.FromArgb("#E0604A") : Color.FromArgb("#C9D3EA");

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
            canvas.StrokeColor = NightMode ? Color.FromArgb("#FF5A40") : Color.FromArgb("#E5484D");
            canvas.DrawLine(cx, cy, nx, ny);
            canvas.StrokeColor = text.WithAlpha(0.6f);
            canvas.DrawLine(cx, cy, sx, sy);

            var nRad = -HeadingDeg * Math.PI / 180.0;
            canvas.FontColor = text;
            canvas.FontSize = 12;
            canvas.DrawString("N", cx + (float)(Math.Sin(nRad) * (r + 0)) , cy - (float)(Math.Cos(nRad) * (r + 0)) + 4, HorizontalAlignment.Center);
        }
    }
}
