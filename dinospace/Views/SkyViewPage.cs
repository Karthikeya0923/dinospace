using System;
using System.Linq;
using System.Numerics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Sky View — point your phone at the sky and see what's there, live:
    // stars, constellation figures with names, planets, and the moon, all
    // drawn for exactly the direction you're facing. Uses the orientation
    // sensor when the phone has one; drag to explore when it doesn't.
    public class SkyViewPage : ContentPage
    {
        private readonly double _lat, _lon;
        private readonly SkyViewDrawable _drawable;
        private GraphicsView _view = null!;
        private Label _lookingAt = null!;
        private Label _hint = null!;

        private double _az = 180, _alt = 30;     // where we're "pointing"
        private bool _sensorMode;
        private bool _dirty = true;
        private IDispatcherTimer? _timer;

        public SkyViewPage()
        {
            var where = SkyService.Cached;
            _lat = where.Lat; _lon = where.Lon;
            _drawable = new SkyViewDrawable { Lat = _lat, Lon = _lon };
            Build();
        }

        private void Build()
        {
            _view = new GraphicsView { Drawable = _drawable };

            // drag-to-pan always works; the sensor takes over when available
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

            var back = new Border
            {
                Content = Ui.Icon(Ui.IconBack, 24, Colors.White),
                BackgroundColor = Color.FromArgb("#33000000"),
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 22 },
                WidthRequest = 44, HeightRequest = 44,
                HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(14, 14, 0, 0)
            };
            Ui.OnTap(back, async (_, _) =>
            {
                try { if (Shell.Current.Navigation.NavigationStack.Count > 1) await Shell.Current.Navigation.PopAsync(); } catch { }
            });
            Ui.Describe(back, "Go back");

            _lookingAt = new Label
            {
                Text = "Sky View",
                FontFamily = Ui.Display, FontSize = Ui.S(22),
                TextColor = Color.FromArgb("#F5F1E4"),
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 24, 0, 0)
            };
            _hint = new Label
            {
                Text = "…",
                FontFamily = Ui.Fonts, FontSize = Ui.S(13),
                TextColor = Color.FromArgb("#B9BDD1"),
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(20, 0, 20, 28),
                HorizontalTextAlignment = TextAlignment.Center
            };

            var root = new Grid { BackgroundColor = Color.FromArgb("#070B14") };
            root.Add(_view);
            root.Add(_lookingAt);
            root.Add(_hint);
            root.Add(back);
            Content = root;
            Shell.SetNavBarIsVisible(this, false);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            StartSensor();
            _hint.Text = _sensorMode
                ? "Move your phone around the sky — names appear as you aim"
                : "Drag to look around the sky";

            _timer ??= Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(50);   // 20 fps is plenty for stars
            _timer.Tick += (_, _) =>
            {
                if (!_dirty) return;
                _dirty = false;
                _drawable.CenterAz = _az;
                _drawable.CenterAlt = _alt;
                _view.Invalidate();
                UpdateLookingAt();
            };
            _timer.Start();
            _dirty = true;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _timer?.Stop();
            StopSensor();
        }

        // ----- orientation sensor -> where the back of the phone points -----

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
            // The quaternion maps device axes into world axes (x east, y north,
            // z up). The back camera looks along the device's -Z, so rotate
            // that and read off compass azimuth + altitude.
            var q = e.Reading.Orientation;
            var dir = Vector3.Transform(new Vector3(0, 0, -1), q);
            double len = Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y + dir.Z * dir.Z);
            if (len < 1e-6) return;
            double alt = Math.Asin(Math.Clamp(dir.Z / len, -1, 1)) * 180 / Math.PI;
            double az = (Math.Atan2(dir.X, dir.Y) * 180 / Math.PI + 360) % 360;

            // light smoothing so the view doesn't jitter with the hand
            double dAz = ((az - _az + 540) % 360) - 180;
            _az = (_az + dAz * 0.25 + 360) % 360;
            _alt += (alt - _alt) * 0.25;
            _dirty = true;
        }

        private void UpdateLookingAt()
        {
            var utc = DateTime.UtcNow;
            string? figure = SkyMap.NearestFigure(_lat, _lon, utc, _alt, _az);

            // anything bright sitting close to the crosshair?
            string? target = null;
            double best = 8;
            double jd = SkyCalc.JulianDay(utc);
            foreach (var b in Enum.GetValues<SkyCalc.Body>())
            {
                var (ra, dec, _) = SkyCalc.PlanetRaDec(b, jd);
                var (alt, az) = SkyCalc.AltAz(ra, dec, _lat, _lon, utc);
                double sep = SkyMap.Separation(alt, az, _alt, _az);
                if (alt > -5 && sep < best) { best = sep; target = b.ToString(); }
            }
            foreach (var s in SkyMap.Stars)
            {
                var (alt, az) = SkyCalc.AltAz(s.RaHours * 15.0, s.DecDeg, _lat, _lon, utc);
                double sep = SkyMap.Separation(alt, az, _alt, _az);
                if (alt > -5 && sep < best) { best = sep; target = s.Name; }
            }

            _lookingAt.Text = target != null && figure != null ? $"{target} · {figure}"
                            : target ?? figure ?? (_alt < -5 ? "Below the horizon" : "Open sky");
        }
    }

    // Draws one frame of the pointed-at sky.
    public class SkyViewDrawable : IDrawable
    {
        public double Lat, Lon;
        public double CenterAz = 180, CenterAlt = 30;
        public double FovDeg = 95;

        public void Draw(ICanvas canvas, RectF rect)
        {
            var utc = DateTime.UtcNow;
            float size = Math.Min(rect.Width, rect.Height);
            var v = new SkyMap.View(Lat, Lon, utc, CenterAz, CenterAlt, FovDeg, Math.Max(rect.Width, rect.Height));

            canvas.Antialias = true;

            // below-horizon shading: project a few ground points and wash the
            // bottom when we're aimed low
            if (CenterAlt < 12)
            {
                canvas.FillColor = Color.FromArgb("#0E1526").WithAlpha(0.85f);
                canvas.FillRectangle(0, rect.Height * (float)(0.5 + CenterAlt / FovDeg), rect.Width, rect.Height);
            }

            // constellation lines first, so stars sit on top
            canvas.StrokeColor = Color.FromArgb("#5A7BB8").WithAlpha(0.8f);
            canvas.StrokeSize = 1.6f;
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
                        canvas.DrawLine(pts[a].x, pts[a].y, pts[b].x, pts[b].y);
                        any = true;
                    }
                if (any)
                {
                    // name near the figure's first visible star
                    var anchor = pts.FirstOrDefault(p => p.ok);
                    canvas.FontColor = Color.FromArgb("#8FA1C7");
                    canvas.FontSize = 13;
                    canvas.DrawString(f.Name, anchor.x + 10, anchor.y - 10, HorizontalAlignment.Left);
                }
            }

            // stars, sized by brightness
            foreach (var s in SkyMap.Stars)
            {
                var (alt, az) = SkyCalc.AltAz(s.RaHours * 15.0, s.DecDeg, Lat, Lon, utc);
                if (alt < -5) continue;
                var (x, y, vis) = SkyMap.Project(alt, az, v);
                if (!vis) continue;
                float r = (float)Math.Max(1.8, 6.5 - s.Mag * 2.0);
                canvas.FillColor = s.Colour switch
                {
                    "red" or "red-orange" => Color.FromArgb("#FFAA80"),
                    "orange" => Color.FromArgb("#FFC888"),
                    "golden" or "yellow" or "yellow-white" => Color.FromArgb("#FFE9B0"),
                    "blue" or "blue-white" => Color.FromArgb("#CFE2FF"),
                    _ => Color.FromArgb("#F2F5FA")
                };
                canvas.FillCircle(x, y, r);
                if (s.Mag < 0.5)
                {
                    canvas.FontColor = Color.FromArgb("#B9C4DE");
                    canvas.FontSize = 12;
                    canvas.DrawString(s.Name, x + 8, y + 4, HorizontalAlignment.Left);
                }
            }

            // planets + moon, gold and labelled
            double jd = SkyCalc.JulianDay(utc);
            foreach (var b in Enum.GetValues<SkyCalc.Body>())
            {
                var (ra, dec, _) = SkyCalc.PlanetRaDec(b, jd);
                DrawBody(canvas, v, ra, dec, b.ToString(), 5, Color.FromArgb("#FFD98C"), utc);
            }
            var (mra, mdec) = SkyCalc.MoonRaDec(jd);
            DrawBody(canvas, v, mra, mdec, "Moon", 11, Color.FromArgb("#F2ECD8"), utc);

            // horizon compass letters
            (string t, double az)[] cardinals = { ("N", 0), ("NE", 45), ("E", 90), ("SE", 135), ("S", 180), ("SW", 225), ("W", 270), ("NW", 315) };
            canvas.FontColor = Color.FromArgb("#7FB4FF");
            canvas.FontSize = 17;
            foreach (var (t, az) in cardinals)
            {
                var (x, y, vis) = SkyMap.Project(0, az, v);
                if (vis) canvas.DrawString(t, x, y, HorizontalAlignment.Center);
            }

            // crosshair
            float cx = rect.Width / 2, cy = rect.Height / 2;
            canvas.StrokeColor = Colors.White.WithAlpha(0.35f);
            canvas.StrokeSize = 1.4f;
            canvas.DrawLine(cx - 14, cy, cx - 5, cy);
            canvas.DrawLine(cx + 5, cy, cx + 14, cy);
            canvas.DrawLine(cx, cy - 14, cx, cy - 5);
            canvas.DrawLine(cx, cy + 5, cx, cy + 14);
        }

        private void DrawBody(ICanvas canvas, SkyMap.View v, double raDeg, double decDeg, string name, float r, Color colour, DateTime utc)
        {
            var (alt, az) = SkyCalc.AltAz(raDeg, decDeg, Lat, Lon, utc);
            if (alt < -5) return;
            var (x, y, vis) = SkyMap.Project(alt, az, v);
            if (!vis) return;
            canvas.FillColor = colour;
            canvas.FillCircle(x, y, r);
            canvas.FontColor = Color.FromArgb("#FFD98C");
            canvas.FontSize = 13;
            canvas.DrawString(name, x + r + 4, y + 4, HorizontalAlignment.Left);
        }
    }
}
