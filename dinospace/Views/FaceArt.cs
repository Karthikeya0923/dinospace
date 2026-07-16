using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The face-finder behind every little round thumbnail. Entry art is a
    // wide full-body illustration on a transparent background — beautiful on
    // the entry page, but a 50 dp circle wants the FACE. This engine decodes
    // each PNG's alpha once, takes the creature's bounding box, and works out
    // where the head is:
    //
    //   • Compact art (planets, moons, nebulae — box nearly square) shows
    //     whole, centred. Saturn's thumb is simply Saturn.
    //   • Side-profile art (every dinosaur) gets a head-end crop: of the two
    //     horizontal ends of the creature, the head end carries far more bulk
    //     than the thin, tapering tail — so crop a square around the centre
    //     of mass of that end, nudged upward toward the skull.
    //
    // Results are cached (decoded once per image, ever), and a tiny override
    // table pins any pose the heuristic can't know about. Art that hasn't
    // arrived yet simply draws nothing, so the letter tile behind stays
    // visible — the tile is now ONLY a missing-art placeholder.
    public static class FaceArt
    {
        // Hand overrides: image base name -> focal centre (cx, cy) and crop
        // side, all normalized to the image (cx/cy in 0..1 of width/height,
        // side as a fraction of image height). Values were tuned against the
        // current art; if a file is ever replaced with a different pose, the
        // override is ignored automatically (see the coverage check below)
        // and the automatic head-finder takes over.
        private static readonly Dictionary<string, (float cx, float cy, float side)> Overrides = new()
        {
            ["allosaurus"] = (0.875f, 0.22f, 0.34f),
            ["ankylosaurus"] = (0.87f, 0.52f, 0.28f),
            ["apatosaurus"] = (0.935f, 0.25f, 0.20f),
            ["archaeopteryx"] = (0.895f, 0.21f, 0.36f),
            ["argentinosaurus"] = (0.91f, 0.245f, 0.20f),
            ["baryonyx"] = (0.89f, 0.285f, 0.38f),
            ["brachiosaurus"] = (0.90f, 0.115f, 0.20f),
            ["carcharodontosaurus"] = (0.89f, 0.28f, 0.38f),
            ["carnotaurus"] = (0.885f, 0.28f, 0.40f),
            ["compsognathus"] = (0.875f, 0.265f, 0.30f),
            ["deinonychus"] = (0.90f, 0.215f, 0.36f),
            ["deinosuchus"] = (0.88f, 0.44f, 0.34f),
            ["dilophosaurus"] = (0.86f, 0.25f, 0.44f),
            ["dimetrodon"] = (0.855f, 0.50f, 0.30f),
            ["diplodocus"] = (0.96f, 0.35f, 0.18f),
            ["dreadnoughtus"] = (0.945f, 0.245f, 0.20f),
            ["elasmosaurus"] = (0.935f, 0.175f, 0.22f),
            ["gallimimus"] = (0.85f, 0.14f, 0.28f),
            ["iguanodon"] = (0.875f, 0.33f, 0.28f),
            ["kronosaurus"] = (0.82f, 0.42f, 0.34f),
            ["liopleurodon"] = (0.84f, 0.46f, 0.34f),
            ["mosasaurus"] = (0.85f, 0.31f, 0.32f),
            ["plesiosaurus"] = (0.93f, 0.32f, 0.22f),
            ["pteranodon"] = (0.665f, 0.35f, 0.34f),
            ["stegosaurus"] = (0.875f, 0.53f, 0.30f),
            ["therizinosaurus"] = (0.715f, 0.10f, 0.22f),
            ["titanoboa"] = (0.895f, 0.45f, 0.26f),
            ["titanosaurus"] = (0.945f, 0.20f, 0.20f),
            ["trex"] = (0.855f, 0.29f, 0.44f),
            ["woollymammoth"] = (0.70f, 0.35f, 0.40f),
        };

        public sealed record Face(Microsoft.Maui.Graphics.IImage Image, RectF CropPx);

        private static readonly ConcurrentDictionary<string, Lazy<Face?>> _cache = new(StringComparer.OrdinalIgnoreCase);

        public static string BaseName(string imageFile)
        {
            string n = imageFile ?? "";
            int slash = Math.Max(n.LastIndexOf('/'), n.LastIndexOf('\\'));
            if (slash >= 0) n = n[(slash + 1)..];
            return n.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? n[..^4] : n;
        }

        // True when this is a bundled entry image name rather than a file
        // path (user drawings live at full paths and keep their own renderer).
        public static bool IsResourceName(string imageFile)
            => !string.IsNullOrEmpty(imageFile) && !imageFile.Contains('/') && !imageFile.Contains('\\');

        public static Face? Get(string imageFile)
        {
            if (!IsResourceName(imageFile)) return null;
            string name = BaseName(imageFile);
            if (name.Length == 0) return null;
            return _cache.GetOrAdd(name, n => new Lazy<Face?>(() => Decode(n))).Value;
        }

        public static bool TryGetCached(string imageFile, out Face? face)
        {
            face = null;
            if (!IsResourceName(imageFile)) return false;
            if (_cache.TryGetValue(BaseName(imageFile), out var lazy) && lazy.IsValueCreated)
            {
                face = lazy.Value;
                return true;
            }
            return false;
        }

        // Only creatures get face crops; space art (a planet, a nebula, a
        // space telescope) always shows whole — Saturn's thumb is Saturn.
        private static readonly Lazy<HashSet<string>> CreatureNames = new(() =>
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in dinospace.Data.DinoData.All) set.Add(BaseName(d.ImageFile));
            return set;
        });

        // Warms every encyclopedia face once per launch (App kicks this off in
        // the background), so the list scrolls with zero pop-in even on a cold
        // start. Cheap on every launch after the first: each face is a tiny
        // pre-cropped PNG in the cache dir (see LoadThumb / SaveThumb).
        public static void WarmAll()
        {
            try
            {
                foreach (var d in dinospace.Data.DinoData.All) Get(d.ImageFile);
                foreach (var s in dinospace.Data.SpaceData.All) Get(s.ImageFile);
            }
            catch { }
        }

