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
    // The drawing studio. Two clean steps a young child can follow:
    //
    //   1. DRAW  — a big white canvas that fills the screen with a row of real
    //              brushes (pencil, marker, highlighter, glow, rainbow), an
    //              eraser and a fill bucket, a colour palette, brush sizes, and
    //              undo/redo. The canvas is NOT inside a scroll view, so drags
    //              become smooth strokes instead of dots (the old bug: a parent
    //              ScrollView stole the vertical drag on Android).
    //
    //   2. DETAILS — name it and give it stats, so a creation looks exactly like
    //                a real encyclopedia entry and dinosaurs can enter battle.
    //
    // Splitting the two also means the details form can scroll freely without
    // ever fighting the canvas for the same gesture.
    public class CreationEditorPage : ContentPage
    {
        private readonly UserCreation _c;
        private readonly bool _isNew;

        // Canvas state.
        private readonly PaintDrawable _paint = new();
        private GraphicsView _canvas = null!;
        private Stroke? _current;
        private readonly Stack<Stroke> _redo = new();

        // Tools.
        private BrushKind _brush = BrushKind.Marker;
        private Color _color = Color.FromArgb("#2B2B33");
        private float _size = 9;
        private readonly Random _rng = new();

        // The studio is always a bright, neutral space — the same on every app
        // theme. This is deliberate: on a dark theme the swatches and brush
        // previews used to vanish against the near-black surface. A light tray,
        // like every real paint app, keeps every colour clearly visible.
        private static readonly Color StudioBg = Color.FromArgb("#E9ECF1");
        private static readonly Color StudioCard = Colors.White;
        private static readonly Color StudioText = Color.FromArgb("#2B2B33");
        private static readonly Color StudioHint = Color.FromArgb("#6B7280");
        private static readonly Color StudioLine = Color.FromArgb("#D2D7E0");
        private static readonly Color StudioAccent = Color.FromArgb("#3E9BFF");

        // Toolbar pieces we repaint on change.
        private readonly List<(Border border, Color color)> _swatches = new();
        private readonly List<(Border border, float size)> _sizes = new();
        private readonly List<(Border border, BrushKind kind, GraphicsView preview)> _brushBtns = new();
        private Border _customSwatch = null!;
        private VerticalStackLayout _mixer = null!;
        private int _cr = 120, _cg = 90, _cb = 220;

        // Two steps, swapped in place.
        private View _drawStep = null!;
        private View _detailsStep = null!;

        // The canvas size captured while it was on screen — used when exporting
        // the PNG from the details step, where the canvas is no longer laid out.
        private double _canvasW, _canvasH;

        // The form.
        private CreationKind _kind;
        private VerticalStackLayout _formArea = null!;
        private readonly Dictionary<string, Entry> _fields = new();
        private readonly Dictionary<string, string> _picks = new();

        public CreationEditorPage(UserCreation? existing = null)
        {
            _isNew = existing == null;
            _c = existing ?? new UserCreation { Id = Guid.NewGuid().ToString("N"), Kind = CreationKind.Dinosaur };
            _kind = _c.Kind;
            _drawStep = BuildDrawStep();
            _detailsStep = BuildDetailsStep();
            Content = _drawStep;
            // NOTE: no SwipeBack here — a left-to-right brush stroke must never
            // be mistaken for a back-swipe. The top-bar arrow handles going back.
        }

        // ================= STEP 1: DRAW =================
        private View BuildDrawStep()
        {
            _canvas = new GraphicsView { Drawable = _paint, BackgroundColor = Colors.White };
            WireCanvas();

            var frame = new Border
            {
                Content = _canvas,
                BackgroundColor = Colors.White,
                Stroke = StudioLine, StrokeThickness = 1.5,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Padding = 0,
                Margin = new Thickness(14, 6, 14, 10),
                Shadow = Theme.CardShadow()
            };

            var toolPanel = BuildToolPanel();

            var root = new Grid { BackgroundColor = StudioBg, RowSpacing = 0 };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // top bar
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });   // canvas
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // tools
            root.Add(DrawTopBar(), 0, 0);
            root.Add(frame, 0, 1);
            root.Add(toolPanel, 0, 2);

            // Lock to landscape for better drawing experience - wider canvas
