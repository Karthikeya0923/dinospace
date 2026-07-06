using System;
using System.Collections.Generic;
using System.Linq;

namespace dinospace
{
    // One annual meteor shower: when it runs, where it radiates from, and how
    // strong it gets (ZHR = meteors/hour under a perfect dark sky at peak).
    public sealed class MeteorShower
    {
        public string Name = "";
        public string Parent = "";           // the comet/asteroid that shed the debris
        public int StartMonth, StartDay;
        public int EndMonth, EndDay;
        public int PeakMonth, PeakDay;
        public int Zhr;
        public double RadiantRaHours;
        public double RadiantDecDeg;
        public string Blurb = "";

        public DateTime PeakOn(int year) => new(year, PeakMonth, PeakDay, 0, 0, 0, DateTimeKind.Utc);

        public bool ActiveOn(DateTime date)
        {
            var d = (date.Month, date.Day);
            var s = (StartMonth, StartDay);
            var e = (EndMonth, EndDay);
            // showers like the Quadrantids straddle New Year
            bool wraps = s.CompareTo(e) > 0;
            return wraps ? d.CompareTo(s) >= 0 || d.CompareTo(e) <= 0
                         : d.CompareTo(s) >= 0 && d.CompareTo(e) <= 0;
        }
    }

    // The major annual showers (IMO calendar values, rounded to the usual
    // peak night). Peak dates drift by ±1 day year to year.
    public static class MeteorShowers
    {
        public static readonly IReadOnlyList<MeteorShower> All = new List<MeteorShower>
        {
            new() { Name = "Quadrantids", Parent = "asteroid 2003 EH1", StartMonth = 12, StartDay = 28, EndMonth = 1, EndDay = 12, PeakMonth = 1, PeakDay = 3, Zhr = 110, RadiantRaHours = 15.3, RadiantDecDeg = 49.5, Blurb = "A short, sharp New Year burst — blink and you miss the peak" },
            new() { Name = "Lyrids", Parent = "comet Thatcher", StartMonth = 4, StartDay = 14, EndMonth = 4, EndDay = 30, PeakMonth = 4, PeakDay = 22, Zhr = 18, RadiantRaHours = 18.1, RadiantDecDeg = 34, Blurb = "Spring's classic shower, watched for 2,700 years" },
            new() { Name = "Eta Aquariids", Parent = "Halley's Comet", StartMonth = 4, StartDay = 19, EndMonth = 5, EndDay = 28, PeakMonth = 5, PeakDay = 6, Zhr = 50, RadiantRaHours = 22.5, RadiantDecDeg = -1, Blurb = "Dust from Halley's Comet — best before dawn" },
            new() { Name = "Delta Aquariids", Parent = "comet 96P/Machholz", StartMonth = 7, StartDay = 12, EndMonth = 8, EndDay = 23, PeakMonth = 7, PeakDay = 30, Zhr = 25, RadiantRaHours = 22.7, RadiantDecDeg = -16, Blurb = "A steady mid-summer drizzle of meteors" },
            new() { Name = "Perseids", Parent = "comet Swift-Tuttle", StartMonth = 7, StartDay = 17, EndMonth = 8, EndDay = 24, PeakMonth = 8, PeakDay = 12, Zhr = 100, RadiantRaHours = 3.2, RadiantDecDeg = 58, Blurb = "The summer favourite — fast, bright, and plenty of them" },
            new() { Name = "Orionids", Parent = "Halley's Comet", StartMonth = 10, StartDay = 2, EndMonth = 11, EndDay = 7, PeakMonth = 10, PeakDay = 21, Zhr = 20, RadiantRaHours = 6.3, RadiantDecDeg = 16, Blurb = "Halley's other shower, radiating from Orion's club" },
            new() { Name = "Leonids", Parent = "comet Tempel-Tuttle", StartMonth = 11, StartDay = 6, EndMonth = 11, EndDay = 30, PeakMonth = 11, PeakDay = 17, Zhr = 15, RadiantRaHours = 10.1, RadiantDecDeg = 22, Blurb = "Usually modest — but famous for once-in-33-year meteor storms" },
            new() { Name = "Geminids", Parent = "asteroid 3200 Phaethon", StartMonth = 12, StartDay = 4, EndMonth = 12, EndDay = 17, PeakMonth = 12, PeakDay = 14, Zhr = 150, RadiantRaHours = 7.5, RadiantDecDeg = 32, Blurb = "The strongest shower of the year, slow and colourful" },
            new() { Name = "Ursids", Parent = "comet 8P/Tuttle", StartMonth = 12, StartDay = 17, EndMonth = 12, EndDay = 26, PeakMonth = 12, PeakDay = 22, Zhr = 10, RadiantRaHours = 14.5, RadiantDecDeg = 76, Blurb = "A quiet solstice shower from the Little Dipper" },
        };

        // Any showers active on this date.
        public static List<MeteorShower> ActiveOn(DateTime utc) =>
            All.Where(s => s.ActiveOn(utc)).ToList();

        // The next shower peak at or after `utc`, with the exact peak date.
        public static (MeteorShower shower, DateTime peakUtc) Next(DateTime utc)
        {
            MeteorShower? best = null;
            DateTime bestPeak = DateTime.MaxValue;
            foreach (var s in All)
                foreach (int year in new[] { utc.Year, utc.Year + 1 })
                {
                    var peak = s.PeakOn(year);
                    if (peak >= utc.Date && peak < bestPeak) { best = s; bestPeak = peak; }
                }
            return (best!, bestPeak);
        }

        // How badly the moon spoils a given peak night: 0 = new moon (perfect
        // dark sky), 1 = full moon (all but the brightest meteors washed out).
        public static double MoonInterference(MeteorShower shower, int year)
            => SkyCalc.Moon(shower.PeakOn(year).AddHours(23)).Illumination;

        public static string MoonVerdict(double interference) => interference switch
        {
            < 0.25 => "dark skies — great year for it",
            < 0.6 => "some moonlight, still worth watching",
            _ => "bright moon — only the fireballs will punch through"
        };
    }
}
