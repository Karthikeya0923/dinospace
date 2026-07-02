This folder holds the NovaSaur model chunks that ship inside the Play Store
release as asset packs.

Before building the RELEASE .aab, run (from the repo root):

    python tools/split_model.py path\to\NovaSaur.litertlm

That creates NovaSaur.litertlm.part1 ... part4 here. The csproj picks them
up automatically. For day-to-day Debug builds, leave this folder without
part files - the app then falls back to its built-in downloader.

DO NOT commit the .part files to git (they're ~3 GB). Add this line to your
.gitignore:

    dinospace/NovaModelParts/*.part*
