using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using dinospace.Data;
using dinospace.Models;

namespace dinospace.Services
{
    // Answers the great majority of dinosaur and space questions instantly and
    // correctly, straight from the vetted encyclopedia, so NovaSaur feels fast
    // and never sits stuck on "thinking…". The on-device language model is kept
    // only as a fallback for genuinely open questions it doesn't recognise.
    //
    // Every answer here is built from real data in DinoData / SpaceData /
    // KnowledgeBase, so it is always factually grounded — no hallucinations.
    public static class LocalAnswer
    {
        // Returns a finished, kid-friendly answer, or null to let the model try.
        public static string? TryAnswer(string question, string q, Grounding g, IReadOnlyList<string> carryover)
        {
            if (string.IsNullOrWhiteSpace(question)) return null;

            // 0) Live sky questions ("is the moon full tonight?") — the model
            //    can't know tonight's sky, but the Sky Tonight engine can.
            var sky = SkyAnswer(q);
            if (sky != null) return sky;

            var dinos = new List<Dinosaur>();
            var spaces = new List<SpaceObject>();
            foreach (var name in g.Entities)
            {
                var d = DinoData.ByName(name);
                if (d != null) { dinos.Add(d); continue; }
                var s = SpaceData.ByName(name);
                if (s != null) spaces.Add(s);
            }

            // 1) Two creatures head to head ("could a Spinosaurus beat a T. Rex?").
            if (dinos.Count >= 2 && IsComparison(q))
                return Compare(dinos[0], dinos[1], q);

            // 1.5) "How far is Neptune from Venus?" — computed live from the
            //      same orbital math as Sky Tonight, not looked up wrong.
            if (spaces.Count >= 2 && Has(q, "far", "distance", "away", "apart", "close", "closer", "between"))
            {
                var d = SpaceDistance(spaces[0], spaces[1]);
                if (d != null) return d;
            }

            // 2) A specific measurable stat about one entity ("how fast was it?").
            if (dinos.Count == 1)
            {
                var s = DinoStat(dinos[0], q);
                if (s != null) return s;
            }
            if (spaces.Count == 1 && dinos.Count == 0)
            {
                var s = SpaceStat(spaces[0], q);
                if (s != null) return s;
            }

            // 3) A curated general-knowledge fact (extinction, black holes, how
            //    stars form, …). Prefer it for conceptual "why/how/what is"
            //    questions, or when no specific entity was matched.
            var nugget = Retriever.BestNugget(q);
            if (nugget != null && ((dinos.Count == 0 && spaces.Count == 0) || IsConceptual(q)))
                return nugget.Fact;

            // 4) Anything else about a recognised entity: answer from its entry,
            //    leading with whatever sentence best fits the question so it
            //    feels like a real reply, not a canned blurb.
            if (dinos.Count >= 1) return EntityAnswer(q, dinos[0].Name, DinoSentences(dinos[0]), DinoIntro(dinos[0]));
            if (spaces.Count >= 1) return EntityAnswer(q, spaces[0].Name, SpaceSentences(spaces[0]), SpaceIntro(spaces[0]));

            // 5) A last-chance curated fact even without a conceptual cue.
            if (nugget != null) return nugget.Fact;

            return null;
        }

        // ---------- distance between two space objects ----------

        private static string? SpaceDistance(SpaceObject a, SpaceObject b)
        {
            if (a.Name == b.Name) return null;

            double jd = SkyCalc.JulianDay(DateTime.UtcNow);
            double? au = SkyCalc.DistanceBetweenAu(a.Name, b.Name, jd);
            if (au != null)
            {
                double km = au.Value * 149_597_870.7;
                string dist = km >= 1e9 ? $"{km / 1e9:0.#} billion km" : $"{km / 1e6:0} million km";
                return $"Right now, {a.Name} and {b.Name} are about {dist} apart ({au:0.#} times the Earth–Sun distance). " +
                       "Both are always moving along their orbits, so this gap changes through the year.";
            }

            // not both computable (a galaxy, a star, the moon...) — fall back
            // to each one's own distance stat so the answer is still honest
            string? da = FirstStat(a, "distance", "orbit", "location", "altitude");
            string? db = FirstStat(b, "distance", "orbit", "location", "altitude");
            if (da != null && db != null)
                return $"{a.Name} is {LowerLead(da)}, while {b.Name} is {LowerLead(db)}. They're not a fixed distance from each other — everything out there is moving.";
            return null;
        }

