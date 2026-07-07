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
            // The rest of the full roster of 88 official constellations.
            new() { Name = "Hydra", RaHours = 11.6, DecDeg = -14, Blurb = "The water snake — the biggest constellation of all" },
            new() { Name = "Eridanus", RaHours = 3.3, DecDeg = -29, Blurb = "The river, winding from Orion's foot to bright Achernar" },
            new() { Name = "Puppis", RaHours = 7.6, DecDeg = -31, Blurb = "The stern of the great ship Argo" },
            new() { Name = "Vela", RaHours = 9.6, DecDeg = -47, Blurb = "The ship's billowing sails" },
            new() { Name = "Pyxis", RaHours = 8.9, DecDeg = -27, Blurb = "The ship's compass, keeping Argo on course" },
            new() { Name = "Monoceros", RaHours = 7.0, DecDeg = 0, Blurb = "The unicorn, hiding shyly between Orion's dogs" },
            new() { Name = "Lepus", RaHours = 5.6, DecDeg = -19, Blurb = "The hare, crouching right under Orion's feet" },
            new() { Name = "Columba", RaHours = 5.9, DecDeg = -35, Blurb = "The dove, flying just south of the hare" },
            new() { Name = "Canes Venatici", RaHours = 13.0, DecDeg = 40, Blurb = "The hunting dogs, chasing the bears around the pole" },
            new() { Name = "Coma Berenices", RaHours = 12.8, DecDeg = 23, Blurb = "A queen's sparkling hair — really a nearby star cluster" },
            new() { Name = "Leo Minor", RaHours = 10.2, DecDeg = 32, Blurb = "The lion cub, padding along above big Leo" },
            new() { Name = "Lynx", RaHours = 8.0, DecDeg = 47, Blurb = "So faint you supposedly need a lynx's eyes to see it" },
            new() { Name = "Sextans", RaHours = 10.3, DecDeg = -2, Blurb = "An astronomer's sextant, resting below Leo" },
            new() { Name = "Crater", RaHours = 11.4, DecDeg = -15, Blurb = "The cup, balanced on the water snake's back" },
            new() { Name = "Corvus", RaHours = 12.4, DecDeg = -18, Blurb = "The crow — a neat little box of four stars" },
            new() { Name = "Camelopardalis", RaHours = 6.0, DecDeg = 69, Blurb = "The giraffe, tall and faint near the North Star" },
            new() { Name = "Lacerta", RaHours = 22.5, DecDeg = 46, Blurb = "The little lizard, zigzagging between Cygnus and Andromeda" },
            new() { Name = "Triangulum", RaHours = 2.2, DecDeg = 31, Blurb = "A slim triangle pointing to a faraway spiral galaxy" },
            new() { Name = "Delphinus", RaHours = 20.7, DecDeg = 12, Blurb = "The dolphin — a tiny diamond leaping out of the Milky Way" },
            new() { Name = "Equuleus", RaHours = 21.2, DecDeg = 8, Blurb = "The little horse, the second-smallest constellation" },
            new() { Name = "Sagitta", RaHours = 19.7, DecDeg = 18, Blurb = "The arrow, shot right between the swan and the eagle" },
            new() { Name = "Vulpecula", RaHours = 20.2, DecDeg = 24, Blurb = "The fox, carrying the Dumbbell Nebula in its jaws" },
            new() { Name = "Scutum", RaHours = 18.7, DecDeg = -10, Blurb = "The shield, set in one of the Milky Way's brightest clouds" },
            new() { Name = "Serpens", RaHours = 15.8, DecDeg = 8, Blurb = "The serpent — the only constellation split into two halves" },
            new() { Name = "Corona Australis", RaHours = 18.6, DecDeg = -41, Blurb = "The Southern Crown, curled under the teapot" },
            new() { Name = "Piscis Austrinus", RaHours = 22.3, DecDeg = -30, Blurb = "The southern fish, drinking with bright Fomalhaut" },
            new() { Name = "Sculptor", RaHours = 0.4, DecDeg = -32, Blurb = "The sculptor's studio, home to the south galactic pole" },
            new() { Name = "Fornax", RaHours = 2.8, DecDeg = -30, Blurb = "The furnace, cradled in a bend of the river Eridanus" },
            new() { Name = "Caelum", RaHours = 4.7, DecDeg = -38, Blurb = "The engraver's chisel — tiny and faint" },
            new() { Name = "Horologium", RaHours = 3.3, DecDeg = -53, Blurb = "The pendulum clock, ticking beside the river" },
            new() { Name = "Reticulum", RaHours = 3.9, DecDeg = -60, Blurb = "The crosshair — a little diamond of southern stars" },
            new() { Name = "Pictor", RaHours = 5.7, DecDeg = -53, Blurb = "The painter's easel, next to brilliant Canopus" },
            new() { Name = "Dorado", RaHours = 5.2, DecDeg = -60, Blurb = "The dolphinfish, swimming with the Large Magellanic Cloud" },
            new() { Name = "Mensa", RaHours = 5.5, DecDeg = -77, Blurb = "Table Mountain — the only constellation named after a place on Earth" },
            new() { Name = "Volans", RaHours = 7.8, DecDeg = -69, Blurb = "The flying fish, gliding beside the ship's keel" },
            new() { Name = "Chamaeleon", RaHours = 10.7, DecDeg = -79, Blurb = "The chameleon, blending in near the south pole" },
            new() { Name = "Musca", RaHours = 12.6, DecDeg = -70, Blurb = "The fly, buzzing just south of the Southern Cross" },
            new() { Name = "Circinus", RaHours = 14.6, DecDeg = -63, Blurb = "The drawing compasses, tucked beside Alpha Centauri" },
            new() { Name = "Lupus", RaHours = 15.2, DecDeg = -43, Blurb = "The wolf, prowling between the centaur and the scorpion" },
            new() { Name = "Norma", RaHours = 16.0, DecDeg = -51, Blurb = "The set square, laid on a rich stretch of Milky Way" },
            new() { Name = "Triangulum Australe", RaHours = 16.1, DecDeg = -65, Blurb = "The Southern Triangle — brighter than its northern twin" },
            new() { Name = "Ara", RaHours = 17.3, DecDeg = -55, Blurb = "The altar, smoking beneath the scorpion's tail" },
            new() { Name = "Apus", RaHours = 16.0, DecDeg = -76, Blurb = "The bird-of-paradise, deep in the far south" },
            new() { Name = "Telescopium", RaHours = 19.3, DecDeg = -51, Blurb = "The telescope, pointed below the Southern Crown" },
            new() { Name = "Pavo", RaHours = 19.6, DecDeg = -66, Blurb = "The peacock, showing off its bright namesake star" },
            new() { Name = "Octans", RaHours = 21.0, DecDeg = -85, Blurb = "Holds the south celestial pole — the southern sky spins around it" },
            new() { Name = "Indus", RaHours = 21.3, DecDeg = -60, Blurb = "A quiet southern constellation with a very close, sun-like star" },
            new() { Name = "Microscopium", RaHours = 21.0, DecDeg = -36, Blurb = "The microscope — you nearly need one to spot it" },
            new() { Name = "Grus", RaHours = 22.5, DecDeg = -46, Blurb = "The crane, striding on long starry legs" },
            new() { Name = "Phoenix", RaHours = 0.9, DecDeg = -48, Blurb = "The firebird, rising anew beside bright Achernar" },
            new() { Name = "Tucana", RaHours = 23.8, DecDeg = -64, Blurb = "The toucan, keeping the Small Magellanic Cloud under its wing" },
            new() { Name = "Hydrus", RaHours = 2.3, DecDeg = -70, Blurb = "The little water snake, wriggling between the Magellanic Clouds" },
            new() { Name = "Antlia", RaHours = 10.2, DecDeg = -33, Blurb = "The air pump — proof astronomers name constellations after anything" },
        };
    }
}