#if ANDROID
            try
            {
                if (Platform.CurrentActivity is Android.App.Activity a)
                    a.RequestedOrientation = Android.Content.PM.ScreenOrientation.SensorLandscape;
            }
            catch { }
#endif

            return root;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Restore portrait when leaving
#if ANDROID
            try
            {
                if (Platform.CurrentActivity is Android.App.Activity a)
                    a.RequestedOrientation = Android.Content.PM.ScreenOrientation.Unspecified;
            }
            catch { }
#endif
        }

        private View DrawTopBar()
        {
            var back = new Border
            {
                Content = Ui.Icon(Ui.IconBack, 24, StudioText),
                BackgroundColor = Colors.Transparent, Stroke = Colors.Transparent,
                Padding = new Thickness(6, 8, 12, 8)
            };
            Ui.OnTap(back, async (_, _) => { try { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); } catch { } });
            Ui.Describe(back, "Go back");

            var title = new Label
            {
                Text = _isNew ? "Draw it!" : "Edit drawing",
                FontFamily = Ui.Display, FontSize = Ui.S(20), TextColor = StudioText,
                VerticalOptions = LayoutOptions.Center
            };

            var nextLabel = new Label { Text = "Next  ›", FontFamily = Ui.Fonts, FontSize = Ui.S(15), FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalTextAlignment = TextAlignment.Center };
            var next = new Border
            {
                Content = nextLabel, BackgroundColor = StudioAccent, Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(16, 9),
                VerticalOptions = LayoutOptions.Center
            };
            Ui.OnTap(next, (_, _) => ShowDetails());
            Ui.Describe(next, "Add details");

            var bar = new Grid { Padding = new Thickness(8, 8, 12, 4), ColumnSpacing = 4 };
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.Add(back, 0, 0);
            bar.Add(title, 1, 0);
            bar.Add(next, 2, 0);
            return bar;
        }

        // The canvas touch pipeline. GraphicsView's interaction events fire on
        // real finger drags on Android; with no scroll-view ancestor stealing
        // the gesture, every move between down and up becomes part of one
        // smooth stroke.
        private void WireCanvas()
        {
            _canvas.StartInteraction += (_, e) =>
            {
                var p = e.Touches.FirstOrDefault();
                _redo.Clear();

                if (_brush == BrushKind.Fill)
                {
                    _paint.Background = _color;
                    _canvas.Invalidate();
                    AppSettings.Tap();
                    return;
                }

                _current = new Stroke
                {
                    Kind = _brush,
                    Color = _color,
                    Width = _size,
                    HueStart = _rng.Next(360)
                };
                _current.Points.Add(p);
                _paint.Strokes.Add(_current);
                _canvas.Invalidate();
            };
            _canvas.DragInteraction += (_, e) =>
            {
                if (_current == null || e.Touches.Length == 0) return;
                // One pointer only — a second finger must not zig-zag the line.
                _current.Points.Add(e.Touches[0]);
                _current.Dirty = true;
                _canvas.Invalidate();
            };
            _canvas.EndInteraction += (_, e) =>
            {
                if (_current == null) return;
                var p = e.Touches.FirstOrDefault();
                if (p != default) _current.Points.Add(p);
                _current.Dirty = true;
                _current = null;
                _canvas.Invalidate();
            };
        }

        // ---------- tool panel ----------
        private View BuildToolPanel()
        {
            var col = new VerticalStackLayout { Spacing = 9, Padding = new Thickness(14, 4, 14, 14) };

            // Brushes + eraser + fill, each with a live little preview.
            _brushBtns.Clear();
            var brushRow = new HorizontalStackLayout { Spacing = 8, Padding = new Thickness(2, 2) };
            (BrushKind kind, string name)[] brushes =
            {
                (BrushKind.Pencil, "Pencil"), (BrushKind.Marker, "Marker"), (BrushKind.Highlighter, "Marker+"),
                (BrushKind.Glow, "Glow"), (BrushKind.Rainbow, "Rainbow"), (BrushKind.Eraser, "Eraser"), (BrushKind.Fill, "Fill"),
            };
            foreach (var (kind, name) in brushes)
                brushRow.Add(BrushChip(kind, name));
            col.Add(HScroll(brushRow));

            // Sizes.
            _sizes.Clear();
            var sizeRow = new HorizontalStackLayout { Spacing = 8, Padding = new Thickness(2, 0) };
            foreach (var s in new float[] { 4, 9, 18, 30 })
            {
                var preview = new GraphicsView { Drawable = new DotDrawable { Radius = Math.Min(s / 2f, 13), Fill = _color }, InputTransparent = true };
                var b = new Border
                {
                    WidthRequest = 48, HeightRequest = 40, BackgroundColor = StudioCard,
                    StrokeThickness = 2, Stroke = StudioLine,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Content = preview
                };
                float sz = s;
                Ui.OnTap(b, (_, _) => { _size = sz; RefreshTools(); });
                _sizes.Add((b, s));
                sizeRow.Add(b);
            }
            col.Add(HScroll(sizeRow));

            // Colours.
            _swatches.Clear();
            var swatchRow = new HorizontalStackLayout { Spacing = 9, Padding = new Thickness(2, 0) };
            string[] palette =
            {
                "#2B2B33", "#8A8A8A", "#FFFFFF", "#E23B3B", "#FF7A1A", "#FFD23B", "#8BE04E", "#2FA85A",
                "#20C4C4", "#3B82F6", "#1E3A8A", "#9B5DE5", "#EC4899", "#FF9EC4", "#8B5A2B", "#3D2A1A",
            };
            foreach (var hex in palette)
            {
                var swatchColor = Color.FromArgb(hex);
                var sw = new Border
                {
                    WidthRequest = 34, HeightRequest = 34, BackgroundColor = swatchColor,
                    StrokeThickness = 3, Stroke = Color.FromArgb("#22000000"),
                    StrokeShape = new RoundRectangle { CornerRadius = 17 }
                };
                Ui.OnTap(sw, (_, _) => PickColor(swatchColor));
                _swatches.Add((sw, swatchColor));
                swatchRow.Add(sw);
            }
            _customSwatch = new Border
            {
                WidthRequest = 34, HeightRequest = 34, BackgroundColor = Color.FromRgb(_cr, _cg, _cb),
                StrokeThickness = 3, Stroke = Color.FromArgb("#22000000"),
                StrokeShape = new RoundRectangle { CornerRadius = 17 },
                Content = new Label { Text = "🎨", FontSize = 15, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }
            };
            Ui.OnTap(_customSwatch, (_, _) => { _mixer.IsVisible = !_mixer.IsVisible; PickColor(Color.FromRgb(_cr, _cg, _cb)); });
            swatchRow.Add(_customSwatch);
            col.Add(HScroll(swatchRow));

            _mixer = BuildMixer();
            col.Add(_mixer);

            // Undo / redo / clear.
            var actions = new HorizontalStackLayout { Spacing = 8, Padding = new Thickness(2, 0) };
            actions.Add(ActionChip("↩  Undo", Undo));
            actions.Add(ActionChip("↪  Redo", Redo));
            actions.Add(ActionChip("🗑  Clear", ClearCanvas));
            col.Add(actions);

            RefreshTools();
            return col;
        }

        private View BrushChip(BrushKind kind, string name)
        {
            var preview = new GraphicsView
            {
                Drawable = new BrushPreviewDrawable { Kind = kind, Color = _color },
                HeightRequest = 22, WidthRequest = 56, InputTransparent = true
            };
            var label = new Label { Text = name, FontFamily = Ui.Fonts, FontSize = Ui.S(10.5), FontAttributes = FontAttributes.Bold, TextColor = StudioHint, HorizontalTextAlignment = TextAlignment.Center };
            var content = new VerticalStackLayout { Spacing = 2, Padding = new Thickness(6, 7), Children = { preview, label } };
            var b = new Border
            {
                Content = content, BackgroundColor = StudioCard,
                StrokeThickness = 2, Stroke = StudioLine,
                StrokeShape = new RoundRectangle { CornerRadius = 14 }
            };
            Ui.OnTap(b, (_, _) => { _brush = kind; RefreshTools(); });
            _brushBtns.Add((b, kind, preview));
            Ui.Describe(b, name);
            return b;
        }

        private VerticalStackLayout BuildMixer()
        {
            var box = new VerticalStackLayout { Spacing = 4, IsVisible = false, Padding = new Thickness(2, 2) };
            box.Add(new Label { Text = "Mix your own colour", FontFamily = Ui.Fonts, FontSize = Ui.S(11), FontAttributes = FontAttributes.Bold, TextColor = StudioHint });
            Slider Make(int init, Color tint, Action<int> set)
            {
                var sl = new Slider { Minimum = 0, Maximum = 255, Value = init, MinimumTrackColor = tint, ThumbColor = tint, HeightRequest = 28 };
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
            g.Add(new Label { Text = letter, FontFamily = Ui.Fonts, FontSize = Ui.S(13), FontAttributes = FontAttributes.Bold, TextColor = StudioText, VerticalOptions = LayoutOptions.Center, WidthRequest = 16 }, 0, 0);
            g.Add(slider, 1, 0);
            return g;
        }

        private void PickColor(Color c)
        {
            _color = c;
            if (_brush == BrushKind.Eraser) _brush = BrushKind.Marker;
            RefreshTools();
        }

        private void UpdateCustom()
        {
            var c = Color.FromRgb(_cr, _cg, _cb);
            _customSwatch.BackgroundColor = c;
            PickColor(c);
        }

        private static ScrollView HScroll(View content) =>
            new() { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = content };

        private Border ActionChip(string text, Action tap)
        {
            var b = new Border
            {
                Content = new Label { Text = text, FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), FontAttributes = FontAttributes.Bold, TextColor = StudioText, VerticalTextAlignment = TextAlignment.Center },
                BackgroundColor = StudioCard, Stroke = StudioLine, StrokeThickness = 2,
                StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = new Thickness(14, 9)
            };
            Ui.OnTap(b, (_, _) => tap());
            return b;
        }

        private void Undo()
        {
            if (_paint.Strokes.Count == 0) return;
            var last = _paint.Strokes[^1];
            _paint.Strokes.RemoveAt(_paint.Strokes.Count - 1);
            _redo.Push(last);
            _canvas.Invalidate();
            AppSettings.Tap();
        }

        private void Redo()
        {
            if (_redo.Count == 0) return;
            _paint.Strokes.Add(_redo.Pop());
            _canvas.Invalidate();
            AppSettings.Tap();
        }

        private void ClearCanvas()
        {
            _paint.Strokes.Clear();
            _paint.Background = Colors.White;
            _redo.Clear();
            _canvas.Invalidate();
            AppSettings.Tap();
        }

        // Repaints selection rings and keeps the previews in the current colour.
        private void RefreshTools()
        {
            foreach (var (b, kind, preview) in _brushBtns)
            {
                bool on = _brush == kind;
                b.Stroke = on ? StudioAccent : StudioLine;
                b.BackgroundColor = on ? StudioAccent.WithAlpha(0.14f) : StudioCard;
                if (preview.Drawable is BrushPreviewDrawable d) { d.Color = _color; preview.Invalidate(); }
            }
            foreach (var (b, size) in _sizes)
            {
                b.Stroke = Math.Abs(size - _size) < 0.1f ? StudioAccent : StudioLine;
                if (b.Content is GraphicsView gv && gv.Drawable is DotDrawable dd) { dd.Fill = _color; gv.Invalidate(); }
            }
            foreach (var (b, color) in _swatches)
                b.Stroke = (_brush != BrushKind.Eraser && ColorsEqual(color, _color)) ? StudioAccent : Color.FromArgb("#33000000");
            _customSwatch.Stroke = (_brush != BrushKind.Eraser && ColorsEqual(Color.FromRgb(_cr, _cg, _cb), _color)) ? StudioAccent : Color.FromArgb("#33000000");
            AppSettings.Tap();
        }

        private static bool ColorsEqual(Color a, Color b)
            => Math.Abs(a.Red - b.Red) < 0.01 && Math.Abs(a.Green - b.Green) < 0.01 && Math.Abs(a.Blue - b.Blue) < 0.01;

        // ================= STEP 2: DETAILS =================
        private void ShowDetails()
        {
            // CRITICAL: Capture the canvas dimensions BEFORE switching views.
            // The GraphicsView won't be measured after leaving this page,
            // so we must lock them down now for proper PNG export.
            if (_canvas.Width > 0 && _canvas.Height > 0) 
            { 
                _canvasW = _canvas.Width; 
                _canvasH = _canvas.Height; 
            }
            else
            {
                // Fallback to reasonable defaults if measurement failed
                _canvasW = 400;
                _canvasH = 600;
            }
            Content = _detailsStep;
            AppSettings.Tap();
        }
        private void ShowDraw() { Content = _drawStep; AppSettings.Tap(); }

        // Hardware back on the details step returns to the drawing, not out of
        // the editor — so a half-finished creation isn't lost by one tap.
        protected override bool OnBackButtonPressed()
        {
            if (Content == _detailsStep) { ShowDraw(); return true; }
            return base.OnBackButtonPressed();
        }

        private View BuildDetailsStep()
        {
            var stack = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(18, 6, 18, 30) };

            stack.Add(new Label
            {
                Text = "Add the details",
                FontFamily = Ui.Display, FontSize = Ui.S(26), TextColor = Theme.TextPrimary
            });
            stack.Add(new Label
            {
                Text = "Name your creation and give it stats. It'll join your collection — and your dinosaurs can even enter Dino Battle!",
                FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), LineHeight = 1.4, TextColor = Theme.TextSecondary
            });

            stack.Add(KindToggle());

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

            // Top bar back-arrow returns to the drawing step (not out of the page).
            var back = new Border
            {
                Content = Ui.Icon(Ui.IconBack, 24, Theme.TextPrimary),
                BackgroundColor = Colors.Transparent, Stroke = Colors.Transparent,
                Padding = new Thickness(6, 8, 12, 8), HorizontalOptions = LayoutOptions.Start
            };
            Ui.OnTap(back, (_, _) => ShowDraw());
            Ui.Describe(back, "Back to drawing");
            var backLabel = new Label { Text = "Back to drawing", FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), FontAttributes = FontAttributes.Bold, TextColor = Theme.TextSecondary, VerticalOptions = LayoutOptions.Center };
            Ui.OnTap(backLabel, (_, _) => ShowDraw());
            var bar = new HorizontalStackLayout { Padding = new Thickness(8, 6, 16, 2), Children = { back, backLabel } };

            var root = new Grid { RowSpacing = 0 };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            root.Add(bar, 0, 0);
            root.Add(new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never }, 0, 1);
            return Ui.PageRoot(root);
        }

        private View KindToggle()
        {
            var row = new Grid { ColumnSpacing = 10 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            Border Pill(string text, CreationKind kind)
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
                    StrokeShape = new RoundRectangle { CornerRadius = AppLayout.ButtonRadius }, Padding = new Thickness(10, 12)
                };
                return b;
            }

            var dino = Pill("🦖  Dinosaur", CreationKind.Dinosaur);
            var space = Pill("🪐  Space object", CreationKind.Space);
            Ui.OnTap(dino, (_, _) => { if (_kind != CreationKind.Dinosaur) { _kind = CreationKind.Dinosaur; RebuildKind(); } });
            Ui.OnTap(space, (_, _) => { if (_kind != CreationKind.Space) { _kind = CreationKind.Space; RebuildKind(); } });
            row.Add(dino, 0, 0);
            row.Add(space, 1, 0);
            return row;
        }

        // Rebuild just the details step so the kind pills repaint and the form
        // swaps between the dinosaur and space-object fields.
        private void RebuildKind()
        {
            _detailsStep = BuildDetailsStep();
            Content = _detailsStep;
            AppSettings.Tap();
        }

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
            if (_paint.Strokes.Count > 0 || _paint.Background != Colors.White)
            {
                double w = _canvasW > 0 ? _canvasW : _canvas.Width;
                double h = _canvasH > 0 ? _canvasH : _canvas.Height;
                if (w > 0 && h > 0)
                {
                    string path = CreationStore.NewImagePath(_c.Id);
                    double density = 2.75;
                    try { density = DeviceDisplay.MainDisplayInfo.Density; } catch { }
                    bool ok = CreationCanvas.ExportPng(_paint, w, h, density, path);
                    if (ok) _c.ImagePath = path;
                }
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
}