        private static string? FirstStat(SpaceObject s, params string[] keys)
        {
            foreach (var (label, value) in SpaceStats(s))
            {
                string l = label.ToLowerInvariant();
                if (keys.Any(k => l.Contains(k)) && !string.IsNullOrWhiteSpace(value)) return value;
            }
            return null;
        }

        // ---------- live sky ----------

        // Questions about tonight's actual sky, answered by the same astronomy
        // engine behind Sky Tonight. Cues are kept tight so "how big is the
        // moon" still goes to the encyclopedia, not the sky report.
        private static string? SkyAnswer(string q)
        {
            bool tonight = Has(q, "tonight", "now", "currently", "today", "evening");

            // Moon phase, and next full/new moon dates.
            if (q.Contains("moon") && Has(q, "phase", "full", "new", "crescent", "gibbous", "quarter", "lit", "cycle", "waxing", "waning"))
            {
                var m = SkyCalc.Moon(DateTime.UtcNow);
                if (Has(q, "next", "when"))
                {
                    if (q.Contains("new"))
                        return $"The next new moon is on {m.NextNewUtc.ToLocalTime():dddd, MMMM d} — the best night for spotting faint stars.";
                    return $"The next full moon is on {m.NextFullUtc.ToLocalTime():dddd, MMMM d}.";
                }
                string trend = m.Waxing ? "growing a little brighter every night" : "shrinking a little every night";
                string next = m.Waxing
                    ? $"It will be full on {m.NextFullUtc.ToLocalTime():MMMM d}."
                    : $"It will be new on {m.NextNewUtc.ToLocalTime():MMMM d}.";
                return $"Tonight the moon is a {m.PhaseName} — about {m.Illumination * 100:0}% lit and {trend}. {next}";
            }

            // "What can I see tonight?", "which constellations are out?" ...
            bool wantsSky = (Has(q, "sky", "see", "visible", "spot", "look", "out") && tonight)
                          || (Has(q, "constellation", "constellations") && Has(q, "see", "visible", "tonight", "now", "above", "overhead", "out", "my"))
                          || (Has(q, "planet", "planets") && Has(q, "see", "visible", "tonight", "now", "spot", "out"));
            if (wantsSky) return SkySummary();

            // Sunset and sunrise times ("why does the sun rise" stays conceptual).
            if (!q.StartsWith("why") &&
                (Has(q, "sunset", "sunrise") || (q.Contains("sun") && Has(q, "set", "sets", "rise", "rises"))))
            {
                var r = SkyService.BuildReport(SkyService.Cached);
                if (r.NextSunsetLocal is DateTime set && r.NextSunriseLocal is DateTime rise)
                    return $"The sun sets at {SkyService.FormatTime(set)} and rises again at {SkyService.FormatTime(rise)}. " +
                           $"The sky is dark enough for stargazing about an hour and a half after sunset.";
                return null;
            }

            // "Where is Jupiter?" — a live pointing answer, not a paragraph.
            var point = PointAtAnswer(q);
            if (point != null) return point;

            // "When is the next meteor shower?", "can I see shooting stars tonight?"
            if ((q.Contains("meteor") || q.Contains("shooting star")) &&
                Has(q, "next", "when", "tonight", "soon", "peak", "peaks", "see", "watch", "active", "coming"))
                return ShowerAnswer();

            return null;
        }