#if ANDROID
        // Face thumbs persist across launches: the first decode of each entry
        // renders its crop window into a small square (≤192 px) and saves it
        // in the app's cache dir, so a cold start re-reads tiny PNGs instead
        // of re-running the head-finder over every 1280 px illustration. The
        // folder is stamped with the APK's LastUpdateTime — any reinstall
        // (which is how new art arrives) throws the cache away and it
        // rebuilds in the background.
        private const int ThumbMax = 192;

        private static readonly Lazy<string?> ThumbDir = new(() =>
        {
            try
            {
                var ctx = Android.App.Application.Context;
                long stamp = ctx.PackageManager?.GetPackageInfo(ctx.PackageName!, 0)?.LastUpdateTime ?? 0;
                string dir = System.IO.Path.Combine(ctx.CacheDir!.AbsolutePath, "facethumbs");
                string stampFile = System.IO.Path.Combine(dir, ".stamp");
                System.IO.Directory.CreateDirectory(dir);
                string want = stamp.ToString();
                if (!System.IO.File.Exists(stampFile) || System.IO.File.ReadAllText(stampFile) != want)
                {
                    foreach (var f in System.IO.Directory.GetFiles(dir)) System.IO.File.Delete(f);
                    System.IO.File.WriteAllText(stampFile, want);
                }
                return dir;
            }
            catch { return null; }
        });

        private static Face? LoadThumb(string baseName)
        {
            try
            {
                if (ThumbDir.Value is not string dir) return null;
                string path = System.IO.Path.Combine(dir, baseName + ".png");
                if (!System.IO.File.Exists(path)) return null;
                using var fs = System.IO.File.OpenRead(path);
                var img = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(fs);
                return img == null ? null : new Face(img, new RectF(0, 0, img.Width, img.Height));
            }
            catch { return null; }
        }

        private static void SaveThumb(string baseName, byte[] png)
        {
            try
            {
                if (ThumbDir.Value is not string dir) return;
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, baseName + ".png"), png);
            }
            catch { }
        }

        private static Face? Decode(string baseName)
        {
            if (LoadThumb(baseName) is Face cached) return cached;
            try
            {
                var ctx = Android.App.Application.Context;
                int id = ctx.Resources?.GetIdentifier(baseName, "drawable", ctx.PackageName) ?? 0;
                if (id == 0) return null;

                // Decode at thumb resolution — full art is 1280 px wide and a
                // thumbnail needs nowhere near that.
                var bounds = new Android.Graphics.BitmapFactory.Options { InJustDecodeBounds = true };
                Android.Graphics.BitmapFactory.DecodeResource(ctx.Resources, id, bounds);
                int sample = 1;
                while (bounds.OutWidth / (sample * 2) >= 360) sample *= 2;
                var opts = new Android.Graphics.BitmapFactory.Options
                {
                    InSampleSize = sample,
                    InPreferredConfig = Android.Graphics.Bitmap.Config.Argb8888
                };
                using var bmp = Android.Graphics.BitmapFactory.DecodeResource(ctx.Resources, id, opts);
                if (bmp == null) return null;

                var crop = FindFace(bmp, baseName);

                // Render just the crop window into its own little bitmap —
                // that's all a thumbnail ever draws. It keeps ~40 KB per face
                // in memory instead of the whole illustration, and it's what
                // gets persisted for instant cold starts. Areas of the window
                // that fall outside the art stay transparent.
                float f = MathF.Min(1f, ThumbMax / MathF.Max(crop.Width, crop.Height));
                int tw = Math.Max(1, (int)MathF.Round(crop.Width * f));
                int th = Math.Max(1, (int)MathF.Round(crop.Height * f));
                using var thumb = Android.Graphics.Bitmap.CreateBitmap(tw, th, Android.Graphics.Bitmap.Config.Argb8888!);
                using (var canvas = new Android.Graphics.Canvas(thumb))
                using (var m = new Android.Graphics.Matrix())
                using (var paint = new Android.Graphics.Paint(Android.Graphics.PaintFlags.FilterBitmap))
                {
                    m.PostTranslate(-crop.X, -crop.Y);
                    m.PostScale(f, f);
                    canvas.DrawBitmap(bmp, m, paint);
                }

                using var ms = new System.IO.MemoryStream();
                thumb.Compress(Android.Graphics.Bitmap.CompressFormat.Png!, 100, ms);
                SaveThumb(baseName, ms.ToArray());
                ms.Position = 0;
                var img = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(ms);
                return img == null ? null : new Face(img, new RectF(0, 0, tw, th));
            }
            catch { return null; }
        }

        private static RectF FindFace(Android.Graphics.Bitmap bmp, string baseName)
        {
            int w = bmp.Width, h = bmp.Height;

            // Column-by-column opacity profile of the art.
            var px = new int[w * h];
            bmp.GetPixels(px, 0, w, 0, 0, w, h);

            // A hand override is only trusted while it still points at the
            // creature: if the crop square lands on (nearly) nothing but
            // transparency, the art behind this name has changed and the
            // automatic head-finder below takes over.
            if (Overrides.TryGetValue(baseName, out var o))
            {
                float os = o.side * h;
                var r = new RectF(o.cx * w - os / 2f, o.cy * h - os / 2f, os, os);
                int x0 = Math.Max(0, (int)r.Left), x1 = Math.Min(w - 1, (int)r.Right);
                int y0 = Math.Max(0, (int)r.Top), y1 = Math.Min(h - 1, (int)r.Bottom);
                long opaque = 0;
                for (int y = y0; y <= y1; y++)
                {
                    int row = y * w;
                    for (int x = x0; x <= x1; x++)
                        if (((px[row + x] >> 24) & 0xFF) >= 48) opaque++;
                }
                if (opaque >= 0.10f * os * os) return r;
            }
            var colCount = new int[w];
            var colYSum = new long[w];
            int minX = w, maxX = -1, minY = h, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    if (((px[row + x] >> 24) & 0xFF) < 48) continue;
                    colCount[x]++;
                    colYSum[x] += y;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
            if (maxX < 0) return new RectF(0, 0, w, h);   // fully transparent?

            float bw = maxX - minX + 1, bh = maxY - minY + 1;

            // Whole-object view: all space art, and any creature drawn
            // compactly enough that its box is nearly square.
            if (!CreatureNames.Value.Contains(baseName) || bw < 1.45f * bh)
            {
                float s = MathF.Max(bw, bh) * 1.08f;
                return new RectF(minX + bw / 2f - s / 2f, minY + bh / 2f - s / 2f, s, s);
            }

            // Side profile: the head end out-weighs the thin, tapering tail.
            int strip = Math.Max(1, (int)(bw * 0.24f));
            long massL = 0, massR = 0;
            for (int x = minX; x < minX + strip; x++) massL += colCount[x];
            for (int x = maxX - strip + 1; x <= maxX; x++) massR += colCount[x];
            bool right = massR >= massL;
            int sx0 = right ? maxX - strip + 1 : minX;
            int sx1 = right ? maxX : minX + strip - 1;

            // Within the head strip, the skull is the TOP of the shape — legs
            // and chest hang below, and on a sauropod the neck climbs to the
            // head. Centre on the mass of the strip's upper part only.
            int sTop = h, sBot = -1;
            for (int x = sx0; x <= sx1; x++)
            {
                if (colCount[x] == 0) continue;
                // column extents need a re-scan; cheap at thumb resolution
                for (int y = 0; y < h; y++)
                {
                    if (((px[y * w + x] >> 24) & 0xFF) < 48) continue;
                    if (y < sTop) sTop = y;
                    if (y > sBot) sBot = y;
                }
            }
            if (sBot < 0) return new RectF(minX, minY, bw, bh);

            float yCut = sTop + 0.38f * (sBot - sTop + 1);
            long mass = 0, xSum = 0, ySum = 0;
            for (int x = sx0; x <= sx1; x++)
            {
                for (int y = sTop; y <= (int)yCut; y++)
                {
                    if (((px[y * w + x] >> 24) & 0xFF) < 48) continue;
                    mass++; xSum += x; ySum += y;
                }
            }
            if (mass <= 0) return new RectF(minX, minY, bw, bh);
            float cx = xSum / (float)mass;
            float cy = ySum / (float)mass;

            float side = 0.58f * bh;
            return new RectF(cx - side / 2f, cy - side / 2f, side, side);
        }
