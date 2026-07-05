using System.Collections.Generic;

namespace dinospace
{
    // The constellation catalogue for Sky Tonight. Each entry is the
    // constellation's approximate centre (J2000 RA/Dec) — precise enough to
    // say where it sits in your sky — plus a one-line hook for kids, and an
    // optional encyclopedia entry it links to.
    public sealed class Constellation
    {
        public string Name = "";
        public double RaHours;       // right ascension of centre, hours
        public double DecDeg;        // declination of centre, degrees
        public string Blurb = "";
        public string? LinkEntry;    // SpaceData entry name, when one exists
    }

    public static class SkyData
    {
        public static readonly List<Constellation> All = new()
        {
            new() { Name = "Orion", RaHours = 5.6, DecDeg = 3, Blurb = "The Hunter — find the three-star belt", LinkEntry = "Orion" },
            new() { Name = "Ursa Major", RaHours = 11.0, DecDeg = 55, Blurb = "Home of the Big Dipper" },
            new() { Name = "Ursa Minor", RaHours = 15.7, DecDeg = 78, Blurb = "The Little Dipper — its handle ends at Polaris, the North Star" },
            new() { Name = "Cassiopeia", RaHours = 1.0, DecDeg = 62, Blurb = "The queen — a bright zigzag W of five stars" },
            new() { Name = "Scorpius", RaHours = 16.9, DecDeg = -27, Blurb = "The scorpion, with red Antares at its heart" },
            new() { Name = "Sagittarius", RaHours = 19.0, DecDeg = -25, Blurb = "The teapot — aim here for the centre of the Milky Way", LinkEntry = "Sagittarius A*" },
            new() { Name = "Cygnus", RaHours = 20.6, DecDeg = 42, Blurb = "The swan, flying along the Milky Way", LinkEntry = "Milky Way" },
            new() { Name = "Lyra", RaHours = 18.8, DecDeg = 36, Blurb = "Small but mighty — home of brilliant Vega" },
            new() { Name = "Aquila", RaHours = 19.7, DecDeg = 3, Blurb = "The eagle, carrying bright Altair" },
            new() { Name = "Taurus", RaHours = 4.5, DecDeg = 16, Blurb = "The bull — its red eye is the star Aldebaran" },
            new() { Name = "Gemini", RaHours = 7.0, DecDeg = 22, Blurb = "The twins, crowned by Castor and Pollux" },
            new() { Name = "Canis Major", RaHours = 6.8, DecDeg = -22, Blurb = "Holds Sirius, the brightest star in the night sky" },
            new() { Name = "Canis Minor", RaHours = 7.6, DecDeg = 6, Blurb = "The little dog, with bright Procyon" },
            new() { Name = "Leo", RaHours = 10.6, DecDeg = 13, Blurb = "The lion — spot the backwards question mark" },
            new() { Name = "Virgo", RaHours = 13.4, DecDeg = -4, Blurb = "The maiden, with blue-white Spica" },
            new() { Name = "Boötes", RaHours = 14.7, DecDeg = 31, Blurb = "The kite-shaped herdsman, with orange Arcturus" },
            new() { Name = "Perseus", RaHours = 3.2, DecDeg = 45, Blurb = "The hero, rich in sparkling star clusters" },
            new() { Name = "Andromeda", RaHours = 0.8, DecDeg = 38, Blurb = "Points the way to the Andromeda Galaxy", LinkEntry = "Andromeda Galaxy" },
            new() { Name = "Pegasus", RaHours = 22.7, DecDeg = 19, Blurb = "The winged horse — find the Great Square" },
            new() { Name = "Auriga", RaHours = 5.9, DecDeg = 42, Blurb = "The charioteer, with golden Capella" },
            new() { Name = "Cepheus", RaHours = 22.0, DecDeg = 65, Blurb = "The king — shaped like a child's drawing of a house" },
            new() { Name = "Draco", RaHours = 17.0, DecDeg = 62, Blurb = "The dragon, coiled around the North Star" },
            new() { Name = "Hercules", RaHours = 17.1, DecDeg = 27, Blurb = "The strongman — look for the four-star Keystone" },
            new() { Name = "Ophiuchus", RaHours = 17.2, DecDeg = -3, Blurb = "The serpent-bearer, a huge summer diamond" },
            new() { Name = "Aries", RaHours = 2.6, DecDeg = 20, Blurb = "The ram, a neat little arc of three stars" },
            new() { Name = "Pisces", RaHours = 0.8, DecDeg = 11, Blurb = "The fishes, a faint V below Pegasus" },
            new() { Name = "Aquarius", RaHours = 22.3, DecDeg = -10, Blurb = "The water-bearer of the watery autumn sky" },
            new() { Name = "Capricornus", RaHours = 21.0, DecDeg = -18, Blurb = "The sea-goat, a wide smile of faint stars" },
            new() { Name = "Cancer", RaHours = 8.7, DecDeg = 20, Blurb = "Faint, but it hides the Beehive star cluster" },
            new() { Name = "Libra", RaHours = 15.2, DecDeg = -15, Blurb = "The scales, between Virgo and Scorpius" },
            new() { Name = "Corona Borealis", RaHours = 15.8, DecDeg = 33, Blurb = "The Northern Crown — a perfect little arc" },
            new() { Name = "Cetus", RaHours = 1.7, DecDeg = -8, Blurb = "The sea monster of the autumn sky" },
            // For explorers south of the equator.
            new() { Name = "Crux", RaHours = 12.4, DecDeg = -60, Blurb = "The Southern Cross — the icon of the southern sky" },
            new() { Name = "Centaurus", RaHours = 13.1, DecDeg = -47, Blurb = "Holds Alpha Centauri, the nearest star system to us" },
            new() { Name = "Carina", RaHours = 8.7, DecDeg = -60, Blurb = "Home of Canopus, the second-brightest star" },
        };
    }
}