        // Live direction for "where is X" style questions about the moon and
        // the naked-eye planets. Kept to explicit finding phrases so "where
        // did Jupiter get its name" still reads the encyclopedia.
        private static string? PointAtAnswer(string q)
        {
            bool asking = q.StartsWith("where is") || q.StartsWith("where s") ||
                          Has(q, "which direction", "which way", "where can i see", "where do i look",
                                 "how do i find", "how can i find", "how do i spot", "point me");
            if (!asking) return null;

            var where = SkyService.Cached;
            var utc = DateTime.UtcNow;
            double jd = SkyCalc.JulianDay(utc);

            if (q.Contains("moon"))
            {
                var (ra, dec) = SkyCalc.MoonRaDec(jd);
                var (alt, az) = SkyCalc.AltAz(ra, dec, where.Lat, where.Lon, utc);
                var m = SkyCalc.Moon(utc);
                if (alt > 3)
                    return $"The moon is {SkyService.Describe(alt, az)} right now — a {m.PhaseName}, about {m.Illumination * 100:0}% lit. Try Scan Sky and point your phone at it!";
                return $"The moon is below the horizon right now (it's a {m.PhaseName} at the moment). Check Sky Tonight later — it rises and sets at different times every day.";
            }

            foreach (var b in Enum.GetValues<SkyCalc.Body>())
            {
                if (!q.Contains(b.ToString().ToLowerInvariant())) continue;
                var (ra, dec, _) = SkyCalc.PlanetRaDec(b, jd);
                var (alt, az) = SkyCalc.AltAz(ra, dec, where.Lat, where.Lon, utc);
                bool dark = SkyCalc.SunAltitude(where.Lat, where.Lon, utc) < -6;
                if (alt > 5 && dark)
                    return $"{b} is {SkyService.Describe(alt, az)} right now. Open Scan Sky and point your phone that way to see it labelled live!";
                if (alt > 5)
                    return $"{b} is {SkyService.Describe(alt, az)} right now, but the sky is still too bright to spot it. Look again about an hour after sunset.";
                return $"{b} is below the horizon right now, so you can't see it tonight from where you are. Check Sky Tonight — the planets shift a little every night.";
            }

            return null;
        }

        // Live meteor-shower report: what's active now, or the next peak.
        private static string ShowerAnswer()
        {
            var now = DateTime.UtcNow;
            var active = MeteorShowers.ActiveOn(now).OrderByDescending(s => s.Zhr).ToList();

            if (active.Count > 0)
            {
                var s = active[0];
                // The peak for THIS activity run: this year's date if it's
                // still ahead, or January's date for showers (Quadrantids)
                // whose run straddles New Year.
                DateTime? peak = null;
                if (s.PeakOn(now.Year) >= now.Date) peak = s.PeakOn(now.Year);
                else if (now.Month == 12 && s.StartMonth == 12 && s.PeakMonth == 1) peak = s.PeakOn(now.Year + 1);

                if (peak is DateTime p)
                {
                    string verdict = MeteorShowers.MoonVerdict(MeteorShowers.MoonInterference(s, p.Year));
                    return $"The {s.Name} are active right now — {LowerLead(s.Blurb)}. They peak around {p:MMMM d} with up to {s.Zhr} meteors an hour under perfect skies ({verdict}). Find a dark spot after midnight and give your eyes twenty minutes to adjust.";
                }
                return $"The {s.Name} are still active — {LowerLead(s.Blurb)}. The peak night has passed this year, but you can still catch stragglers on any clear, dark night.";
            }

            var (next, peakUtc) = MeteorShowers.Next(now);
            string moon = MeteorShowers.MoonVerdict(MeteorShowers.MoonInterference(next, peakUtc.Year));
            return $"The next meteor shower is the {next.Name}, peaking around {peakUtc:MMMM d} — {LowerLead(next.Blurb)}. Expect up to {next.Zhr} meteors an hour under perfect dark skies ({moon}).";
        }

