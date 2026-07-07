using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using dinospace.Models;
using dinospace.Services;

namespace dinospace.Views
{
    // Draw a creature or space object, then give it stats — a simple, fun
    // "make your own encyclopedia entry" studio. The drawing is a real paint
    // canvas (finger strokes, colours, brush sizes, undo, clear); the form
    // collects every field a built-in entry has.
    public class CreationEditorPage : ContentPage
    {
        private readonly UserCreation _c;
        private readonly bool _isNew;
        private readonly PaintDrawable _paint = new();
        private GraphicsView _canvas = null!;
        private Stroke? _active;

        private CreationKind _kind;
        private VerticalStackLayout _formArea = null!;
        private Color _brush = Color.FromArgb("#2B2B33");
        private float _brushSize = 8;
        private bool _erasing;

        // Toolbar pieces we repaint when the selection changes.
        private readonly List<(Border border, Color color)> _swatches = new();
        private readonly List<(Border border, float size)> _sizes = new();
        private Border _eraserBtn = null!;

        // The form entries, read back on save.
        private readonly Dictionary<string, Entry> _fields = new();
        private readonly Dictionary<string, string> _picks = new();

        public CreationEditorPage(UserCreation? existing = null)
        {
            _isNew = existing == null;
            _c = existing ?? new UserCreation { Id = Guid.NewGuid().ToString("N"), Kind = CreationKind.Dinosaur };
            _kind = _c.Kind;
            Build();
            SwipeBack.Attach(this);
        }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(18, 6, 18, 30) };

