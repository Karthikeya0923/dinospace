#!/usr/bin/env python3
"""
Splits the NovaSaur model into 1 GB chunks for Google Play asset packs.

Google Play caps each asset pack at 1.5 GB, so the ~3 GB model can't ship
as one file. This script cuts it into NovaSaur.litertlm.part1..partN
(1 GB each) inside dinospace/NovaModelParts/, where the csproj packages
each part into its own fast-follow asset pack. The app joins them back
into the real model file on first launch.

Run from the repo root before building the RELEASE .aab:

    python tools/split_model.py path/to/NovaSaur.litertlm

Run it again any time the model file changes.
"""

import os
import sys

CHUNK_BYTES = 1_000_000_000  # 1 GB per part (Play's per-pack cap is 1.5 GB)
MAX_PARTS = 4                # csproj declares packs novamodel1..novamodel4
OUT_DIR = os.path.join("dinospace", "NovaModelParts")


def main():
    if len(sys.argv) != 2:
        print(__doc__)
        sys.exit(1)

    src = sys.argv[1]
    if not os.path.isfile(src):
        print(f"Model file not found: {src}")
        sys.exit(1)
    if not os.path.isdir(OUT_DIR):
        print(f"Couldn't find {OUT_DIR} - run this from the repo root.")
        sys.exit(1)

    size = os.path.getsize(src)

    # The app detects "all chunks arrived" by the last chunk being smaller
    # than the others. If the model size were an exact multiple of the chunk
    # size, that signal would vanish - so nudge the chunk size down a hair.
    chunk_bytes = CHUNK_BYTES
    if size % chunk_bytes == 0:
        chunk_bytes -= 4096

    parts = (size + chunk_bytes - 1) // chunk_bytes
    if parts > MAX_PARTS:
        print(f"Model is {size/1e9:.1f} GB which needs {parts} parts, but the "
              f"csproj only declares {MAX_PARTS} asset packs (Play's limit for "
              f"all packs combined is 4 GB). Use a smaller model.")
        sys.exit(1)

    # Clear old parts so a smaller model never leaves stale extras behind.
    for f in os.listdir(OUT_DIR):
        if ".part" in f:
            os.remove(os.path.join(OUT_DIR, f))

    print(f"Splitting {src} ({size/1e9:.2f} GB) into {parts} part(s)...")
    with open(src, "rb") as inp:
        for i in range(1, parts + 1):
            out_path = os.path.join(OUT_DIR, f"NovaSaur.litertlm.part{i}")
            written = 0
            with open(out_path, "wb") as out:
                while written < chunk_bytes:
                    buf = inp.read(min(8 * 1024 * 1024, chunk_bytes - written))
                    if not buf:
                        break
                    out.write(buf)
                    written += len(buf)
            print(f"  wrote {out_path} ({written/1e9:.2f} GB)")

    total = sum(os.path.getsize(os.path.join(OUT_DIR, f))
                for f in os.listdir(OUT_DIR) if ".part" in f)
    print("Done." if total == size else "WARNING: sizes don't match!")
    print(f"Original: {size:,} bytes | Parts total: {total:,} bytes")


if __name__ == "__main__":
    main()