        private static string SkySummary()
        {
            var r = SkyService.BuildReport(SkyService.Cached);
            var sb = new StringBuilder();
            string when = r.IsNight ? "right now" : "after dark tonight";

            if (r.Planets.Count > 0)
                sb.Append($"Planets you can spot {when}: {JoinNames(r.Planets.Select(p => $"{p.Name} in the {SkyCalc.Compass(p.AzDeg)}"))}. ");
            else
                sb.Append($"No bright planets are up {when} — they're hiding in the daytime sky. ");

            if (r.Constellations.Count > 0)
                sb.Append($"Constellations to look for: {JoinNames(r.Constellations.Take(3).Select(c => c.Constellation.Name))}. ");

            sb.Append($"The moon is a {r.Moon.PhaseName}, about {r.Moon.Illumination * 100:0}% lit.");
            if (!r.Where.FromDevice)
                sb.Append(" For your exact sky, open Sky Tonight on the Home tab and allow location.");
            return sb.ToString();
        }

        private static string JoinNames(IEnumerable<string> items)
        {
            var list = items.ToList();
            return list.Count switch
            {
                0 => "",
                1 => list[0],
                _ => string.Join(", ", list.Take(list.Count - 1)) + " and " + list[^1]
            };
        }

        // ---------- intent detection ----------

        private static bool IsComparison(string q) =>
            Has(q, "vs", "versus", "beat", "beats", "win", "wins", "won", "fight", "fights", "battle",
                   "stronger", "bigger", "faster", "tougher", "against", "compare", "compared", "or")
            || q.Contains("who would");

        private static bool IsConceptual(string q)
        {
            string first = q.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            return first is "why" or "how" or "what" or "do" or "does" or "did" or "are" or "is" or "can" or "could";
        }

        // ---------- dinosaur stats ----------

        private static string? DinoStat(Dinosaur d, string q)
        {
            if (Has(q, "mean", "means", "meaning", "named", "called"))
                return Trim($"The name {d.Name} means “{d.Meaning}”. {FirstSentence(d.AboutText)}");

            // Temporal must come before "size", since "how long AGO" contains "long".
            if (Has(q, "when", "era", "period", "ago", "alive", "exist", "existed", "old"))
                return Trim($"{d.Name} lived during the {d.Era}. {AgeSentence(d)}");

            if (q.Contains("bite") || Has(q, "strong", "stronger", "strongest", "powerful"))
            {
                if (!string.IsNullOrEmpty(d.BiteForce))
                    return Trim($"{d.Name} had a bite force of about {Pretty(d.BiteForce)}. {BiteFlavor(d)}");
                string reliedOn = d.Diet.Contains("Herb", StringComparison.OrdinalIgnoreCase) ? "its size and beak" : "speed and its claws";
                return $"{d.Name} wasn’t known for a strong bite — it relied more on {reliedOn}.";
            }

            if (Has(q, "eat", "eats", "ate", "diet", "food", "meat", "plants", "plant", "carnivore", "herbivore", "omnivore", "hunt", "prey"))
                return DietAnswer(d);

            if (Has(q, "fast", "fastest", "speed", "quick", "quickest", "quickly", "slow", "slowest", "run", "runs", "running", "swim", "sprint"))
            {
                if (string.IsNullOrEmpty(d.Speed)) return null;
                return Trim($"{d.Name} could move at around {Pretty(d.Speed)}. {SpeedFlavor(d)}");
            }

            if (Has(q, "tall", "height"))
            {
                if (string.IsNullOrEmpty(d.Height)) return null;
                string fact = RelevantFact(q, d);
                return Trim($"{d.Name} stood about {Pretty(d.Height)} tall. {fact}");
            }

            if (Has(q, "weigh", "weight", "heavy", "heavier", "heaviest", "mass"))
            {
                if (string.IsNullOrEmpty(d.Weight)) return null;
                string fact = RelevantFact(q, d);
                return Trim($"{d.Name} weighed around {Pretty(d.Weight)}. {fact}");
            }

            if (Has(q, "big", "bigger", "biggest", "large", "larger", "largest", "long", "longer", "longest",
                       "size", "huge", "small", "smaller", "smallest", "tiny", "tiniest"))
                return SizeAnswer(d);

            if (Has(q, "where", "habitat", "continent", "country", "live", "lived", "found", "region"))
            {
                // Entries usually open with "X lived in ..." already — don't
                // say it twice ("Stegosaurus lived in stegosaurus lived in...").
                string habitat = FirstSentence(d.LifeEnvironmentText);
                if (habitat.StartsWith(d.Name, StringComparison.OrdinalIgnoreCase))
                    return Trim(habitat);
                return Trim($"{d.Name} lived in {LowerLead(habitat.Length > 0 ? habitat : "a variety of habitats.")}");
            }

            return null;
        }