#else
        private static Face? Decode(string baseName) => null;
#endif
    }

    // Draws a face crop: the source window CropPx of the image is mapped onto
    // the whole view, background painted only once art actually exists — so
    // an art-less entry keeps showing whatever placeholder sits behind.
    public sealed class FaceDrawable : IDrawable
    {
        public Microsoft.Maui.Graphics.IImage? Image;
        public RectF Crop;
        public Color Bg = Colors.Transparent;

        public void Draw(ICanvas canvas, RectF rect)
        {
            if (Image == null || Crop.Width <= 0) return;
            if (Bg.Alpha > 0) { canvas.FillColor = Bg; canvas.FillRectangle(rect); }
            float scale = rect.Width / Crop.Width;
            canvas.SaveState();
            canvas.ClipRectangle(rect);
            canvas.DrawImage(Image,
                rect.Left - Crop.X * scale,
                rect.Top - Crop.Y * scale,
                Image.Width * scale,
                Image.Height * scale);
            canvas.RestoreState();
        }
    }

    // The bindable thumbnail view. Give it an image name (or bind one inside
    // a CollectionView template) and it shows the face; while art is missing
    // it stays fully transparent so the letter tile behind remains visible.
    public class FaceThumbView : GraphicsView
    {
        public static readonly BindableProperty ImageNameProperty = BindableProperty.Create(
            nameof(ImageName), typeof(string), typeof(FaceThumbView), "",
            propertyChanged: (b, _, _) => ((FaceThumbView)b).Reload());

        public static readonly BindableProperty FaceBgProperty = BindableProperty.Create(
            nameof(FaceBg), typeof(Color), typeof(FaceThumbView), Colors.Transparent,
            propertyChanged: (b, _, _) => ((FaceThumbView)b).ApplyBg());

        private readonly FaceDrawable _drawable = new();

        public FaceThumbView()
        {
            Drawable = _drawable;
            BackgroundColor = Colors.Transparent;
            InputTransparent = true;
        }

        public string ImageName
        {
            get => (string)GetValue(ImageNameProperty);
            set => SetValue(ImageNameProperty, value);
        }

        public Color FaceBg
        {
            get => (Color)GetValue(FaceBgProperty);
            set => SetValue(FaceBgProperty, value);
        }

        private void ApplyBg()
        {
            _drawable.Bg = FaceBg;
            Invalidate();
        }

        private void Reload()
        {
            string name = ImageName;
            _drawable.Image = null;

            if (!FaceArt.IsResourceName(name) || string.IsNullOrEmpty(name))
            {
                Invalidate();
                return;
            }

            // Already decoded? Show it immediately — no flash of the letter
            // tile while scrolling back through a list.
            if (FaceArt.TryGetCached(name, out var cached))
            {
                Apply(cached);
                return;
            }

            Invalidate();
            System.Threading.Tasks.Task.Run(() =>
            {
                var face = FaceArt.Get(name);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (ImageName == name) Apply(face);   // template may have been recycled
                });
            });
        }

        private void Apply(FaceArt.Face? face)
        {
            _drawable.Image = face?.Image;
            _drawable.Crop = face?.CropPx ?? default;
            Invalidate();
        }
    }
}
