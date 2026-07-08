# DinoSpace

**A fully offline dinosaur & space encyclopedia for Android, with its own on-device AI and a live astronomy engine.** Built from scratch in C# with .NET MAUI.

DinoSpace blends the two things every kid (and plenty of adults) can't get enough of — dinosaurs and outer space — into one app that works with zero internet: a hand-written encyclopedia, an AI guide that runs entirely on the phone, and a personalized report of tonight's actual sky.

![Platform](https://img.shields.io/badge/platform-Android-green)
![Language](https://img.shields.io/badge/language-C%23-blue)
![Framework](https://img.shields.io/badge/framework-.NET%20MAUI-purple)
![Status](https://img.shields.io/badge/status-Play%20Store%20prep-orange)

(add screenshot of home screen — Native layout) (add screenshot of home screen — Playful layout) (add screenshot of Sky Tonight) (add screenshot of Scan Sky in landscape with the camera + white star overlay) (add screenshot of NovaSaur chat) (add screenshot of a dino detail page) (add screenshot of Your Creations — the drawing studio) (add screenshot of Dino Battle with "Include my creatures" on) (add screenshot of a quiz) (add screenshot of the layout picker) (add screenshot of the themes picker)

---

## Highlights

### 🦖 The encyclopedia
33 dinosaurs and prehistoric creatures, 23 space objects — every entry hand-written and fact-checked against sources like NASA and published paleontology research. Stats, fun facts, behaviour, habitat, era, and full-page write-ups, with search, category filters, bookmarks, and curated ranked collections.

### 🔭 Scan Sky — point your phone at the sky
Hold your phone up and the live camera fills the screen, with stars, constellation stick-figures, planets and the moon drawn over the real sky in clean white lines exactly where they are — the page flips into landscape automatically for a natural two-hands grip. Aim the crosshair at anything and a card names it, links into the encyclopedia, and offers **Ask NovaSaur** for whatever's under the crosshair. No camera (or no permission) falls back to a rendered sky, and drag-to-explore works with no sensors at all.

### 🌙 Sky Tonight
Open the app and it tells you what's above you *right now*: the moon's phase (drawn with the real terminator curve), moonrise and moonset, which planets are visible and where to look, the constellations overhead, sunset/sunrise, the moment the sky gets *properly* dark, and the next meteor shower with a moonlight forecast. All of it is computed on-device by [SkyScanner](https://github.com/Karthikeya0923/SkyScanner), an astronomy engine verified against NASA JPL's Horizons ephemeris to within a few hundredths of a degree. Location is optional — say no and you still get a general Northern-sky view. A built-in "Learn the sky" page explains every moon phase and how to tell a planet from a star.

### 🤖 Ask NovaSaur
An AI guide powered by Google's Gemma running locally through [NovaSaur](https://github.com/Karthikeya0923/novasaur), the inference engine built for this app. Anything askable by name answers **instantly** from the hand-checked encyclopedia and a 70-topic knowledge base — verified by an in-repo harness that runs 160+ typed-style questions through the production pipeline (`tools/AnswerHarness`). Live sky questions ("where is Jupiter right now?", "when is the next meteor shower?") are answered by the astronomy engine with tonight's real answer — something a frozen language model could never know. Everything else goes straight to the model — there is no topic wall that bounces typed questions. Open-ended answers stream token by token, ChatGPT-style; the engine handles exactly one question at a time and reloads itself before each new one, so every answer starts on a completely clean slate and the chat can never hang or clog up. The model itself is optional: the chat works fully without it, and a card in the chat (and in Settings) downloads the 2.4 GB model in-app — resumable, pausable, removable.

### 🎨 Your Creations — draw your own
A proper little paint studio: real finger-drag freehand with five brushes (pencil, marker, wide marker, glow, rainbow), an eraser and a fill bucket, adjustable sizes, a colour palette with a custom R/G/B mixer, and undo/redo — with a live preview of the drawing above the details form so naming it never feels disconnected from what was just drawn. Then fill in a full entry — name, pronunciation, name meaning, era, diet, size/weight/speed/bite for a dinosaur (or type + four facts for a space object), plus About, Key features, Habitat, Behaviour and Fun facts — so your creation looks *identical* to a built-in encyclopedia entry. Creations get their own gallery, drop into your custom lists, and the dinosaurs you draw can march straight into **Dino Battle** with an "Include my creatures" toggle — your drawing and all. Everywhere a drawing appears — gallery, entry page, battle arena, list thumbnails — it shows **whole**, letterboxed on its own canvas colour, never cropped to the middle. (They're yours, so there's no Ask-NovaSaur button, and they stay out of Surprise Me.)

### 🎮 Play
Quizzes (5 to 100 questions, dinosaurs / space / mixed), Dino Battles with stat-driven verdicts that argue each matchup like a sports column, daily featured creatures, a **Surprise Me** button that pulls a creature or world you haven't met yet, and streak + discovery counters to keep explorers coming back.

### 🪄 Two layouts, one app
A whole second look you can switch to instantly, with the same seamless cross-fade the themes use. **Native** is the grown-up editorial style — elegant serif headlines, a flat tab bar, magazine-style lists. **Playful** is a ground-up redesign for 5-to-10-year-olds: rounded Baloo headlines, a home screen of big colourful gradient "worlds" to tap into, chunky buttons, and a bubbly tab bar. Same features, a completely different app. Choose it under Settings → Choose a layout. Five full themes sit alongside, under Choose a theme.

---

## Engineering notes

- **All-C# UI** — every screen is built in code (no XAML pages), on a small component kit with design tokens. Both the **theme** (palette + wallpaper) and the **layout** (fonts, shapes, tab bar, home screen) are switchable at runtime and re-skin the whole app with a freeze-frame cross-fade.
- **Instant-first answering** — a typo-tolerant matcher (aliases, edit distance, kid abbreviations) routes questions to the encyclopedia, the knowledge base, or the live astronomy engine before the model is ever considered; follow-up pronouns ("how fast was *it*?") resolve against the last-mentioned entities. Everything else goes straight to the model — one question at a time, with a full engine reload before each new question so the small model always starts clean.
- **On-device drawing** — the Your Creations studio captures finger-strokes via pointer events, renders them live through `Microsoft.Maui.Graphics`, and rasterises to a PNG with a native Android canvas; creations convert into the same models the built-in encyclopedia uses, so they battle and list like any other entry.
- **2.4 GB model delivery** — the AI model ships via Google Play Asset Delivery in 1 GB chunks and assembles on first run; a resumable in-app download (with pause/resume, storage preflight, and a remove-to-free-space option in Settings) covers every other install. No notification or foreground-service permissions needed.
- **True edge-to-edge** — window insets are intercepted natively so the tab bar and chat input reach the physical bottom edge of the screen on every Android version.
- **Zero-warning build** — the project compiles with 0 errors and 0 warnings.

## Built with

- [.NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/) (net10.0-android), C#
- [NovaSaur](https://github.com/Karthikeya0923/novasaur) — on-device LLM engine (Kotlin/Java, LiteRT-LM)
- [SkyScanner](https://github.com/Karthikeya0923/SkyScanner) — NASA-verified astronomy engine (C#)

## Privacy

No accounts, no ads, no analytics, no data collection — everything runs and stays on-device. Full policy: [PRIVACY_POLICY.md](PRIVACY_POLICY.md).

---

## Roadmap

Tracked in detail on the [project board →](https://github.com/users/Karthikeya0923/projects/4)

**Shipped**
- [x] Core encyclopedia — 33 prehistoric creatures + 23 space objects, hand-written and fact-checked
- [x] Search, category filters, bookmarks, curated collections
- [x] All-C# UI system — serif/sans editorial design, design tokens, zero XAML pages
- [x] NovaSaur on-device AI — Gemma via LiteRT-LM, JNI bridge, retrieval-grounded answers
- [x] 3 GB model delivery — Play Asset Delivery chunking + resumable fallback download
- [x] Quizzes (slider from 5 to 100 questions), Dino Battles with stat-driven verdicts, streaks
- [x] Sky Tonight — live moon phase, visible planets & constellations, NASA-verified math
- [x] Learn the Sky — every moon phase and sky-watching basics, explained for kids
- [x] Custom lists — build and mix your own dino/space collections (your creations included)
- [x] Scan Sky — camera passthrough with a live white star overlay, automatic landscape, all 88 constellations, target card with Learn More & Ask NovaSaur
- [x] Streaming AI answers + 70-topic knowledge base, verified by a 160+-question harness
- [x] Reset-per-question AI: one question at a time, a clean engine before every answer, no long-session decay, and no topic wall
- [x] Your Creations — a drawing studio with full stats, its own gallery, custom-list support, and a Dino Battle "Include my creatures" toggle
- [x] Two switchable layouts — grown-up **Native** and kid-first **Playful** — with a seamless cross-fade
- [x] Live sky answers in chat — "where is Jupiter right now?", "when is the next meteor shower?"
- [x] Meteor showers on Sky Tonight — active shower, next peak, moonlight forecast
- [x] Moonrise/moonset and true astronomical-dark times
- [x] Surprise Me discovery, daily streak & discovery counters
- [x] Five full themes with wallpapers, switched with a seamless cross-fade
- [x] True edge-to-edge UI, haptic strength control, adjustable text size
- [x] In-app AI model manager — download / pause / resume / remove, in the chat and in Settings
- [x] Drawings always display whole — letterboxed on their canvas colour in the gallery, entries, battles, and thumbnails
- [x] Zero-warning build; AI answer pipeline covered by an automated test harness

**In progress**
- [ ] Final artwork for all encyclopedia entries
- [ ] Play Store assets (icon, feature graphic, splash screen)
- [ ] Google Play closed testing, then production release

---

## About the developer

Karthikeya Arikirevula is a Software Engineering (Co-op) student at the University of Guelph who grew up obsessed with dinosaurs and space, and built DinoSpace to put both in one pocket-sized, offline package — from the UI down to the AI engine and the orbital math.