        private static string SizeAnswer(Dinosaur d)
        {
            var sb = new StringBuilder();
            if (Has(d.Length)) sb.Append($"{d.Name} was about {Pretty(d.Length)} long");
            else sb.Append(d.Name);
            if (Has(d.Height)) sb.Append($" and {Pretty(d.Height)} tall");
            sb.Append(". ");
            if (Has(d.Weight)) sb.Append($"It weighed around {Pretty(d.Weight)}. ");
            string extra = SizeFact(d);
            if (extra.Length > 0) sb.Append(extra);
            return sb.ToString().Trim();
        }

        private static string DietAnswer(Dinosaur d)
        {
            string kind = d.Diet.ToLowerInvariant();
            string lead;
            if (kind.Contains("herbivore")) lead = $"{d.Name} was a plant-eater (a herbivore).";
            else if (kind.Contains("omnivore")) lead = $"{d.Name} was an omnivore, eating both plants and small animals.";
            else lead = $"{d.Name} was a meat-eater (a carnivore).";
            string why = FirstSentence(d.BehaviourText);
            return Trim(lead + " " + why);
        }

        // ---------- space stats ----------

        private static string? SpaceStat(SpaceObject s, string q)
        {
            if ((q.Split(' ').FirstOrDefault() ?? "") == "why") return null; // "why" wants an explanation, not a number

            if (Has(q, "big", "bigger", "biggest", "large", "largest", "size", "wide", "diameter", "across", "radius"))
                return Stat(s, new[] { "diameter", "width" }, v => $"{s.Name} is about {v} across.");

            if (Has(q, "far", "distance", "away", "close"))
                return Stat(s, new[] { "distance", "orbit", "location", "altitude" }, v => $"{s.Name} is {LowerLead(v)}.");

            if (Has(q, "hot", "temperature", "temp", "cold", "warm"))
                return Stat(s, new[] { "temp" }, v => $"{s.Name}’s surface is around {v}.");

            if (Has(q, "moon", "moons"))
                return Stat(s, new[] { "moon" }, v => MoonSentence(s.Name, v));

            if (Has(q, "day", "rotate", "rotates", "spin", "spins"))
                return Stat(s, new[] { "day" }, v => $"A day on {s.Name} lasts about {v}.");

            if (Has(q, "mass", "heavy", "weigh", "weight"))
                return Stat(s, new[] { "mass" }, v => $"{s.Name} has a mass of about {v}.");

            if (Has(q, "star", "stars"))
                return Stat(s, new[] { "stars" }, v => $"{s.Name} holds about {v} stars.");

            return null;
        }

        private static string MoonSentence(string name, string v)
        {
            string word = v.Trim() == "1" ? "moon" : "moons";
            return $"{name} has {v} {word}.";
        }

        private static string? Stat(SpaceObject s, string[] labelKeys, Func<string, string> render)
        {
            foreach (var (label, value) in SpaceStats(s))
            {
                string l = label.ToLowerInvariant();
                if (labelKeys.Any(k => l.Contains(k)) && !string.IsNullOrWhiteSpace(value))
                    return render(value);
            }
            return null;
        }

        private static IEnumerable<(string label, string value)> SpaceStats(SpaceObject s)
        {
            yield return (s.Stat1Label, s.Stat1Value);
            yield return (s.Stat2Label, s.Stat2Value);
            yield return (s.Stat3Label, s.Stat3Value);
            yield return (s.Stat4Label, s.Stat4Value);
        }

        // ---------- comparisons ----------

