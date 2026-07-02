#!/usr/bin/env python3
"""
DinoSpace image optimizer.

The app was slow to open pages because some PNGs were enormous
(mainbackground.png was 4000x7100 - a 113 MB bitmap once decoded!).
Android has to decode images at their full pixel size before showing
them, so oversized art = seconds of lag + huge memory use.

This script shrinks every PNG in Resources/Images to sizes that still
look perfect on phones but decode instantly:

  - *background*.png   -> fits inside 1440 x 2560  (full-screen art)
  - bookmark*.png      -> fits inside 256 x 256    (small icons)
  - everything else    -> fits inside 1280 x 1280  (entry art, logos)

It never upscales, keeps transparency, and only rewrites a file when
the result is actually smaller or meaningfully fewer pixels.

USE IT WHENEVER NEW IMAGES ARE ADDED (run from the repo root):
    pip install pillow
    python tools/optimize_images.py

It's safe to run repeatedly - already-optimized files are skipped.
"""

import os
import sys

try:
    from PIL import Image
except ImportError:
    print("Pillow is required:  pip install pillow")
    sys.exit(1)

IMAGES_DIR = os.path.join("dinospace", "Resources", "Images")
if not os.path.isdir(IMAGES_DIR):
    # also allow running from inside the dinospace project folder
    alt = os.path.join("Resources", "Images")
    if os.path.isdir(alt):
        IMAGES_DIR = alt
    else:
        print(f"Couldn't find {IMAGES_DIR} - run this from the repo root.")
        sys.exit(1)


def limits_for(name: str):
    n = name.lower()
    if "background" in n:
        return (1440, 2560)
    if n.startswith("bookmark"):
        return (256, 256)
    return (1280, 1280)


def optimize(path: str) -> str:
    name = os.path.basename(path)
    max_w, max_h = limits_for(name)

    im = Image.open(path)
    w, h = im.size
    scale = min(max_w / w, max_h / h, 1.0)
    new_w, new_h = max(1, int(w * scale)), max(1, int(h * scale))

    before = os.path.getsize(path)
    needs_resize = scale < 0.999

    if not needs_resize and before < 400_000:
        return f"  ok       {name} ({w}x{h}, {before//1024} KB)"

    # Work in a mode that keeps transparency if the image has any.
    has_alpha = im.mode in ("RGBA", "LA", "P") and (
        im.mode != "P" or "transparency" in im.info or im.convert("RGBA").getextrema()[3][0] < 255
    )
    work = im.convert("RGBA" if has_alpha else "RGB")
    if needs_resize:
        work = work.resize((new_w, new_h), Image.LANCZOS)

    # Try a palette version too and keep whichever file is smaller.
    tmp_rgba = path + ".opt1"
    tmp_pal = path + ".opt2"
    work.save(tmp_rgba, "PNG", optimize=True)
    size_rgba = os.path.getsize(tmp_rgba)
    size_pal = None
    try:
        pal = work.quantize(colors=256, method=Image.FASTOCTREE)
        pal.save(tmp_pal, "PNG", optimize=True)
        size_pal = os.path.getsize(tmp_pal)
    except Exception:
        pass

    # Palette wins only when clearly smaller (it can add banding to photos).
    use_pal = size_pal is not None and size_pal < size_rgba * 0.7 and "background" not in name.lower()
    chosen = tmp_pal if use_pal else tmp_rgba
    after = os.path.getsize(chosen)

    if after < before or needs_resize:
        os.replace(chosen, path)
        result = f"  shrunk   {name}: {w}x{h} -> {new_w}x{new_h}, {before//1024} KB -> {after//1024} KB"
    else:
        result = f"  ok       {name} ({w}x{h}, {before//1024} KB)"

    for t in (tmp_rgba, tmp_pal):
        if os.path.exists(t):
            os.remove(t)
    return result


def main():
    total_before = total_after = 0
    print(f"Optimizing PNGs in {IMAGES_DIR} ...")
    for f in sorted(os.listdir(IMAGES_DIR)):
        if not f.lower().endswith(".png"):
            continue
        p = os.path.join(IMAGES_DIR, f)
        total_before += os.path.getsize(p)
        print(optimize(p))
        total_after += os.path.getsize(p)
    print(f"\nTotal: {total_before/1024/1024:.1f} MB -> {total_after/1024/1024:.1f} MB")


if __name__ == "__main__":
    main()
