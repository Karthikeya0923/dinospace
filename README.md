<p align="center">
  <img src="docs/assets/icon.png" width="104" alt="DinoSpace">
</p>

<h1 align="center">DinoSpace</h1>

<p align="center">
  A hand-drawn dinosaur &amp; space encyclopedia for kids.<br>
  Fully offline. No ads, no accounts, no data collected.
</p>

<p align="center">
  <a href="https://play.google.com/store/apps/details?id=com.dinospace.kids">
    <img src="https://play.google.com/intl/en_us/badges/static/images/badges/en_badge_web_generic.png" height="72" alt="Get it on Google Play">
  </a>
</p>

<p align="center">
  <a href="https://karthikeya0923.github.io/dinospace/">Website</a>&ensp;·&ensp;<a href="https://karthikeya0923.github.io/dinospace/privacy.html">Privacy policy</a>&ensp;·&ensp;<a href="#whats-inside">What's inside</a>&ensp;·&ensp;<a href="#under-the-hood">Under the hood</a>
</p>

<p align="center"><sub>In closed testing on Google Play — public release August 2026.</sub></p>

<br>

<table>
  <tr>
    <td><img src="docs/readme-cards/card-home.png" alt="Home"></td>
    <td><img src="docs/readme-cards/card-encyclopedia.png" alt="Encyclopedia"></td>
    <td><img src="docs/readme-cards/card-entry.png" alt="Creature entries"></td>
  </tr>
  <tr>
    <td><img src="docs/readme-cards/card-funfacts.png" alt="Fun facts"></td>
    <td><img src="docs/readme-cards/card-nova.png" alt="Ask Nova"></td>
    <td><img src="docs/readme-cards/card-sky.png" alt="Tonight's sky"></td>
  </tr>
  <tr>
    <td><img src="docs/readme-cards/card-quiz.png" alt="Quizzes"></td>
    <td><img src="docs/readme-cards/card-battle.png" alt="Dino battle"></td>
    <td><img src="docs/readme-cards/card-home-dark.png" alt="Twilight theme"></td>
  </tr>
</table>

<p align="center">
  <img src="docs/readme-cards/card-ar-wide.png" alt="Scan Sky — live camera with a real star map">
</p>

## What's inside

- **100 hand-drawn entries** — 50 prehistoric creatures and 50 space objects, each with stats, habitat, era, behaviour and fun facts, fact-checked against NASA data and published paleontology.
- **Scan the sky** — a live camera view that overlays only what is genuinely above you: the sun, a phase-correct moon, the planets, and after dark the bright named stars and constellation figures.
- **Tonight's sky report** — moon phase and rise/set, visible planets and where to look, the next meteor shower, and true-dark time, all computed on the phone.
- **Ask Nova** — a question-answering buddy that runs entirely on-device. Instant answers for anything in the encyclopedia; a downloadable model handles the open-ended rest.
- **Dino battles** — stat-driven matchups between any two creatures, including ones you drew yourself.
- **Quizzes** — five to a hundred questions, dinosaurs or space or both, with a friendly explanation for every answer.
- **Drawing studio** — five brushes, fill bucket, colour mixer and undo/redo; finished drawings become full encyclopedia entries and can fight in battles.
- **Two looks** — a soft pastel storybook and a hand-painted twilight, switchable any time.

## Under the hood

- **All-code UI** — every screen is C# on .NET MAUI; no XAML pages, one small component kit, themed at runtime.
- **[SkyScanner](https://github.com/Karthikeya0923/SkyScanner)** — the in-house astronomy engine behind every sky feature. Positions verified against NASA JPL's Horizons ephemeris to within hundredths of a degree, fully offline.
- **[NovaSaur](https://github.com/Karthikeya0923/novasaur)** — the in-house inference engine that runs Google's Gemma locally through LiteRT-LM. Nothing typed in the chat ever leaves the device.
  
## Privacy

Built for kids, so the bar is absolute:

- No ads, no purchases, no accounts.
- No analytics, no tracking, no data collection of any kind.
- Location and camera are optional, processed on the device, and never stored or transmitted.

Full details: [privacy policy](https://karthikeya0923.github.io/dinospace/privacy.html).

## License

GPL-3.0 — see [LICENSE](LICENSE).