            stack.Add(new Label
            {
                Text = _isNew ? "Create your own" : "Edit your creation",
                FontFamily = Ui.Display, FontSize = Ui.S(26), TextColor = Theme.TextPrimary
            });
            stack.Add(new Label
            {
                Text = "Draw it, name it, and give it stats. It'll join your collection — and your dinosaurs can even enter Dino Battle!",
                FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), LineHeight = 1.4, TextColor = Theme.TextSecondary
            });

            stack.Add(KindToggle());
            stack.Add(CanvasCard());
            stack.Add(Toolbar());

            _formArea = new VerticalStackLayout { Spacing = 14 };
            stack.Add(_formArea);
            BuildForm();

            stack.Add(Ui.PrimaryButton(_isNew ? "SAVE MY CREATION" : "SAVE CHANGES", async (_, _) => await Save()));
            if (!_isNew)
            {
                var del = new Label
                {
                    Text = "Delete this creation", FontFamily = Ui.Display, FontSize = Ui.S(17),
                    TextColor = Theme.Danger, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 8, 0, 0)
                };
                Ui.OnTap(del, async (_, _) => await ConfirmDelete());
                stack.Add(del);
            }

            var body = Nav.DetailScaffoldFixed("", new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never });
            Content = Ui.PageRoot(body);
        }

        // ----- kind toggle -----
        private View KindToggle()
        {
            var row = new Grid { ColumnSpacing = 10 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            View Pill(string text, CreationKind kind)
            {
                bool on = _kind == kind;
                var label = new Label
                {
                    Text = text, FontFamily = Ui.Fonts, FontSize = Ui.S(14), FontAttributes = FontAttributes.Bold,
                    TextColor = on ? Theme.TextOnAccent : Theme.TextSecondary,
                    HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center
                };
                var b = new Border
                {
                    Content = label,
                    BackgroundColor = on ? Theme.Accent : Theme.Surface,
                    Stroke = on ? Colors.Transparent : Theme.HairlineSoft, StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = 14 }, Padding = new Thickness(10, 12)
                };
                Ui.OnTap(b, (_, _) => { if (_kind != kind) { _kind = kind; Build(); } });
                return b;
            }

            row.Add(Pill("🦖  Dinosaur", CreationKind.Dinosaur), 0, 0);
            row.Add(Pill("🪐  Space object", CreationKind.Space), 1, 0);
            return row;
        }

        // ----- the drawing canvas -----
        private View CanvasCard()
        {
            _canvas = new GraphicsView { Drawable = _paint, HeightRequest = 300, BackgroundColor = Colors.White };

            var pointer = new PointerGestureRecognizer();
            pointer.PointerPressed += (_, e) =>
            {
                var p = e.GetPosition(_canvas);
                if (p == null) return;
                _active = new Stroke { Color = _erasing ? Colors.White : _brush, Width = _erasing ? _brushSize * 2.2f : _brushSize };
                _active.Points.Add(new PointF((float)p.Value.X, (float)p.Value.Y));
                _paint.Strokes.Add(_active);
                _canvas.Invalidate();
            };
            pointer.PointerMoved += (_, e) =>
            {
                if (_active == null) return;
                var p = e.GetPosition(_canvas);
                if (p == null) return;
                _active.Points.Add(new PointF((float)p.Value.X, (float)p.Value.Y));
                _canvas.Invalidate();
            };
            pointer.PointerReleased += (_, _) => _active = null;
            _canvas.GestureRecognizers.Add(pointer);

            return new Border
            {
                Content = _canvas,
                BackgroundColor = Colors.White,
                Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = 0, HeightRequest = 300
            };
        }

        // ----- brush / colour / undo / clear toolbar -----
        private View Toolbar()
        {
            string[] palette =
            {
                "#2B2B33", "#E5484D", "#F0883E", "#F4C63D", "#3FB950", "#3B82F6",
                "#8B5CF6", "#EC4899", "#8B5E3C", "#12A594", "#FFFFFF"
            };
            _swatches.Clear();
            _sizes.Clear();

            var swatches = new HorizontalStackLayout { Spacing = 10, Padding = new Thickness(2, 2) };
            foreach (var hex in palette)
            {
                var swatchColor = Color.FromArgb(hex);
                var sw = new Border
                {
                    WidthRequest = 34, HeightRequest = 34,
                    BackgroundColor = swatchColor,
                    StrokeThickness = 3,
                    StrokeShape = new RoundRectangle { CornerRadius = 17 }
                };
                Ui.OnTap(sw, (_, _) => { _brush = swatchColor; _erasing = false; RefreshTools(); });
                _swatches.Add((sw, swatchColor));
                swatches.Add(sw);
            }

            var sizeRow = new HorizontalStackLayout { Spacing = 8, Padding = new Thickness(2, 2) };
            foreach (var size in new float[] { 4, 8, 16, 28 })
            {
                var dotWrap = new Border
                {
                    WidthRequest = 46, HeightRequest = 38,
                    BackgroundColor = Theme.Surface, StrokeThickness = 2,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Content = new GraphicsView { Drawable = new DotDrawable { Radius = Math.Min(size / 2, 12) }, InputTransparent = true }
                };
                float s = size;
                Ui.OnTap(dotWrap, (_, _) => { _brushSize = s; _erasing = false; RefreshTools(); });
                _sizes.Add((dotWrap, size));
                sizeRow.Add(dotWrap);
            }

            Border ToolBtn(string glyph, string label, Action tap)
            {
                var content = new HorizontalStackLayout
                {
                    Spacing = 6, HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label { Text = glyph, FontSize = 15, VerticalTextAlignment = TextAlignment.Center },
                        new Label { Text = label, FontFamily = Ui.Fonts, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextSecondary, VerticalTextAlignment = TextAlignment.Center }
                    }
                };
                var b = new Border
                {
                    Content = content, BackgroundColor = Theme.Surface,
                    Stroke = Theme.HairlineSoft, StrokeThickness = 2,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = new Thickness(14, 9)
                };
                Ui.OnTap(b, (_, _) => tap());
                return b;
            }

            _eraserBtn = ToolBtn("🧽", "Eraser", () => { _erasing = !_erasing; RefreshTools(); });

            var actions = new HorizontalStackLayout { Spacing = 8, Padding = new Thickness(2, 2) };
            actions.Add(_eraserBtn);
            actions.Add(ToolBtn("↩", "Undo", () =>
            {
                if (_paint.Strokes.Count > 0) { _paint.Strokes.RemoveAt(_paint.Strokes.Count - 1); _canvas.Invalidate(); AppSettings.Tap(); }
            }));
            actions.Add(ToolBtn("🗑", "Clear", () => { _paint.Strokes.Clear(); _canvas.Invalidate(); AppSettings.Tap(); }));

            var col = new VerticalStackLayout { Spacing = 10 };
            col.Add(new Label { Text = "Colours", FontFamily = Ui.Fonts, FontSize = Ui.S(11.5), FontAttributes = FontAttributes.Bold, TextColor = Theme.TextHint });
            col.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = swatches });
            col.Add(new Label { Text = "Brush & tools", FontFamily = Ui.Fonts, FontSize = Ui.S(11.5), FontAttributes = FontAttributes.Bold, TextColor = Theme.TextHint, Margin = new Thickness(0, 2, 0, 0) });
            var tools = new HorizontalStackLayout { Spacing = 8 };
            tools.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = sizeRow });
            col.Add(tools);
            col.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = actions });

            RefreshTools();
            return col;
        }

        // Repaints the selected colour, brush size and eraser so the current
        // tool is always obvious.
        private void RefreshTools()
        {
            foreach (var (border, color) in _swatches)
                border.Stroke = (!_erasing && ColorsEqual(color, _brush)) ? Theme.Accent : Color.FromArgb("#22000000");
            foreach (var (border, size) in _sizes)
                border.Stroke = (!_erasing && Math.Abs(size - _brushSize) < 0.1f) ? Theme.Accent : Theme.HairlineSoft;
            if (_eraserBtn != null)
            {
                _eraserBtn.Stroke = _erasing ? Theme.Accent : Theme.HairlineSoft;
                _eraserBtn.BackgroundColor = _erasing ? Ui.MultiplyAlpha(Theme.Accent, 0.14f) : Theme.Surface;
            }
            AppSettings.Tap();
        }

        private static bool ColorsEqual(Color a, Color b)
            => Math.Abs(a.Red - b.Red) < 0.01 && Math.Abs(a.Green - b.Green) < 0.01 && Math.Abs(a.Blue - b.Blue) < 0.01;

        // ----- the stat form -----
        private void BuildForm()
        {
            _formArea.Children.Clear();
            _fields.Clear();
            _picks.Clear();

            _formArea.Add(Ui.SectionHeader("The basics"));
            _formArea.Add(Field("name", "Name", _c.Name, "What's it called?"));
            _formArea.Add(Field("pron", "How to say it", _c.Pronunciation, "e.g. DINO-space-us"));

            if (_kind == CreationKind.Dinosaur)
            {
                _formArea.Add(Field("meaning", "Name meaning", _c.Meaning, "e.g. \"thunder lizard\""));
                _formArea.Add(Field("short", "Subtitle", _c.ShortDescription, "a short catchy line"));

                _formArea.Add(Picker("era", "When did it live?", new[] { "Triassic", "Jurassic", "Cretaceous" }, _c.Era));
                _formArea.Add(Picker("diet", "What did it eat?", new[] { "Carnivore", "Herbivore", "Omnivore" }, string.IsNullOrWhiteSpace(_c.Diet) ? "Carnivore" : _c.Diet));
                _formArea.Add(Picker("cat", "Where did it live?", new[] { "Land", "Sea", "Flying" }, string.IsNullOrWhiteSpace(_c.Category) ? "Land" : _c.Category));

                _formArea.Add(Ui.SectionHeader("Stats"));
                _formArea.Add(Field("length", "Length (feet)", _c.Length, "e.g. 40", numeric: true));
                _formArea.Add(Field("height", "Height (feet)", _c.Height, "e.g. 15", numeric: true));
                _formArea.Add(Field("weight", "Weight (kg)", _c.Weight, "e.g. 7000", numeric: true));
                _formArea.Add(Field("speed", "Top speed (km/h)", _c.Speed, "e.g. 30", numeric: true));
                _formArea.Add(Field("bite", "Bite force (PSI)", _c.BiteForce, "e.g. 12000", numeric: true));
            }
            else
            {
                _formArea.Add(Field("short", "Subtitle", string.IsNullOrWhiteSpace(_c.Subtitle) ? _c.ShortDescription : _c.Subtitle, "a short catchy line"));
                _formArea.Add(Picker("type", "What kind is it?", new[] { "Planet", "Moon", "Star", "Galaxy", "Nebula", "Comet", "Asteroid", "Black hole" }, string.IsNullOrWhiteSpace(_c.TypeLabel) ? "Planet" : _c.TypeLabel));

                _formArea.Add(Ui.SectionHeader("Facts (label + value)"));
                _formArea.Add(StatPair("s1", "Fact 1", _c.Stat1Label, _c.Stat1Value, "Diameter", "e.g. 12,000 km"));
                _formArea.Add(StatPair("s2", "Fact 2", _c.Stat2Label, _c.Stat2Value, "Distance", "e.g. 200 million km"));
                _formArea.Add(StatPair("s3", "Fact 3", _c.Stat3Label, _c.Stat3Value, "Temperature", "e.g. -100°C"));
                _formArea.Add(StatPair("s4", "Fact 4", _c.Stat4Label, _c.Stat4Value, "Moons", "e.g. 3"));
            }

            _formArea.Add(Ui.SectionHeader("Tell its story"));
            _formArea.Add(Field("about", "About", _c.About, "Describe your creation…", multiline: true));
            _formArea.Add(Field("facts", "Fun facts (one per line)", _c.FunFacts, "Write a cool fact…", multiline: true));
        }

        private View Field(string key, string label, string value, string placeholder, bool numeric = false, bool multiline = false)
        {
            var entry = new Entry
            {
                Text = value, Placeholder = placeholder,
                FontFamily = Ui.Fonts, FontSize = Ui.S(15),
                TextColor = Theme.TextPrimary, PlaceholderColor = Theme.TextHint,
                BackgroundColor = Colors.Transparent,
                Keyboard = numeric ? Keyboard.Numeric : Keyboard.Default
            };
            _fields[key] = entry;
            var col = new VerticalStackLayout { Spacing = 4 };
            col.Add(new Label { Text = label, FontFamily = Ui.Fonts, FontSize = Ui.S(12), FontAttributes = FontAttributes.Bold, TextColor = Theme.Accent });
            col.Add(new Border
            {
                Content = entry, BackgroundColor = Theme.Surface,
                Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = new Thickness(14, multiline ? 8 : 2),
                MinimumHeightRequest = multiline ? 84 : 0
            });
            return col;
        }

        private View StatPair(string key, string label, string labelVal, string valueVal, string labelHint, string valueHint)
        {
            var l = new Entry { Text = labelVal, Placeholder = labelHint, FontFamily = Ui.Fonts, FontSize = Ui.S(14), TextColor = Theme.TextPrimary, PlaceholderColor = Theme.TextHint, BackgroundColor = Colors.Transparent };
            var v = new Entry { Text = valueVal, Placeholder = valueHint, FontFamily = Ui.Fonts, FontSize = Ui.S(14), TextColor = Theme.TextPrimary, PlaceholderColor = Theme.TextHint, BackgroundColor = Colors.Transparent };
            _fields[key + "L"] = l;
            _fields[key + "V"] = v;

            Border Wrap(View e) => new()
            {
                Content = e, BackgroundColor = Theme.Surface, Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = new Thickness(12, 2)
            };
            var grid = new Grid { ColumnSpacing = 8 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
            grid.Add(Wrap(l), 0, 0);
            grid.Add(Wrap(v), 1, 0);
            return grid;
        }

        // A simple wrap of tappable pills; the chosen one is remembered in _picks.
        private View Picker(string key, string label, string[] options, string current)
        {
            _picks[key] = string.IsNullOrWhiteSpace(current) ? options[0] : current;
            var flow = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, Direction = Microsoft.Maui.Layouts.FlexDirection.Row };
            var pills = new List<(Border b, Label l, string opt)>();

            void Repaint()
            {
                foreach (var (b, lab, opt) in pills)
                {
                    bool on = _picks[key] == opt;
                    b.BackgroundColor = on ? Theme.Accent : Theme.Surface;
                    b.Stroke = on ? Colors.Transparent : Theme.HairlineSoft;
                    lab.TextColor = on ? Theme.TextOnAccent : Theme.TextSecondary;
                }
            }

            foreach (var opt in options)
            {
                var lab = new Label { Text = opt, FontFamily = Ui.Fonts, FontSize = Ui.S(13), FontAttributes = FontAttributes.Bold, VerticalTextAlignment = TextAlignment.Center };
                var b = new Border
                {
                    Content = lab, StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = new Thickness(14, 8),
                    Margin = new Thickness(0, 4, 8, 0)
                };
                string o = opt;
                Ui.OnTap(b, (_, _) => { _picks[key] = o; Repaint(); AppSettings.Tap(); });
                pills.Add((b, lab, opt));
                flow.Add(b);
            }
            Repaint();

            var col = new VerticalStackLayout { Spacing = 6 };
            col.Add(new Label { Text = label, FontFamily = Ui.Fonts, FontSize = Ui.S(12), FontAttributes = FontAttributes.Bold, TextColor = Theme.Accent });
            col.Add(flow);
            return col;
        }

        private string F(string key) => _fields.TryGetValue(key, out var e) ? (e.Text ?? "").Trim() : "";
        private string P(string key) => _picks.TryGetValue(key, out var v) ? v : "";

        // ----- save -----
        private async System.Threading.Tasks.Task Save()
        {
            string name = F("name");
            if (name.Length == 0)
            {
                await DisplayAlertAsync("Give it a name", "Every creation needs a name before you can save it!", "OK");
                return;
            }

            _c.Kind = _kind;
            _c.Name = name;
            _c.Pronunciation = F("pron");
            _c.ShortDescription = F("short");
            _c.About = F("about");
            _c.FunFacts = F("facts");

            if (_kind == CreationKind.Dinosaur)
            {
                _c.Meaning = F("meaning");
                _c.Era = P("era");
                _c.Diet = P("diet");
                _c.Category = P("cat");
                _c.Length = F("length");
                _c.Height = F("height");
                _c.Weight = F("weight");
                _c.Speed = F("speed");
                _c.BiteForce = F("bite");
            }
            else
            {
                _c.Subtitle = F("short");
                _c.TypeLabel = P("type");
                _c.Stat1Label = F("s1L"); _c.Stat1Value = F("s1V");
                _c.Stat2Label = F("s2L"); _c.Stat2Value = F("s2V");
                _c.Stat3Label = F("s3L"); _c.Stat3Value = F("s3V");
                _c.Stat4Label = F("s4L"); _c.Stat4Value = F("s4V");
            }

            if (_c.CreatedTicks == 0) _c.CreatedTicks = DateTime.UtcNow.Ticks;

            // Export the drawing to a PNG. If they didn't draw anything on an
            // edit, keep the picture they already had.
            if (_paint.Strokes.Count > 0)
            {
                string path = CreationStore.NewImagePath(_c.Id);
                double density = 2.75;
                try { density = DeviceDisplay.MainDisplayInfo.Density; } catch { }
                bool ok = CreationCanvas.ExportPng(_paint.Strokes, _canvas.Width, _canvas.Height, density, path);
                if (ok) _c.ImagePath = path;
            }

            CreationStore.Save(_c);
            AppSettings.LongPress();
            try { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); } catch { }
        }

        private async System.Threading.Tasks.Task ConfirmDelete()
        {
            bool sure = await DisplayAlertAsync("Delete this creation?", $"“{_c.Name}” will be gone for good. This can't be undone.", "Delete", "Keep it");
            if (!sure) return;
            CreationStore.Delete(_c.Id);
            try { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); } catch { }
        }
    }

    // One drawn stroke: a colour, a width, and the points the finger traced.
    public class Stroke
    {
        public List<PointF> Points { get; set; } = new();
        public Color Color { get; set; } = Colors.Black;
        public float Width { get; set; } = 8;
    }

    // Renders the strokes onto the on-screen canvas.
    public class PaintDrawable : IDrawable
    {
        public List<Stroke> Strokes { get; } = new();

        public void Draw(ICanvas canvas, RectF rect)
        {
            canvas.Antialias = true;
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(rect);
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;

            foreach (var s in Strokes)
            {
                canvas.StrokeColor = s.Color;
                canvas.StrokeSize = s.Width;
                if (s.Points.Count == 1)
                {
                    canvas.FillColor = s.Color;
                    canvas.FillCircle(s.Points[0].X, s.Points[0].Y, s.Width / 2);
                }
                else if (s.Points.Count > 1)
                {
                    var path = new PathF();
                    path.MoveTo(s.Points[0].X, s.Points[0].Y);
                    for (int i = 1; i < s.Points.Count; i++) path.LineTo(s.Points[i].X, s.Points[i].Y);
                    canvas.DrawPath(path);
                }
            }
        }
    }

    // A tiny dot preview for the brush-size buttons.
    public class DotDrawable : IDrawable
    {
        public double Radius = 4;
        public void Draw(ICanvas canvas, RectF rect)
        {
            canvas.Antialias = true;
            canvas.FillColor = Color.FromArgb("#2B2B33");
            canvas.FillCircle(rect.Center.X, rect.Center.Y, (float)Radius);
        }
    }

    // Rasterises the strokes to a PNG file. Android-native (the shipping
    // platform); a no-op elsewhere, where the gallery falls back to a placeholder.
    public static class CreationCanvas
    {
        public static bool ExportPng(List<Stroke> strokes, double viewWidth, double viewHeight, double density, string path)
        {
#if ANDROID
            try
            {
                int w = Math.Max(1, (int)(viewWidth * density));
                int h = Math.Max(1, (int)(viewHeight * density));
                using var bitmap = Android.Graphics.Bitmap.CreateBitmap(w, h, Android.Graphics.Bitmap.Config.Argb8888!);
                using var acanvas = new Android.Graphics.Canvas(bitmap);
                acanvas.DrawColor(Android.Graphics.Color.White);

                using var paint = new Android.Graphics.Paint { AntiAlias = true };
                paint.StrokeCap = Android.Graphics.Paint.Cap.Round;
                paint.StrokeJoin = Android.Graphics.Paint.Join.Round;

                foreach (var s in strokes)
                {
                    var col = Android.Graphics.Color.Argb(
                        (int)(s.Color.Alpha * 255), (int)(s.Color.Red * 255),
                        (int)(s.Color.Green * 255), (int)(s.Color.Blue * 255));
                    paint.Color = col;
                    paint.StrokeWidth = (float)(s.Width * density);
                    if (s.Points.Count == 1)
                    {
                        paint.SetStyle(Android.Graphics.Paint.Style.Fill);
                        acanvas.DrawCircle((float)(s.Points[0].X * density), (float)(s.Points[0].Y * density), (float)(s.Width * density / 2), paint);
                    }
                    else if (s.Points.Count > 1)
                    {
                        paint.SetStyle(Android.Graphics.Paint.Style.Stroke);
                        using var apath = new Android.Graphics.Path();
                        apath.MoveTo((float)(s.Points[0].X * density), (float)(s.Points[0].Y * density));
                        for (int i = 1; i < s.Points.Count; i++)
                            apath.LineTo((float)(s.Points[i].X * density), (float)(s.Points[i].Y * density));
                        acanvas.DrawPath(apath, paint);
                    }
                }

                using var fs = System.IO.File.Create(path);
                bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Png!, 100, fs);
                bitmap.Recycle();
                return true;
            }
            catch { return false; }
#else
            return false;
#endif
        }
    }
}
