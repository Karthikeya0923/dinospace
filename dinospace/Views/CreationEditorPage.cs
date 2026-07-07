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
    // Draw a creature or space object, then give it stats — a proper little
    // paint studio (comparable to MS Paint: freehand brush/pencil/marker,
    // shapes, adjustable sizes, a colour palette with a custom-colour mixer,
    // a fill bucket, undo/redo) plus a full stat form so a creation looks
    // exactly like a real encyclopedia entry.
    public class CreationEditorPage : ContentPage
    {
        private readonly UserCreation _c;
        private readonly bool _isNew;

        // Canvas state.
        private readonly PaintDrawable _paint = new();
        private GraphicsView _canvas = null!;
        private DrawOp? _current;
        private readonly Stack<DrawOp> _redo = new();

        private CreationKind _kind;
        private VerticalStackLayout _formArea = null!;

        // Tools.
        private DrawTool _tool = DrawTool.Brush;
        private Color _brush = Color.FromArgb("#2B2B33");
        private float _brushSize = 8;
        private bool _fillShapes;

        // Toolbar pieces we repaint on change.
        private readonly List<(Border border, Color color)> _swatches = new();
        private readonly List<(Border border, float size)> _sizes = new();
        private readonly List<(Border border, DrawTool tool)> _toolBtns = new();
        private Border _customSwatch = null!;
        private Border _fillToggle = null!;
        private VerticalStackLayout _mixer = null!;
        private int _cr = 120, _cg = 90, _cb = 220;

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

            // GraphicsView interaction events fire on real touch drags on
            // Android (unlike PointerGestureRecognizer.PointerMoved, which only
            // reported hovers — that was why drawing came out as dots).
            _canvas.StartInteraction += (_, e) =>
            {
                var p = e.Touches.FirstOrDefault();
                _redo.Clear();
                if (_tool == DrawTool.Fill) { _paint.Background = _brush; _canvas.Invalidate(); AppSettings.Tap(); return; }

                _current = new DrawOp
                {
                    Tool = _tool,
                    Color = _tool == DrawTool.Eraser ? _paint.Background : _brush,
                    Width = _tool == DrawTool.Eraser ? _brushSize * 2.4f : _brushSize,
                    Fill = _fillShapes && IsShape(_tool),
                    Start = p, End = p
                };
                if (_tool is DrawTool.Brush or DrawTool.Eraser) _current.Points.Add(p);
                _paint.Ops.Add(_current);
                _canvas.Invalidate();
            };
            _canvas.DragInteraction += (_, e) =>
            {
                if (_current == null) return;
                var p = e.Touches.FirstOrDefault();
                if (_current.Tool is DrawTool.Brush or DrawTool.Eraser) _current.Points.Add(p);
                else _current.End = p;   // live shape preview
                _canvas.Invalidate();
            };
            _canvas.EndInteraction += (_, e) =>
            {
                if (_current == null) return;
                var p = e.Touches.FirstOrDefault();
                if (_current.Tool is DrawTool.Brush or DrawTool.Eraser) { if (p != default) _current.Points.Add(p); }
                else _current.End = p == default ? _current.End : p;
                _current = null;
                _canvas.Invalidate();
            };

            var frame = new Border
            {
                Content = _canvas,
                BackgroundColor = Colors.White,
                Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = 0, HeightRequest = 300
            };
            return frame;
        }

        private static bool IsShape(DrawTool t) =>
            t is DrawTool.Rectangle or DrawTool.Ellipse or DrawTool.Triangle or DrawTool.Star or DrawTool.Line or DrawTool.Arrow;

        // ----- toolbar -----
        private View Toolbar()
        {
            var col = new VerticalStackLayout { Spacing = 10 };

            // Tools
            col.Add(SmallLabel("Tools"));
            _toolBtns.Clear();
            var toolsRow = new HorizontalStackLayout { Spacing = 8, Padding = new Thickness(2, 2) };
            (DrawTool tool, string glyph)[] tools =
            {
                (DrawTool.Brush, "🖌"), (DrawTool.Line, "／"), (DrawTool.Rectangle, "▭"), (DrawTool.Ellipse, "◯"),
                (DrawTool.Triangle, "△"), (DrawTool.Star, "★"), (DrawTool.Arrow, "➤"), (DrawTool.Fill, "🪣"), (DrawTool.Eraser, "🧽")
            };
            foreach (var (tool, glyph) in tools)
            {
                var b = ChromeChip(new Label { Text = glyph, FontSize = 18, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center });
                b.WidthRequest = 46; b.HeightRequest = 42;
                Ui.OnTap(b, (_, _) => { _tool = tool; RefreshTools(); });
                _toolBtns.Add((b, tool));
                toolsRow.Add(b);
            }
            col.Add(HScroll(toolsRow));

            // Sizes
            col.Add(SmallLabel("Brush size"));
            _sizes.Clear();
            var sizeRow = new HorizontalStackLayout { Spacing = 8, Padding = new Thickness(2, 2) };
            foreach (var size in new float[] { 3, 8, 16, 28 })
            {
                var b = new Border
                {
                    WidthRequest = 46, HeightRequest = 40, BackgroundColor = Theme.Surface, StrokeThickness = 2,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Content = new GraphicsView { Drawable = new DotDrawable { Radius = Math.Min(size / 2, 12) }, InputTransparent = true }
                };
                float s = size;
                Ui.OnTap(b, (_, _) => { _brushSize = s; RefreshTools(); });
                _sizes.Add((b, size));
                sizeRow.Add(b);
            }
            col.Add(HScroll(sizeRow));

            // Colours
            col.Add(SmallLabel("Colours"));
            _swatches.Clear();
            var swatchRow = new HorizontalStackLayout { Spacing = 9, Padding = new Thickness(2, 2) };
            string[] palette =
            {
                "#000000", "#5A5A5A", "#9B9B9B", "#FFFFFF", "#7A3B12", "#E5544B", "#F0883E", "#F4C63D",
                "#8ED081", "#2E9E4B", "#12A594", "#3B82F6", "#1E3A8A", "#8B5CF6", "#EC4899", "#F6B8B0"
            };
            foreach (var hex in palette)
            {
                var swatchColor = Color.FromArgb(hex);
                var sw = new Border
                {
                    WidthRequest = 32, HeightRequest = 32, BackgroundColor = swatchColor,
                    StrokeThickness = 3, StrokeShape = new RoundRectangle { CornerRadius = 16 }
                };
                Ui.OnTap(sw, (_, _) => { _brush = swatchColor; if (_tool == DrawTool.Eraser) _tool = DrawTool.Brush; RefreshTools(); });
                _swatches.Add((sw, swatchColor));
                swatchRow.Add(sw);
            }
            // Custom colour swatch (opens the mixer).
            _customSwatch = new Border
            {
                WidthRequest = 32, HeightRequest = 32, BackgroundColor = Color.FromRgb(_cr, _cg, _cb),
                StrokeThickness = 3, StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Content = new Label { Text = "+", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }
            };
            Ui.OnTap(_customSwatch, (_, _) => { _mixer.IsVisible = !_mixer.IsVisible; _brush = Color.FromRgb(_cr, _cg, _cb); if (_tool == DrawTool.Eraser) _tool = DrawTool.Brush; RefreshTools(); });
            swatchRow.Add(_customSwatch);
            col.Add(HScroll(swatchRow));

            // Custom colour mixer (R/G/B sliders), hidden until "+" is tapped.
            _mixer = BuildMixer();
            col.Add(_mixer);

            // Actions
            var actions = new HorizontalStackLayout { Spacing = 8, Padding = new Thickness(2, 2) };
            _fillToggle = ActionChip("⬛ Fill shapes", () => { _fillShapes = !_fillShapes; RefreshTools(); });
            actions.Add(_fillToggle);
            actions.Add(ActionChip("↩ Undo", Undo));
            actions.Add(ActionChip("↪ Redo", Redo));
            actions.Add(ActionChip("🗑 Clear", () => { _paint.Ops.Clear(); _paint.Background = Colors.White; _redo.Clear(); _canvas.Invalidate(); AppSettings.Tap(); }));
            col.Add(HScroll(actions));

            RefreshTools();
            return col;
        }

        private VerticalStackLayout BuildMixer()
        {
            var box = new VerticalStackLayout { Spacing = 6, IsVisible = false, Padding = new Thickness(2, 4) };
            box.Add(SmallLabel("Mix your own colour"));
            Slider Make(int init, Color tint, Action<int> set)
            {
                var sl = new Slider { Minimum = 0, Maximum = 255, Value = init, MinimumTrackColor = tint, ThumbColor = tint };
                sl.ValueChanged += (_, e) => { set((int)e.NewValue); UpdateCustom(); };
                return sl;
            }
            box.Add(RowWith("R", Make(_cr, Colors.Red, v => _cr = v)));
            box.Add(RowWith("G", Make(_cg, Colors.Green, v => _cg = v)));
            box.Add(RowWith("B", Make(_cb, Colors.Blue, v => _cb = v)));
            return box;
        }

        private View RowWith(string letter, View slider)
        {
            var g = new Grid { ColumnSpacing = 8 };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            g.Add(new Label { Text = letter, FontFamily = Ui.Fonts, FontSize = Ui.S(13), FontAttributes = FontAttributes.Bold, TextColor = Theme.TextSecondary, VerticalOptions = LayoutOptions.Center }, 0, 0);
            g.Add(slider, 1, 0);
            return g;
        }

        private void UpdateCustom()
        {
            var c = Color.FromRgb(_cr, _cg, _cb);
            _customSwatch.BackgroundColor = c;
            _brush = c;
            if (_tool == DrawTool.Eraser) _tool = DrawTool.Brush;
            RefreshTools();
        }

        private static Label SmallLabel(string t) => new()
        {
            Text = t, FontFamily = Ui.Fonts, FontSize = Ui.S(11.5), FontAttributes = FontAttributes.Bold, TextColor = Theme.TextHint
        };

        private static ScrollView HScroll(View content) =>
            new() { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = content };

        private static Border ChromeChip(View content) => new()
        {
            Content = content, BackgroundColor = Theme.Surface, StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = 12 }
        };

        private Border ActionChip(string text, Action tap)
        {
            var b = new Border
            {
                Content = new Label { Text = text, FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), FontAttributes = FontAttributes.Bold, TextColor = Theme.TextSecondary, VerticalTextAlignment = TextAlignment.Center },
                BackgroundColor = Theme.Surface, Stroke = Theme.HairlineSoft, StrokeThickness = 2,
                StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = new Thickness(12, 9)
            };
            Ui.OnTap(b, (_, _) => tap());
            return b;
        }

        private void Undo()
        {
            if (_paint.Ops.Count == 0) return;
            var last = _paint.Ops[^1];
            _paint.Ops.RemoveAt(_paint.Ops.Count - 1);
            _redo.Push(last);
            _canvas.Invalidate();
            AppSettings.Tap();
        }

        private void Redo()
        {
            if (_redo.Count == 0) return;
            _paint.Ops.Add(_redo.Pop());
            _canvas.Invalidate();
            AppSettings.Tap();
        }

        // Repaints selection rings so the current tool/size/colour is obvious.
        private void RefreshTools()
        {
            foreach (var (b, tool) in _toolBtns)
            {
                bool on = _tool == tool;
                b.Stroke = on ? Theme.Accent : Theme.HairlineSoft;
                b.BackgroundColor = on ? Ui.MultiplyAlpha(Theme.Accent, 0.14f) : Theme.Surface;
            }
            foreach (var (b, size) in _sizes)
                b.Stroke = Math.Abs(size - _brushSize) < 0.1f ? Theme.Accent : Theme.HairlineSoft;
            foreach (var (b, color) in _swatches)
                b.Stroke = (_tool != DrawTool.Eraser && ColorsEqual(color, _brush)) ? Theme.Accent : Color.FromArgb("#22000000");
            _customSwatch.Stroke = (_tool != DrawTool.Eraser && ColorsEqual(Color.FromRgb(_cr, _cg, _cb), _brush)) ? Theme.Accent : Color.FromArgb("#22000000");
            if (_fillToggle != null)
            {
                _fillToggle.Stroke = _fillShapes ? Theme.Accent : Theme.HairlineSoft;
                _fillToggle.BackgroundColor = _fillShapes ? Ui.MultiplyAlpha(Theme.Accent, 0.14f) : Theme.Surface;
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

                _formArea.Add(Ui.SectionHeader("Its story"));
                _formArea.Add(Field("about", "About", _c.About, "Describe your creation…", multiline: true));
                _formArea.Add(Field("features", "Key features", _c.KeyFeatures, "Claws, horns, armour, feathers…", multiline: true));
                _formArea.Add(Field("habitat", "Habitat & environment", _c.Habitat, "Where and when it lived…", multiline: true));
                _formArea.Add(Field("behaviour", "Behaviour", _c.Behaviour, "How it hunted, moved, or defended itself…", multiline: true));
            }
            else
            {
                _formArea.Add(Field("short", "Subtitle", string.IsNullOrWhiteSpace(_c.Subtitle) ? _c.ShortDescription : _c.Subtitle, "a short catchy line"));
                _formArea.Add(Picker("type", "What kind is it?", new[] { "Planet", "Moon", "Star", "Galaxy", "Nebula", "Comet", "Asteroid", "Black hole" }, string.IsNullOrWhiteSpace(_c.TypeLabel) ? "Planet" : _c.TypeLabel));

                _formArea.Add(Ui.SectionHeader("Facts (label + value)"));
                _formArea.Add(StatPair("s1", _c.Stat1Label, _c.Stat1Value, "Diameter", "e.g. 12,000 km"));
                _formArea.Add(StatPair("s2", _c.Stat2Label, _c.Stat2Value, "Distance", "e.g. 200 million km"));
                _formArea.Add(StatPair("s3", _c.Stat3Label, _c.Stat3Value, "Temperature", "e.g. -100°C"));
                _formArea.Add(StatPair("s4", _c.Stat4Label, _c.Stat4Value, "Moons", "e.g. 3"));

                _formArea.Add(Ui.SectionHeader("Its story"));
                _formArea.Add(Field("about", "About", _c.About, "Describe your creation…", multiline: true));
                _formArea.Add(Field("features", "Key features", _c.SpaceKeyFeatures, "What makes it special…", multiline: true));
                _formArea.Add(Field("orbit", "Orbit & movement", _c.OrbitMovement, "How it moves through space…", multiline: true));
                _formArea.Add(Field("surface", "Surface & composition", _c.SurfaceComposition, "What it's made of…", multiline: true));
                _formArea.Add(Field("history", "History", _c.History, "How it was found or formed…", multiline: true));
                _formArea.Add(Field("inside", "What's inside", _c.WhatsInside, "Its core, layers, or contents…", multiline: true));
            }

            _formArea.Add(Ui.SectionHeader("Fun facts"));
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

        private View StatPair(string key, string labelVal, string valueVal, string labelHint, string valueHint)
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
                _c.KeyFeatures = F("features");
                _c.Habitat = F("habitat");
                _c.Behaviour = F("behaviour");
            }
            else
            {
                _c.Subtitle = F("short");
                _c.TypeLabel = P("type");
                _c.Stat1Label = F("s1L"); _c.Stat1Value = F("s1V");
                _c.Stat2Label = F("s2L"); _c.Stat2Value = F("s2V");
                _c.Stat3Label = F("s3L"); _c.Stat3Value = F("s3V");
                _c.Stat4Label = F("s4L"); _c.Stat4Value = F("s4V");
                _c.SpaceKeyFeatures = F("features");
                _c.OrbitMovement = F("orbit");
                _c.SurfaceComposition = F("surface");
                _c.History = F("history");
                _c.WhatsInside = F("inside");
            }

            if (_c.CreatedTicks == 0) _c.CreatedTicks = DateTime.UtcNow.Ticks;

            // Export the drawing to a PNG. If they didn't draw anything on an
            // edit, keep the picture they already had.
            if (_paint.Ops.Count > 0 || _paint.Background != Colors.White)
            {
                string path = CreationStore.NewImagePath(_c.Id);
                double density = 2.75;
                try { density = DeviceDisplay.MainDisplayInfo.Density; } catch { }
                bool ok = CreationCanvas.ExportPng(_paint, _canvas.Width, _canvas.Height, density, path);
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

    public enum DrawTool { Brush, Line, Rectangle, Ellipse, Triangle, Star, Arrow, Fill, Eraser }

    // One drawing operation — a freehand stroke or a shape.
    public class DrawOp
    {
        public DrawTool Tool { get; set; }
        public Color Color { get; set; } = Colors.Black;
        public float Width { get; set; } = 8;
        public bool Fill { get; set; }
        public List<PointF> Points { get; set; } = new();   // freehand / eraser
        public PointF Start { get; set; }
        public PointF End { get; set; }
    }

    // Renders the operations onto the on-screen canvas. The same geometry is
    // used by the PNG exporter so what you draw is exactly what you save.
    public class PaintDrawable : IDrawable
    {
        public List<DrawOp> Ops { get; } = new();
        public Color Background { get; set; } = Colors.White;

        public void Draw(ICanvas canvas, RectF rect)
        {
            canvas.Antialias = true;
            canvas.FillColor = Background;
            canvas.FillRectangle(rect);
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;

            foreach (var op in Ops)
            {
                canvas.StrokeColor = op.Color;
                canvas.FillColor = op.Color;
                canvas.StrokeSize = op.Width;

                switch (op.Tool)
                {
                    case DrawTool.Brush:
                    case DrawTool.Eraser:
                        if (op.Points.Count == 1)
                            canvas.FillCircle(op.Points[0].X, op.Points[0].Y, op.Width / 2);
                        else if (op.Points.Count > 1)
                        {
                            var path = new PathF();
                            path.MoveTo(op.Points[0].X, op.Points[0].Y);
                            for (int i = 1; i < op.Points.Count; i++) path.LineTo(op.Points[i].X, op.Points[i].Y);
                            canvas.DrawPath(path);
                        }
                        break;
                    case DrawTool.Line:
                        canvas.DrawLine(op.Start.X, op.Start.Y, op.End.X, op.End.Y);
                        break;
                    case DrawTool.Arrow:
                        DrawArrow(canvas, op);
                        break;
                    case DrawTool.Rectangle:
                    {
                        var r = Norm(op.Start, op.End);
                        if (op.Fill) canvas.FillRectangle(r); else canvas.DrawRectangle(r);
                        break;
                    }
                    case DrawTool.Ellipse:
                    {
                        var r = Norm(op.Start, op.End);
                        if (op.Fill) canvas.FillEllipse(r); else canvas.DrawEllipse(r);
                        break;
                    }
                    case DrawTool.Triangle:
                    case DrawTool.Star:
                    {
                        var pts = ShapeGeometry.Polygon(op.Tool, op.Start, op.End);
                        var path = new PathF();
                        path.MoveTo(pts[0].X, pts[0].Y);
                        for (int i = 1; i < pts.Count; i++) path.LineTo(pts[i].X, pts[i].Y);
                        path.Close();
                        if (op.Fill) canvas.FillPath(path); else canvas.DrawPath(path);
                        break;
                    }
                }
            }
        }

        private static void DrawArrow(ICanvas canvas, DrawOp op)
        {
            canvas.DrawLine(op.Start.X, op.Start.Y, op.End.X, op.End.Y);
            foreach (var (x, y) in ShapeGeometry.ArrowHead(op.Start, op.End, op.Width))
                canvas.DrawLine(op.End.X, op.End.Y, x, y);
        }

        private static RectF Norm(PointF a, PointF b)
            => new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }

    // Geometry shared by the on-screen renderer and the PNG exporter.
    public static class ShapeGeometry
    {
        public static List<PointF> Polygon(DrawTool tool, PointF a, PointF b)
        {
            float minX = Math.Min(a.X, b.X), minY = Math.Min(a.Y, b.Y);
            float w = Math.Abs(a.X - b.X), h = Math.Abs(a.Y - b.Y);
            float cx = minX + w / 2, cy = minY + h / 2;

            if (tool == DrawTool.Triangle)
                return new List<PointF> { new(cx, minY), new(minX, minY + h), new(minX + w, minY + h) };

            // 5-point star
            var pts = new List<PointF>();
            float rx = w / 2, ry = h / 2;
            for (int i = 0; i < 10; i++)
            {
                double ang = -Math.PI / 2 + i * Math.PI / 5;
                float fr = (i % 2 == 0) ? 1f : 0.42f;
                pts.Add(new PointF(cx + (float)(Math.Cos(ang) * rx * fr), cy + (float)(Math.Sin(ang) * ry * fr)));
            }
            return pts;
        }

        public static (float x, float y)[] ArrowHead(PointF start, PointF end, float width)
        {
            double ang = Math.Atan2(end.Y - start.Y, end.X - start.X);
            float len = Math.Max(12, width * 3);
            double a1 = ang + Math.PI * 0.82, a2 = ang - Math.PI * 0.82;
            return new[]
            {
                (end.X + (float)(Math.Cos(a1) * len), end.Y + (float)(Math.Sin(a1) * len)),
                (end.X + (float)(Math.Cos(a2) * len), end.Y + (float)(Math.Sin(a2) * len)),
            };
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

    // Rasterises the drawing to a PNG. Android-native (the shipping platform);
    // a no-op elsewhere, where the gallery falls back to a placeholder.
    public static class CreationCanvas
    {
        public static bool ExportPng(PaintDrawable paint, double viewWidth, double viewHeight, double density, string path)
        {
#if ANDROID
            try
            {
                int w = Math.Max(1, (int)(viewWidth * density));
                int h = Math.Max(1, (int)(viewHeight * density));
                using var bitmap = Android.Graphics.Bitmap.CreateBitmap(w, h, Android.Graphics.Bitmap.Config.Argb8888!);
                using var acanvas = new Android.Graphics.Canvas(bitmap);
                acanvas.DrawColor(ToA(paint.Background));

                float d = (float)density;
                foreach (var op in paint.Ops)
                {
                    using var pnt = new Android.Graphics.Paint { AntiAlias = true };
                    pnt.Color = ToA(op.Color);
                    pnt.StrokeWidth = op.Width * d;
                    pnt.StrokeCap = Android.Graphics.Paint.Cap.Round;
                    pnt.StrokeJoin = Android.Graphics.Paint.Join.Round;
                    bool fill = op.Fill;
                    pnt.SetStyle(fill ? Android.Graphics.Paint.Style.Fill : Android.Graphics.Paint.Style.Stroke);

                    switch (op.Tool)
                    {
                        case DrawTool.Brush:
                        case DrawTool.Eraser:
                            pnt.SetStyle(Android.Graphics.Paint.Style.Stroke);
                            if (op.Points.Count == 1)
                            {
                                pnt.SetStyle(Android.Graphics.Paint.Style.Fill);
                                acanvas.DrawCircle(op.Points[0].X * d, op.Points[0].Y * d, op.Width * d / 2, pnt);
                            }
                            else if (op.Points.Count > 1)
                            {
                                using var ap = new Android.Graphics.Path();
                                ap.MoveTo(op.Points[0].X * d, op.Points[0].Y * d);
                                for (int i = 1; i < op.Points.Count; i++) ap.LineTo(op.Points[i].X * d, op.Points[i].Y * d);
                                acanvas.DrawPath(ap, pnt);
                            }
                            break;
                        case DrawTool.Line:
                            acanvas.DrawLine(op.Start.X * d, op.Start.Y * d, op.End.X * d, op.End.Y * d, pnt);
                            break;
                        case DrawTool.Arrow:
                            acanvas.DrawLine(op.Start.X * d, op.Start.Y * d, op.End.X * d, op.End.Y * d, pnt);
                            foreach (var (x, y) in ShapeGeometry.ArrowHead(op.Start, op.End, op.Width))
                                acanvas.DrawLine(op.End.X * d, op.End.Y * d, x * d, y * d, pnt);
                            break;
                        case DrawTool.Rectangle:
                        {
                            float l = Math.Min(op.Start.X, op.End.X) * d, t = Math.Min(op.Start.Y, op.End.Y) * d;
                            float r = Math.Max(op.Start.X, op.End.X) * d, bt = Math.Max(op.Start.Y, op.End.Y) * d;
                            acanvas.DrawRect(l, t, r, bt, pnt);
                            break;
                        }
                        case DrawTool.Ellipse:
                        {
                            float l = Math.Min(op.Start.X, op.End.X) * d, t = Math.Min(op.Start.Y, op.End.Y) * d;
                            float r = Math.Max(op.Start.X, op.End.X) * d, bt = Math.Max(op.Start.Y, op.End.Y) * d;
                            acanvas.DrawOval(new Android.Graphics.RectF(l, t, r, bt), pnt);
                            break;
                        }
                        case DrawTool.Triangle:
                        case DrawTool.Star:
                        {
                            var pts = ShapeGeometry.Polygon(op.Tool, op.Start, op.End);
                            using var ap = new Android.Graphics.Path();
                            ap.MoveTo(pts[0].X * d, pts[0].Y * d);
                            for (int i = 1; i < pts.Count; i++) ap.LineTo(pts[i].X * d, pts[i].Y * d);
                            ap.Close();
                            acanvas.DrawPath(ap, pnt);
                            break;
                        }
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

#if ANDROID
        private static Android.Graphics.Color ToA(Color c) =>
            new Android.Graphics.Color((byte)(c.Red * 255), (byte)(c.Green * 255), (byte)(c.Blue * 255), (byte)(c.Alpha * 255));
#endif
    }
}
