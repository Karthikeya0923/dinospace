# DinoSpace 2.0 — Full Redesign

A ground-up rebuild of both the app and the NovaSaur AI. The goal was to take
DinoSpace from "functional but plain" to something people actually want to open
when they're bored. Everything below is new or substantially rewritten.

## Design system
- **New deep-space theme** (`Resources/Styles/Colors.xaml`, `Ui/Theme.cs`): a
  layered dark palette with three domain accents — amber for dinosaurs, indigo
  for space, teal for NovaSaur — plus gradient backdrops and hero brushes.
- **Component library** (`Ui/Ui.cs`): one set of builders for titles, cards,
  chips, stat bars, list rows and section headers, so every screen is visually
  consistent. Cards use `Border` (not the deprecated `Frame`) for speed.
- **Baloo** for headings, **Nunito** for body text.

## Navigation
- Replaced the janky `CarouselView` tab host with a **custom five-tab shell**
  (`Views/RootPage.cs`): Home · Explore · Nova AI · Play · You. Switching tabs is
  instant (visibility swap, no cross-page scroll). Hardware back returns to Home
  first, then exits.
- Pushed pages (details, quiz, collections, battles) have a floating back button
  and support **swipe-from-left to go back**.

## Screens
- **Home** — welcome, daily featured creature/object (flippable), quick actions,
  live progress (streak / entries seen / XP), and a rotating fact.
- **Explore** — one unified, searchable encyclopedia with a Dino/Space/All
  segment, category filters, and a **two-column visual card grid** (virtualized
  `CollectionView`). Replaces the old plain single-scroll list.
- **Nova AI** — the chat, now a first-class tab (see AI section).
- **Play** — quizzes (5/10/25/50/100 questions), dino battles, curated
  collections, and facts.
- **You** — level & XP, a friendly stats summary, bookmarks, and settings.
- **Detail pages** — full-bleed hero image with gradient scrim and overlaid
  title/chips, a horizontal quick-stat row, animated stat bars, "Ask Nova about
  this", share, save, deep sections, and related entries.

## Content
- **Dinosaurs**: 20 → **33** (added Ankylosaurus, Pachycephalosaurus,
  Quetzalcoatlus, Dilophosaurus, Baryonyx, Iguanodon, Utahraptor, Gallimimus,
  Kronosaurus, Dunkleosteus, Smilodon, Woolly Mammoth, Compsognathus).
- **Space**: 7 → **23** (the full eight planets, Pluto, Europa, Halley's Comet,
  the asteroid belt, Betelgeuse, the Milky Way, Sagittarius A*, the Orion Nebula,
  the ISS, Voyager 1).
- **Bite force** in PSI is now a first-class stat (replacing "power").
- **Quizzes**: ~22 → **56**, each with a difficulty and a teaching explanation.
- **Facts**: 25 → **80**.
- **KnowledgeBase**: 20 curated fact nuggets that ground NovaSaur on common
  questions (extinction, how stars form, what a light-year is …).
- **Collections**: 9 curated ranked lists ("Biggest Creatures Ever", "Most
  Dangerous", "Strongest Bites", "A Journey From the Sun" …).
- Universal **Canadian English**.

## NovaSaur AI rebuild
The old RAG was essentially non-functional and the prompt was bloated. The new
pipeline (`Services/`):
- **Retriever** grounds each question on encyclopedia entries *and* the new
  KnowledgeBase, so general questions ("why did the dinosaurs die?") finally get
  real facts instead of hallucinations.
- **PromptBuilder** produces a much leaner prompt (tight instruction, facts up
  front, one short example) — less overhead means the small on-device model has
  more room to actually answer.
- **NovaGuard** keeps kids safe (personal info, self-harm, violence, weapons,
  adult topics) but is far more generous about what counts as answerable.
- **NovaSaurService** wraps the bound engine, serializes inference, and cleans
  answers. Replies stream in with a typewriter reveal.
- The Kotlin engine's sampler temperature was tuned to 0.5 for steadier,
  more factual answers. (Note: the shipped `novasaur.aar` must be rebuilt from
  the `novasaur` repo for Kotlin changes to take effect on-device.)

## New features
- Dino battles (composite winner from size, weight, bite force, speed, danger).
- Curated collections with ranked medals.
- Share fun facts / entries to any app.
- 3-slide first-launch onboarding.
- Haptic feedback on saves and interactions (toggleable).
- Accessibility: four text-size options and TalkBack/VoiceOver descriptions.
- XP, levels, daily streaks, and a personal stats summary.

## Deferred to v2.1
- **Scan Sky** (point-your-camera feature) — intentionally left for a later
  update.
