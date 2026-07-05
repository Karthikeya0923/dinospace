# DinoSpace

**A fully offline dinosaur & space encyclopedia for Android, with its own on-device AI and a live astronomy engine.** Built from scratch in C# with .NET MAUI.

DinoSpace blends the two things every kid (and plenty of adults) can't get enough of — dinosaurs and outer space — into one app that works with zero internet: a hand-written encyclopedia, an AI guide that runs entirely on the phone, and a personalized report of tonight's actual sky.

![Platform](https://img.shields.io/badge/platform-Android-green)
![Language](https://img.shields.io/badge/language-C%23-blue)
![Framework](https://img.shields.io/badge/framework-.NET%20MAUI-purple)
![Status](https://img.shields.io/badge/status-Play%20Store%20prep-orange)

(add screenshot of home screen) (add screenshot of Sky Tonight) (add screenshot of NovaSaur chat)

---

## Highlights

### 🦖 The encyclopedia
33 dinosaurs and prehistoric creatures, 23 space objects — every entry hand-written and fact-checked against sources like NASA and published paleontology research. Stats, fun facts, behaviour, habitat, era, and full-page write-ups, with search, category filters, bookmarks, and curated ranked collections.

### 🔭 Sky View — point your phone at the sky
Hold your phone up and move it around: stars, constellation stick-figures with names, planets, and the moon render live for exactly the direction you're facing, driven by the orientation sensor (with drag-to-explore as a fallback). Aim the crosshair at anything and it names what you're looking at. This is the Sky Guide experience, running on the SkyScanner chart engine — no internet, no camera permission.

### 🌙 Sky Tonight
Open the app and it tells you what's above you *right now*: the moon's phase (drawn with the real terminator curve), which planets are visible and where to look, the constellations overhead, and sunset/sunrise times. All of it is computed on-device by [SkyScanner](https://github.com/Karthikeya0923/SkyScanner), an astronomy engine verified against NASA JPL's Horizons ephemeris to within a few hundredths of a degree. Location is optional — say no and you still get a general Northern-sky view. A built-in "Learn the sky" page explains every moon phase and how to tell a planet from a star.

### 🤖 Ask NovaSaur
An AI guide powered by Google's Gemma running locally through [NovaSaur](https://github.com/Karthikeya0923/novasaur), the inference engine built for this app. Questions are grounded in the encyclopedia and a 55-topic knowledge base through retrieval, so real questions get instant, always-accurate answers without touching the model — verified by an in-repo harness that runs 108 typed-style questions through the production pipeline (`tools/AnswerHarness`). Open-ended questions stream from the LLM token by token, ChatGPT-style, with layered timeouts so the chat can never hang. NovaSaur even answers live sky questions ("is the moon full tonight?") from the astronomy engine, something a frozen language model could never know.

### 🎮 Play
Quizzes (5 to 100 questions, dinosaurs / space / mixed), Dino Battles with stat-driven verdicts that argue each matchup like a sports column, daily featured creatures, and a streak to keep explorers coming back.

### 🎨 Six app themes
Full looks — wallpaper plus a matching colour palette on every page — switched with a seamless cross-fade: a hand-painted twilight, starry midnight, aurora, dusk, nebula, and a warm parchment light theme.

---

## Engineering notes

- **All-C# UI** — every screen is built in code (no XAML pages), on a small component kit with a serif/sans editorial design system and design tokens for theming.
- **On-device RAG** — a retriever matches questions against entry names, aliases (typo-tolerant, edit-distance based), and a curated knowledge base, then builds compact grounded prompts for the model. Chat history and follow-up pronouns ("how fast was *it*?") resolve against the last-mentioned entities.
- **3 GB model delivery** — the AI model ships via Google Play Asset Delivery in 1 GB chunks and assembles on first run; a resumable in-app download is the fallback. No notification or foreground-service permissions needed.
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
- [x] Custom lists — build and mix your own dino/space collections
- [x] Sky View — sensor-driven point-at-the-sky star finder with live constellation figures
- [x] Streaming AI answers + 55-topic knowledge base, verified by a 108-question harness
- [x] Seven full themes with wallpapers + three layout presets, seamless switching
- [x] True edge-to-edge UI, haptic strength control, adjustable text size
- [x] Zero-warning build; AI answer pipeline covered by an automated test harness

**In progress**
- [ ] Final artwork for all encyclopedia entries
- [ ] Play Store assets (icon, feature graphic, splash screen)
- [ ] Google Play closed testing, then production release

**Future ideas**
- [ ] Meteor-shower alerts ("the Perseids peak tonight!")
- [ ] More creatures and deep-space objects, seasonal featured collections
- [ ] Tablet layout

---

## About the developer

Karthikeya Arikirevula is a Software Engineering (Co-op) student at the University of Guelph who grew up obsessed with dinosaurs and space, and built DinoSpace to put both in one pocket-sized, offline package — from the UI down to the AI engine and the orbital math.
