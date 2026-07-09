#!/bin/bash
# Populates https://github.com/users/Karthikeya0923/projects/4 with the
# DinoSpace roadmap. Idempotence: skips titles that already exist.
set -e
OWNER=Karthikeya0923
NUM=4

existing=$(gh project item-list $NUM --owner $OWNER --format json --limit 200 2>/dev/null | python -c "import json,sys; d=json.load(sys.stdin); print('\n'.join(i['title'] for i in d.get('items',[])))" 2>/dev/null || echo "")

add() {   # add "<title>" "<body>" "<status: Done|In Progress|Todo>"
  local title="$1" body="$2" status="$3"
  if echo "$existing" | grep -Fxq "$title"; then echo "skip: $title"; return; fi
  local out id
  out=$(gh project item-create $NUM --owner $OWNER --title "$title" --body "$body" --format json)
  id=$(echo "$out" | python -c "import json,sys; print(json.load(sys.stdin)['id'])")
  if [ -n "$STATUS_FIELD" ] && [ -n "$id" ]; then
    local opt
    opt=$(echo "$FIELDS_JSON" | python -c "
import json,sys
d=json.load(sys.stdin)
for f in d.get('fields',[]):
    if f.get('name')=='Status':
        for o in f.get('options',[]):
            if o['name'].lower()=='$status'.lower():
                print(o['id']); break
")
    if [ -n "$opt" ]; then
      gh project item-edit --id "$id" --project-id "$PROJECT_ID" --field-id "$STATUS_FIELD" --single-select-option-id "$opt" > /dev/null || true
    fi
  fi
  echo "added: $title [$status]"
}

PROJECT_ID=$(gh project view $NUM --owner $OWNER --format json | python -c "import json,sys; print(json.load(sys.stdin)['id'])")
FIELDS_JSON=$(gh project field-list $NUM --owner $OWNER --format json)
STATUS_FIELD=$(echo "$FIELDS_JSON" | python -c "
import json,sys
d=json.load(sys.stdin)
for f in d.get('fields',[]):
    if f.get('name')=='Status': print(f['id']); break
")

# ---- Phase 1 · Foundation ----
add "Core encyclopedia — 33 prehistoric creatures + 23 space objects" "Hand-written, fact-checked entries with stats, habitats, eras, fun facts and full write-ups." "Done"
add "All-C# UI system with design tokens (zero XAML pages)" "Component kit + runtime-switchable themes and layouts with a freeze-frame cross-fade." "Done"
add "Search, category filters, bookmarks & curated collections" "Typo-tolerant search and ranked collection pages." "Done"

# ---- Phase 2 · On-device intelligence ----
add "NovaSaur: on-device LLM engine (LiteRT-LM, Kotlin) + .NET binding" "Gemma running fully offline; 2.4 GB model via Play Asset Delivery with a resumable in-app download fallback." "Done"
add "Instant-first answer pipeline" "Encyclopedia + 80-topic knowledge base + live-sky math answer first; the model only sees genuinely open questions. Verified by an 800+-question harness in CI-style runs." "Done"
add "Live sky answers in chat" "\"Where is Jupiter right now?\" answered from tonight's actual sky, computed on-device." "Done"

# ---- Phase 3 · The sky ----
add "SkyScanner: NASA-verified astronomy engine" "Moon/planets vs JPL Horizons to hundredths of a degree; eclipses, conjunctions, ISS passes via SGP4; 80 green tests." "Done"
add "Scan Sky AR overlay with true-north pointing" "Camera passthrough + rotation-vector sensor fused to true north; the moon is drawn where the moon is." "Done"
add "Full deep-sky catalogues in Scan Sky" "All 110 Messier + 109 Caldwell objects with stories, 1,700-star catalogue with true colours, the Milky Way band, textured moon & planets, radiant-aware shooting stars." "Done"
add "Time travel & sky darkness in Scan Sky" "Scrub the sky ±12 hours; city / suburb / dark-site modes show an honest star count." "Done"
add "Sky Tonight & Learn the Sky" "Live moon phase card, visible planets, meteor showers with moonlight forecasts, moonrise/set, twilight times." "Done"

# ---- Phase 4 · Play ----
add "Dino Battles with stat-driven verdicts" "Any two creatures argue it out like a sports column — including the ones kids draw." "Done"
add "Quizzes: 5 to 100 questions, three topics" "Difficulty chips, right-answer reveal, score badges." "Done"
add "Your Creations drawing studio" "Five brushes, fill bucket, undo/redo, full entry form — creations join the encyclopedia, lists and battles." "Done"
add "Streaks, discovery counters & Surprise Me" "Daily comeback loop weighted toward unseen entries." "Done"

# ---- Phase 5 · Polish & release ----
add "Playful layout redesign — storybook pastels" "Sage/powder/blush world tiles, bubbly tabs, rounded type; Native stays quiet and editorial." "Done"
add "Dino Meadow theme" "Pale-sage picture-book wallpaper + palette, made for the Playful layout." "Done"
add "Final artwork for all encyclopedia entries" "Consistent 2D art set across all 56 entries." "In Progress"
add "Play Store assets & listing" "Icon, feature graphic, screenshots, store copy, content rating." "In Progress"
add "Google Play closed testing" "Internal track first, then a closed cohort of families." "Todo"
add "Production release on Google Play" "Staged rollout with pre-launch report checks." "Todo"

# ---- Phase 6 · Beyond 1.0 ----
add "Read-aloud narration for young readers" "TTS on entries and NovaSaur replies." "Todo"
add "Seasonal sky event notifications" "Meteor-shower peaks and eclipse reminders, computed on-device." "Todo"
add "New creature packs" "Cambrian oddballs and Ice Age megafauna as content updates." "Todo"
add "Tablet layout pass" "Multi-column encyclopedia and side-by-side battle view." "Todo"
add "iOS build" "MAUI already targets iOS; needs signing, model delivery path and TestFlight." "Todo"

echo "roadmap done"