        private static string Compare(Dinosaur a, Dinosaur b, string q)
        {
            if (a.Name == b.Name) return $"That’s the same creature twice — {a.Name} would just be looking in a mirror!";

            if (Has(q, "long", "longer", "longest") && !q.Contains("how long ago"))
                return Dimension(a, b, x => x.Length, "longer", "long");
            if (Has(q, "big", "bigger", "biggest", "large", "larger", "size"))
                return Dimension(a, b, x => x.Length, "bigger", "long");
            if (Has(q, "tall", "taller", "tallest"))
                return Dimension(a, b, x => x.Height, "taller", "tall");
            if (Has(q, "heavy", "heavier", "heaviest", "weigh", "weight"))
                return Dimension(a, b, x => x.Weight, "heavier", "");
            if (Has(q, "fast", "faster", "fastest", "speed", "quick"))
                return Dimension(a, b, x => x.Speed, "faster", "");
            if (Has(q, "strong", "stronger", "strongest") || q.Contains("bite"))
                return BiteDimension(a, b);

            // General "who would win" — a fair composite of size, weight, bite, speed.
            double pa = Power(a), pb = Power(b);
            Dinosaur win = pa >= pb ? a : b, lose = pa >= pb ? b : a;
            var sb = new StringBuilder();
            sb.Append($"It would be close, but {win.Name} likely has the edge over {lose.Name}. ");
            string edge = EdgeReason(win, lose);
            if (edge.Length > 0) sb.Append(edge + " ");
            sb.Append("In real life the winner would also come down to surprise, terrain, and a bit of luck!");
            return sb.ToString();
        }

        private static string Dimension(Dinosaur a, Dinosaur b, Func<Dinosaur, string> stat, string word, string unitWord)
        {
            double va = Num(stat(a)), vb = Num(stat(b));
            if (va == 0 || vb == 0) return Compare(a, b, ""); // fall back to composite
            Dinosaur big = va >= vb ? a : b, small = va >= vb ? b : a;
            string bigV = Pretty(stat(big)), smallV = Pretty(stat(small));
            string tail = string.IsNullOrEmpty(unitWord) ? "" : " " + unitWord;
            return $"{big.Name} was {word} — about {bigV}{tail}, next to {small.Name}’s {smallV}{tail}.";
        }

        private static string BiteDimension(Dinosaur a, Dinosaur b)
        {
            double va = Num(a.BiteForce), vb = Num(b.BiteForce);
            if (va == 0 && vb == 0) return "Neither one was famous for its bite, so this fight would come down to size and speed.";
            if (va == 0) return $"{b.Name} wins on bite — around {Pretty(b.BiteForce)}, while {a.Name} wasn’t known for a strong bite.";
            if (vb == 0) return $"{a.Name} wins on bite — around {Pretty(a.BiteForce)}, while {b.Name} wasn’t known for a strong bite.";
            Dinosaur big = va >= vb ? a : b, small = va >= vb ? b : a;
            return $"{big.Name} had the stronger bite — about {Pretty(big.BiteForce)}, next to {small.Name}’s {Pretty(small.BiteForce)}.";
        }

        private static string EdgeReason(Dinosaur w, Dinosaur l)
        {
            if (Num(w.BiteForce) > Num(l.BiteForce) && !string.IsNullOrEmpty(w.BiteForce))
                return $"Its bite force of about {Pretty(w.BiteForce)} is a serious weapon.";
            if (Num(w.Weight) > Num(l.Weight))
                return $"At around {Pretty(w.Weight)}, its size and power tip the balance.";
            if (Num(w.Speed) > Num(l.Speed))
                return $"Its speed of about {Pretty(w.Speed)} lets it control the fight.";
            return "";
        }

        private static double Power(Dinosaur d)
        {
            double maxLen = DinoData.All.Max(x => Num(x.Length));
            double maxW = DinoData.All.Max(x => Num(x.Weight));
            double maxBite = DinoData.All.Max(x => Num(x.BiteForce));
            double maxSpd = DinoData.All.Max(x => Num(x.Speed));
            double score = 0;
            if (maxLen > 0) score += Num(d.Length) / maxLen * 30;
            if (maxW > 0) score += Num(d.Weight) / maxW * 30;
            if (maxBite > 0) score += Num(d.BiteForce) / maxBite * 30;
            if (maxSpd > 0) score += Num(d.Speed) / maxSpd * 10;
            return score;
        }

