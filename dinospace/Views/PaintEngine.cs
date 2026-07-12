using System;
using System.Collections.Generic;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The brushes offered in the drawing studio. Each renders the same way on
    // the live canvas and in the exported PNG, so what a child draws is exactly
    // what they save.
    public enum BrushKind
    {
        Pencil,       // thin, crisp
        Marker,       // bold, solid (the default)
        Highlighter,  // wide and see-through, colours build up
        Glow,         // neon core with a soft halo
        Rainbow,      // colour shifts along the stroke
        Eraser,       // paints the background back in
        Fill          // flood the whole canvas
    }

    // One freehand stroke. Points are the raw finger samples; Smoothed() turns
    // them into a dense Catmull-Rom curve so lines look fluid, never jagged.
    public class Stroke
    {
        public BrushKind Kind;
        public Color Color = Colors.Black;
        public float Width = 9;
        public int HueStart;                     // rainbow phase
        public List<PointF> Points { get; } = new();
        public bool Dirty = true;                // set while the stroke is growing

        // Fill strokes: the flooded region as a canvas-sized transparent
        // image, so fill paints exactly the enclosed shape that was tapped —
        // never the whole canvas. Null region = flood the whole canvas
        // (nothing enclosed the tap).
        public byte[]? RegionPng;
        public Microsoft.Maui.Graphics.IImage? RegionImage;

        private List<PointF>? _smooth;
        private int _cachedCount = -1;

        public List<PointF> Smoothed()
        {
            if (_smooth == null || Dirty || _cachedCount != Points.Count)
            {
                _smooth = PaintMath.Smooth(Points);
                _cachedCount = Points.Count;
                Dirty = false;
            }
            return _smooth;
        }
    }

    // Geometry + colour maths shared by both renderers.
    public static class PaintMath
    {
        // Catmull-Rom spline through the sampled points, subdivided by distance
        // so long, fast swipes stay just as smooth as slow ones.
        public static List<PointF> Smooth(IReadOnlyList<PointF> pts)
        {
            int n = pts.Count;
            if (n <= 2) return new List<PointF>(pts);

            var outp = new List<PointF>(n * 6) { pts[0] };
            for (int i = 0; i < n - 1; i++)
            {
                var p0 = pts[Math.Max(0, i - 1)];
                var p1 = pts[i];
                var p2 = pts[i + 1];
                var p3 = pts[Math.Min(n - 1, i + 2)];
                float dx = p2.X - p1.X, dy = p2.Y - p1.Y;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                int steps = (int)Math.Clamp(dist / 6f, 2, 16);
                for (int j = 1; j <= steps; j++)
                {
                    float t = j / (float)steps;
                    outp.Add(CatmullRom(p0, p1, p2, p3, t));
                }
            }
            return outp;
        }

        private static PointF CatmullRom(PointF p0, PointF p1, PointF p2, PointF p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            float x = 0.5f * ((2 * p1.X) + (-p0.X + p2.X) * t +
                              (2 * p0.X - 5 * p1.X + 4 * p2.X - p3.X) * t2 +
                              (-p0.X + 3 * p1.X - 3 * p2.X + p3.X) * t3);
            float y = 0.5f * ((2 * p1.Y) + (-p0.Y + p2.Y) * t +
                              (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * t2 +
                              (-p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y) * t3);
            return new PointF(x, y);
        }

        public static float EffectiveWidth(Stroke s) => s.Kind switch
        {
            BrushKind.Pencil => s.Width * 0.55f,
            BrushKind.Highlighter => s.Width * 1.9f,
            BrushKind.Eraser => s.Width * 2.2f,
            _ => s.Width
        };

        // HSV -> RGB (all 0..1), so rainbow renders identically on every canvas.
        public static (float r, float g, float b) Hsv(float hDeg, float s, float v)
        {
            hDeg = ((hDeg % 360f) + 360f) % 360f;
            float c = v * s;
            float x = c * (1 - MathF.Abs((hDeg / 60f) % 2 - 1));
            float m = v - c;
            float r, g, b;
            if (hDeg < 60) { r = c; g = x; b = 0; }
            else if (hDeg < 120) { r = x; g = c; b = 0; }
            else if (hDeg < 180) { r = 0; g = c; b = x; }
            else if (hDeg < 240) { r = 0; g = x; b = c; }
            else if (hDeg < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }
            return (r + m, g + m, b + m);
        }

        public static float RainbowHue(Stroke s, int index, int count)
            => s.HueStart + (count <= 1 ? 0 : index * 320f / count);

        public static Color Lighten(Color c, float t)
            => new(c.Red + (1 - c.Red) * t, c.Green + (1 - c.Green) * t, c.Blue + (1 - c.Blue) * t, 1f);
    }

    // On-screen rendering onto Microsoft.Maui.Graphics.
    public static class BrushEngine
    {
        public static void Draw(ICanvas canvas, Stroke s, Color background)
        {
            var pts = s.Smoothed();
            if (pts.Count == 0) return;

            Color col = s.Kind == BrushKind.Eraser ? background : s.Color;
            float w = PaintMath.EffectiveWidth(s);
            canvas.StrokeLineJoin = LineJoin.Round;
            canvas.StrokeLineCap = s.Kind == BrushKind.Highlighter ? LineCap.Square : LineCap.Round;

            if (pts.Count == 1)
            {
                var p = pts[0];
                canvas.FillColor = s.Kind == BrushKind.Rainbow ? Rgb(PaintMath.RainbowHue(s, 0, 1)) : Alpha(s, col);
                canvas.FillCircle(p.X, p.Y, Math.Max(1f, w / 2f));
                return;
            }

            switch (s.Kind)
            {
                case BrushKind.Glow:
                    canvas.StrokeColor = col.WithAlpha(0.16f); canvas.StrokeSize = w * 2.6f; StrokePath(canvas, pts);
                    canvas.StrokeColor = col.WithAlpha(0.28f); canvas.StrokeSize = w * 1.6f; StrokePath(canvas, pts);
                    canvas.StrokeColor = PaintMath.Lighten(col, 0.55f); canvas.StrokeSize = Math.Max(1.5f, w * 0.6f); StrokePath(canvas, pts);
                    break;

                case BrushKind.Rainbow:
                    canvas.StrokeSize = w;
                    for (int i = 1; i < pts.Count; i++)
                    {
                        canvas.StrokeColor = Rgb(PaintMath.RainbowHue(s, i, pts.Count));
                        canvas.DrawLine(pts[i - 1].X, pts[i - 1].Y, pts[i].X, pts[i].Y);
                    }
                    break;

                default:
                    canvas.StrokeColor = Alpha(s, col);
                    canvas.StrokeSize = w;
                    StrokePath(canvas, pts);
                    break;
            }
        }

        private static void StrokePath(ICanvas canvas, List<PointF> pts)
        {
            var path = new PathF();
            path.MoveTo(pts[0].X, pts[0].Y);
            for (int i = 1; i < pts.Count; i++) path.LineTo(pts[i].X, pts[i].Y);
            canvas.DrawPath(path);
        }

        private static Color Alpha(Stroke s, Color col) =>
            s.Kind == BrushKind.Highlighter ? col.WithAlpha(0.32f) : col;

        private static Color Rgb(float hue)
        {
            var (r, g, b) = PaintMath.Hsv(hue, 0.85f, 0.95f);
            return new Color(r, g, b);
        }
    }

    // Renders the whole drawing onto the on-screen canvas. Background is the
    // white paper the child draws on; the EXPORTED PNG bakes that paper in,
    // so a saved creation shows exactly what the canvas showed — white by
    // default, or whatever colour the background was painted.
    public class PaintDrawable : IDrawable
    {
        public List<Stroke> Strokes { get; } = new();
        public Color Background { get; set; } = Colors.White;

        // When editing an existing creation, its saved PNG loads here and the
        // new strokes draw on top — so editing continues the drawing instead
        // of starting over. BaseBytes feeds the Android rasteriser (export and
        // flood fill); BaseImage is the same picture for the live canvas.
        public byte[]? BaseBytes;
        public Microsoft.Maui.Graphics.IImage? BaseImage;

        // The colour currently underneath everything: the last whole-canvas
        // fill if there is one, else the paper. It's what the eraser paints
        // and what letterboxes behind the saved art.
        public Color EffectiveBackground
        {
            get
            {
                var bg = Background;
                foreach (var s in Strokes)
                    if (s.Kind == BrushKind.Fill && s.RegionPng == null) bg = s.Color;
                return bg;
            }
        }

        public void Draw(ICanvas canvas, RectF rect)
        {
            canvas.Antialias = true;
            canvas.FillColor = Background;
            canvas.FillRectangle(rect);
            if (BaseImage != null)
                canvas.DrawImage(BaseImage, rect.Left, rect.Top, rect.Width, rect.Height);

            // The eraser paints whatever is underneath at that point in the
            // stroke history — the paper, or the last background fill.
            var bg = Background;
            foreach (var s in Strokes)
            {
                if (s.Kind == BrushKind.Fill)
                {
                    if (s.RegionImage != null)
                        canvas.DrawImage(s.RegionImage, rect.Left, rect.Top, rect.Width, rect.Height);
                    else
                    {
                        canvas.FillColor = s.Color;
                        canvas.FillRectangle(rect);
                        bg = s.Color;
                    }
                }
                else BrushEngine.Draw(canvas, s, bg);
            }
        }
    }

    // Shrinks the live drawing to fit any smaller rect — the details step
    // shows it above the form, so the child sees exactly what they're saving
    // while they name it.
    public class PaintPreviewDrawable : IDrawable
    {
        public PaintDrawable? Source;
        public double SrcW, SrcH;   // the drawing canvas size, in view units

        public void Draw(ICanvas canvas, RectF rect)
        {
            if (Source == null || SrcW <= 0 || SrcH <= 0) return;
            canvas.Antialias = true;
            float s = Math.Min(rect.Width / (float)SrcW, rect.Height / (float)SrcH);
            float w = (float)SrcW * s, h = (float)SrcH * s;
            canvas.SaveState();
            canvas.Translate(rect.Left + (rect.Width - w) / 2f, rect.Top + (rect.Height - h) / 2f);
            canvas.Scale(s, s);
            Source.Draw(canvas, new RectF(0, 0, (float)SrcW, (float)SrcH));
            canvas.RestoreState();
        }
    }

    // A little sample stroke shown on each brush button so it's obvious what
    // the brush does before you pick it.
    public class BrushPreviewDrawable : IDrawable
    {
        public BrushKind Kind = BrushKind.Marker;
        public Color Color = Colors.Black;

        public void Draw(ICanvas canvas, RectF rect)
        {
            canvas.Antialias = true;
            float midY = rect.Center.Y;
            float amp = rect.Height * 0.28f;
            var s = new Stroke { Kind = Kind, Color = Color, Width = 6, HueStart = 0 };
            int n = 14;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                float x = rect.Left + 4 + t * (rect.Width - 8);
                float y = midY - MathF.Sin(t * MathF.PI * 2f) * amp;
                s.Points.Add(new PointF(x, y));
            }
            // Eraser preview reads as a chunky neutral stroke, not "invisible".
            if (Kind == BrushKind.Eraser) { s.Kind = BrushKind.Marker; s.Color = Color.FromArgb("#C7CBD1"); }
            BrushEngine.Draw(canvas, s, Colors.Transparent);
        }
    }

    // A dot preview for the brush-size buttons.
    public class DotDrawable : IDrawable
    {
        public double Radius = 4;
        public Color Fill = Color.FromArgb("#2B2B33");
        public void Draw(ICanvas canvas, RectF rect)
        {
            canvas.Antialias = true;
            canvas.FillColor = Fill;
            canvas.FillCircle(rect.Center.X, rect.Center.Y, (float)Radius);
        }
    }

    // Rasterises the drawing to a PNG. Android-native (the shipping platform);
    // a no-op elsewhere, where the gallery falls back to a placeholder. The
    // geometry mirrors BrushEngine exactly so the saved image matches the canvas.
    public static class CreationCanvas
    {
        // The saved PNG is EXACTLY what the canvas showed: the white paper
        // (or the painted background) baked in, the loaded base drawing when
        // editing, and every stroke on top — never a surprise background from
        // whatever page happens to display it.
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
                if (paint.BaseBytes != null)
                {
                    using var baseBmp = Android.Graphics.BitmapFactory.DecodeByteArray(paint.BaseBytes, 0, paint.BaseBytes.Length);
                    if (baseBmp != null)
                        acanvas.DrawBitmap(baseBmp, null, new Android.Graphics.Rect(0, 0, w, h), null);
                }

                float d = (float)density;
                var bg = paint.Background;
                foreach (var s in paint.Strokes)
                {
                    if (s.Kind == BrushKind.Fill)
                    {
                        if (s.RegionPng != null)
                        {
                            using var region = Android.Graphics.BitmapFactory.DecodeByteArray(s.RegionPng, 0, s.RegionPng.Length);
                            if (region != null)
                                acanvas.DrawBitmap(region, null, new Android.Graphics.Rect(0, 0, w, h), null);
                        }
                        else { acanvas.DrawColor(ToA(s.Color)); bg = s.Color; }
                        continue;
                    }
                    // The eraser paints the colour underneath it — same as on
                    // screen — so the export never punches see-through holes.
                    DrawStrokeA(acanvas, s, bg, d);
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

        // Flood-fills the enclosed region under the tap and returns it as a
        // fill stroke (undoable like any other stroke). The current drawing is
        // rasterised, the tapped colour-region is traced with a scanline fill,
        // and only those pixels become the fill — a circle fills as a circle.
        public static Stroke? MakeFillStroke(PaintDrawable paint, double viewWidth, double viewHeight, PointF tap, Color color)
        {
#if ANDROID
            try
            {
                int w = Math.Max(1, (int)viewWidth);
                int h = Math.Max(1, (int)viewHeight);
                int tx = Math.Clamp((int)tap.X, 0, w - 1);
                int ty = Math.Clamp((int)tap.Y, 0, h - 1);

                // rasterise the drawing as it looks on screen (paper, the
                // loaded base drawing when editing, then every stroke)
                using var snap = Android.Graphics.Bitmap.CreateBitmap(w, h, Android.Graphics.Bitmap.Config.Argb8888!);
                using (var c = new Android.Graphics.Canvas(snap))
                {
                    c.DrawColor(ToA(paint.Background));
                    if (paint.BaseBytes != null)
                    {
                        using var baseBmp = Android.Graphics.BitmapFactory.DecodeByteArray(paint.BaseBytes, 0, paint.BaseBytes.Length);
                        if (baseBmp != null)
                            c.DrawBitmap(baseBmp, null, new Android.Graphics.Rect(0, 0, w, h), null);
                    }
                    var snapBg = paint.Background;
                    foreach (var s in paint.Strokes)
                    {
                        if (s.Kind == BrushKind.Fill)
                        {
                            if (s.RegionPng != null)
                            {
                                using var region = Android.Graphics.BitmapFactory.DecodeByteArray(s.RegionPng, 0, s.RegionPng.Length);
                                if (region != null)
                                    c.DrawBitmap(region, null, new Android.Graphics.Rect(0, 0, w, h), null);
                            }
                            else { c.DrawColor(ToA(s.Color)); snapBg = s.Color; }
                        }
                        else DrawStrokeA(c, s, snapBg, 1f);
                    }
                }

                int[] px = new int[w * h];
                snap.GetPixels(px, 0, w, 0, 0, w, h);

                int target = px[ty * w + tx];
                const int tol = 48;
                bool Match(int p)
                {
                    int dr = ((p >> 16) & 0xFF) - ((target >> 16) & 0xFF);
                    int dg = ((p >> 8) & 0xFF) - ((target >> 8) & 0xFF);
                    int db = (p & 0xFF) - (target & 0xFF);
                    return dr > -tol && dr < tol && dg > -tol && dg < tol && db > -tol && db < tol;
                }

                int fill = (0xFF << 24) | ((int)(color.Red * 255) << 16) | ((int)(color.Green * 255) << 8) | (int)(color.Blue * 255);
                var mask = new int[w * h];
                var filled = new bool[w * h];
                var seen = new bool[w * h];
                var stack = new System.Collections.Generic.Stack<int>();
                stack.Push(ty * w + tx);
                seen[ty * w + tx] = true;
                int count = 0;
                while (stack.Count > 0)
                {
                    int i = stack.Pop();
                    if (!Match(px[i])) continue;
                    mask[i] = fill;
                    filled[i] = true;
                    count++;
                    int x = i % w, y = i / w;
                    if (x > 0 && !seen[i - 1]) { seen[i - 1] = true; stack.Push(i - 1); }
                    if (x < w - 1 && !seen[i + 1]) { seen[i + 1] = true; stack.Push(i + 1); }
                    if (y > 0 && !seen[i - w]) { seen[i - w] = true; stack.Push(i - w); }
                    if (y < h - 1 && !seen[i + w]) { seen[i + w] = true; stack.Push(i + w); }
                }
                if (count == 0) return null;

                // The flood reached (nearly) the whole canvas: nothing enclosed
                // the tap, so this is a background paint — return a clean
                // whole-canvas fill (the eraser then paints THIS colour).
                if ((long)count * 100 >= (long)w * h * 95)
                    return new Stroke { Kind = BrushKind.Fill, Color = color };

                // Grow the region two pixels outward so the fill tucks under
                // the antialiased edge of the outline — without this a pale
                // halo was left around every fill, worst against rainbow
                // strokes whose colours shift along the line.
                for (int pass = 0; pass < 2; pass++)
                {
                    var grow = new System.Collections.Generic.List<int>();
                    for (int i = 0; i < filled.Length; i++)
                    {
                        if (!filled[i]) continue;
                        int x = i % w, y = i / w;
                        if (x > 0 && !filled[i - 1]) grow.Add(i - 1);
                        if (x < w - 1 && !filled[i + 1]) grow.Add(i + 1);
                        if (y > 0 && !filled[i - w]) grow.Add(i - w);
                        if (y < h - 1 && !filled[i + w]) grow.Add(i + w);
                    }
                    foreach (int i in grow) { filled[i] = true; mask[i] = fill; }
                }

                using var regionBmp = Android.Graphics.Bitmap.CreateBitmap(w, h, Android.Graphics.Bitmap.Config.Argb8888!);
                regionBmp.SetPixels(mask, 0, w, 0, 0, w, h);
                using var ms = new System.IO.MemoryStream();
                regionBmp.Compress(Android.Graphics.Bitmap.CompressFormat.Png!, 100, ms);
                regionBmp.Recycle();
                byte[] png = ms.ToArray();

                Microsoft.Maui.Graphics.IImage? img = null;
                try
                {
                    using var rs = new System.IO.MemoryStream(png);
                    img = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(rs);
                }
                catch { }

                return new Stroke { Kind = BrushKind.Fill, Color = color, RegionPng = png, RegionImage = img };
            }
            catch { return null; }
#else
            // no rasteriser off-Android: fall back to a whole-canvas fill
            return new Stroke { Kind = BrushKind.Fill, Color = color };
#endif
        }

#if ANDROID
        private static void DrawStrokeA(Android.Graphics.Canvas canvas, Stroke s, Color background, float d)
        {
            var pts = s.Smoothed();
            if (pts.Count == 0) return;
            // The eraser paints the colour underneath it (paper or the last
            // background fill) — identical to how the live canvas renders it.
            Color col = s.Kind == BrushKind.Eraser ? background : s.Color;
            float w = PaintMath.EffectiveWidth(s) * d;

            using var pnt = new Android.Graphics.Paint { AntiAlias = true };
            pnt.SetStyle(Android.Graphics.Paint.Style.Stroke);
            pnt.StrokeCap = s.Kind == BrushKind.Highlighter ? Android.Graphics.Paint.Cap.Square : Android.Graphics.Paint.Cap.Round;
            pnt.StrokeJoin = Android.Graphics.Paint.Join.Round;

            if (pts.Count == 1)
            {
                pnt.SetStyle(Android.Graphics.Paint.Style.Fill);
                pnt.Color = s.Kind == BrushKind.Rainbow ? RgbA(PaintMath.RainbowHue(s, 0, 1)) : AlphaA(s, col);
                canvas.DrawCircle(pts[0].X * d, pts[0].Y * d, Math.Max(1f, w / 2f), pnt);
                return;
            }

            switch (s.Kind)
            {
                case BrushKind.Glow:
                    pnt.Color = ToA(col.WithAlpha(0.16f)); pnt.StrokeWidth = w * 2.6f; canvas.DrawPath(PathA(pts, d), pnt);
                    pnt.Color = ToA(col.WithAlpha(0.28f)); pnt.StrokeWidth = w * 1.6f; canvas.DrawPath(PathA(pts, d), pnt);
                    pnt.Color = ToA(PaintMath.Lighten(col, 0.55f)); pnt.StrokeWidth = Math.Max(1.5f, w * 0.6f); canvas.DrawPath(PathA(pts, d), pnt);
                    break;

                case BrushKind.Rainbow:
                    pnt.StrokeWidth = w;
                    for (int i = 1; i < pts.Count; i++)
                    {
                        pnt.Color = RgbA(PaintMath.RainbowHue(s, i, pts.Count));
                        canvas.DrawLine(pts[i - 1].X * d, pts[i - 1].Y * d, pts[i].X * d, pts[i].Y * d, pnt);
                    }
                    break;

                default:
                    pnt.Color = AlphaA(s, col);
                    pnt.StrokeWidth = w;
                    canvas.DrawPath(PathA(pts, d), pnt);
                    break;
            }
        }

        private static Android.Graphics.Path PathA(List<PointF> pts, float d)
        {
            var ap = new Android.Graphics.Path();
            ap.MoveTo(pts[0].X * d, pts[0].Y * d);
            for (int i = 1; i < pts.Count; i++) ap.LineTo(pts[i].X * d, pts[i].Y * d);
            return ap;
        }

        private static Android.Graphics.Color AlphaA(Stroke s, Color col)
            => s.Kind == BrushKind.Highlighter ? ToA(col.WithAlpha(0.32f)) : ToA(col);

        private static Android.Graphics.Color RgbA(float hue)
        {
            var (r, g, b) = PaintMath.Hsv(hue, 0.85f, 0.95f);
            return new Android.Graphics.Color((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), (byte)255);
        }

        private static Android.Graphics.Color ToA(Color c) =>
            new((byte)(c.Red * 255), (byte)(c.Green * 255), (byte)(c.Blue * 255), (byte)(c.Alpha * 255));
#endif
    }
}