        // ---------- entity fallback (best-matching sentence + intro) ----------

        private static string EntityAnswer(string q, string name, List<string> sentences, string intro)
        {
            string rel = MostRelevant(q, name, sentences);
            return rel.Length > 0 ? rel : intro;
        }

        private static string DinoIntro(Dinosaur d)
        {
            string about = FirstSentences(d.AboutText, 2);
            string fact = FirstFunFact(d.FunFactsText);
            return fact.Length > 0 ? Trim(about + " " + fact) : about;
        }

        private static string SpaceIntro(SpaceObject s)
        {
            string about = FirstSentences(s.AboutText, 2);
            string fact = FirstFunFact(s.FunFactsText);
            return fact.Length > 0 ? Trim(about + " " + fact) : about;
        }

        private static List<string> DinoSentences(Dinosaur d)
        {
            var list = new List<string>();
            list.AddRange(FunFactLines(d.FunFactsText));
            list.AddRange(Sentences(d.KeyFeaturesText));
            list.AddRange(Sentences(d.BehaviourText));
            list.AddRange(Sentences(d.LifeEnvironmentText));
            list.AddRange(Sentences(d.AboutText));
            return list;
        }

        private static List<string> SpaceSentences(SpaceObject s)
        {
            var list = new List<string>();
            list.AddRange(FunFactLines(s.FunFactsText));
            list.AddRange(Sentences(s.KeyFeaturesText));
            list.AddRange(Sentences(s.OrbitMovementText));
            list.AddRange(Sentences(s.SurfaceCompositionText));
            list.AddRange(Sentences(s.WhatsInsideText));
            list.AddRange(Sentences(s.HistoryText));
            list.AddRange(Sentences(s.AboutText));
            return list;
        }

        private static readonly HashSet<string> Stop = new()
        {
            "the","a","an","is","are","was","were","did","do","does","how","what","why","when","where",
            "who","which","of","to","in","on","for","and","or","it","its","they","them","their","that",
            "this","these","those","tell","me","about","you","your","have","has","had","would","could",
            "can","will","like","really","actually","some","any","kind","type","one","there","here","get",
            "got","just","so","then","than","with","as","at","by","from","if","but","much","many","big","old"
        };

        // Finds the sentence from the entry that best matches the question's
        // content words, so the answer speaks to what was actually asked.
        private static string MostRelevant(string q, string name, List<string> sentences)
        {
            var nameWords = Retriever.Normalize(name).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            var keys = Retriever.Normalize(q)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 3 && !Stop.Contains(t) && !nameWords.Contains(t))
                .Distinct().ToList();
            if (keys.Count == 0) return "";

            string best = ""; int bestScore = 0;
            foreach (var raw in sentences)
            {
                string norm = " " + Retriever.Normalize(raw) + " ";
                int score = keys.Count(k => norm.Contains(" " + k));
                if (score > bestScore || (score == bestScore && score > 0 && best.Length > raw.Length))
                { bestScore = score; best = raw; }
            }
            return bestScore >= 1 ? Clean(best) : "";
        }

        private static string RelevantFact(string q, Dinosaur d)
            => MostRelevant(q, d.Name, FunFactLines(d.FunFactsText).ToList());

        private static string SizeFact(Dinosaur d)
        {
            foreach (var line in FunFactLines(d.FunFactsText))
            {
                string l = line.ToLowerInvariant();
                if (l.Contains("long") || l.Contains("tall") || l.Contains("elephant") || l.Contains("bus") ||
                    l.Contains("court") || l.Contains("weigh") || l.Contains("ton") || l.Contains("feet") || l.Contains("size"))
                    return Clean(line);
            }
            return "";
        }

        private static string BiteFlavor(Dinosaur d)
        {
            foreach (var line in FunFactLines(d.FunFactsText))
            {
                string l = line.ToLowerInvariant();
                if (l.Contains("bite") || l.Contains("bone") || l.Contains("crush")) return Clean(line);
            }
            return "";
        }

        private static string SpeedFlavor(Dinosaur d)
        {
            foreach (var line in FunFactLines(d.FunFactsText))
            {
                string l = line.ToLowerInvariant();
                if (l.Contains("fast") || l.Contains("run") || l.Contains("speed")) return Clean(line);
            }
            return "";
        }

        private static string AgeSentence(Dinosaur d)
        {
            foreach (var sen in Sentences(d.AboutText))
            {
                string l = sen.ToLowerInvariant();
                if (l.Contains("years ago") || l.Contains("million years")) return Clean(sen);
            }
            return "";
        }

        // ---------- text helpers ----------

        private static bool Has(string q, params string[] words)
        {
            string p = " " + q + " ";
            return words.Any(w => p.Contains(" " + w + " ") || (w.Contains(' ') && q.Contains(w)));
        }

        private static bool Has(string v) => !string.IsNullOrWhiteSpace(v);

        // Turns "7500kg" -> "7,500 kg", "20km/h" -> "20 km/h", keeps "42 feet".
        private static string Pretty(string stat)
        {
            if (string.IsNullOrWhiteSpace(stat)) return stat;
            stat = stat.Trim();
            int i = 0;
            while (i < stat.Length && (char.IsDigit(stat[i]) || stat[i] == '.' || stat[i] == ',')) i++;
            string num = stat[..i];
            string unit = stat[i..].Trim();
            if (num.Length == 0) return stat;
            if (double.TryParse(num.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) && v >= 1000)
                num = v.ToString("#,0.##", CultureInfo.InvariantCulture);
            return unit.Length == 0 ? num : num + " " + unit;
        }

        private static double Num(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            var sb = new StringBuilder(); bool started = false;
            foreach (char c in s.Replace(",", ""))
            {
                if (char.IsDigit(c) || (c == '.' && started)) { sb.Append(c); started = true; }
                else if (started) break;
            }
            return sb.Length > 0 && double.TryParse(sb.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
        }

        private static IEnumerable<string> FunFactLines(string funFacts)
        {
            if (string.IsNullOrWhiteSpace(funFacts)) yield break;
            foreach (var raw in funFacts.Split('\n'))
            {
                var line = raw.TrimStart('•', ' ').Trim();
                if (line.Length > 0) yield return line;
            }
        }

        private static List<string> Sentences(string text)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return list;
            var sb = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                sb.Append(c);
                if (c is '.' or '!' or '?')
                {
                    bool decimalPoint = c == '.' && i + 1 < text.Length && char.IsDigit(text[i + 1]) && i > 0 && char.IsDigit(text[i - 1]);
                    bool initial = c == '.' && i > 0 && char.IsUpper(text[i - 1]) && (i < 2 || text[i - 2] == ' ');
                    if (!decimalPoint && !initial)
                    {
                        string s = sb.ToString().Trim();
                        if (s.Length > 0) list.Add(s);
                        sb.Clear();
                    }
                }
            }
            string tail = sb.ToString().Trim();
            if (tail.Length > 0) list.Add(tail);
            return list;
        }

        private static string FirstSentence(string text)
        {
            var s = Sentences(text);
            return s.Count > 0 ? Clean(s[0]) : "";
        }

        private static string FirstSentenceLower(string text)
        {
            string s = FirstSentence(text);
            if (s.Length == 0) return "a variety of habitats.";
            return char.ToLowerInvariant(s[0]) + s[1..];
        }

        private static string FirstSentences(string text, int n)
        {
            var s = Sentences(text);
            return string.Join(" ", s.Take(n).Select(Clean)).Trim();
        }

        private static string FirstFunFact(string funFacts) => FunFactLines(funFacts).Select(Clean).FirstOrDefault() ?? "";

        private static string LowerLead(string v)
        {
            v = v.Trim();
            return v.Length == 0 ? v : char.ToLowerInvariant(v[0]) + v[1..];
        }

        private static string Clean(string s) => (s ?? "").Trim().TrimStart('•', ' ').Trim();

        private static string Trim(string s)
        {
            s = (s ?? "").Trim();
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            return s.Trim();
        }
    }
}
