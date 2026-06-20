namespace dinospace
{
    public static class SpaceData
    {
        public static List<SpaceObject> GetAll()
        {
            return new List<SpaceObject>
            {
                new SpaceObject
                {
                    Name = "Moon",
                    Pronunciation = "moon",
                    Subtitle = "Earth's Natural Satellite",
                    TypeLabel = "Moon",
                    Category = "Solar System",
                    ShortDescription = "Earth's only natural satellite",
                    Stat1Label = "Diameter", Stat1Value = "3475km",
                    Stat2Label = "Distance", Stat2Value = "384,400km from Earth",
                    Stat3Label = "Mass", Stat3Value = "7.35×10²² kg",
                    Stat4Label = "Gravity", Stat4Value = "1.62 m/s²",
                    ImageFile = "moon.png",
                    AboutText = "The Moon formed 4.5 billion years ago and has orbited Earth ever since. It's the second-brightest object in our sky after the Sun and is what creates our ocean tides. The Moon doesn't have an atmosphere capable of supporting life and experiences extreme temperature differences between day and night.",
                    KeyFeaturesText = "The Moon consists of rugged highlands, impact craters, and dark plains. Its surface is coated with fine dust known as regolith. The Moon is tidally locked to Earth, so it always shows the same side to our planet.",
                    OrbitMovementText = "The Moon completes one orbit around Earth around every 27 days. Due to its rotation period matching its orbital period, we always see the same side of the Moon. Its gravitational pull slightly stabilizes Earth's axial tilt.",
                    SurfaceCompositionText = "The Moon's surface consists of silicate rock and dust. The lighter areas consist of anorthosite, and the darker areas have ancient basaltic lava flows. These were formed billions of years ago by volcanic activity.",
                    FunFactsText = "• The Moon is slowly drifting away from Earth at around 4cm each year!\n• The Moon is the reason we have a 24 hour day — without it, Earth would spin so fast a day would only last 6 hours.\n• We could actually fit every planet in our solar system between Earth and our Moon!\n• There's water on the Moon, in the form of ice trapped under the surface.\n• The Moon was made when a giant rock smashed into our Earth. Over time, due to gravity, it turned into a sphere."
                },
                new SpaceObject
                {
                    Name = "Earth",
                    Pronunciation = "urth",
                    Subtitle = "Our Home Planet",
                    TypeLabel = "Planet",
                    Category = "Solar System",
                    ShortDescription = "The only known planet with life",
                    Stat1Label = "Diameter", Stat1Value = "12,750km",
                    Stat2Label = "Distance", Stat2Value = "150 million km from Sun",
                    Stat3Label = "Mass", Stat3Value = "5.97×10²⁴ kg",
                    Stat4Label = "Gravity", Stat4Value = "9.81 m/s²",
                    ImageFile = "earth.png",
                    AboutText = "Earth formed 4.5 billion years ago when gravity pulled clouds of dust and gas together, eventually forming the sphere we now call home. It is the only world known to support life, due to water, breathable atmosphere, and habitable temperatures.",
                    KeyFeaturesText = "Earth has large oceans, continents, mountains, deserts, forests, and poles. Harmful solar radiation is kept away by its atmosphere and helps regulate temperature. Earth also has strong magnetic fields which shield the planet from the Sun's charged particles.",
                    OrbitMovementText = "Earth completes one orbit around the Sun every 365.25 days, and rotates every 24 hours. This rotation creates our day and night, while its tilted axis causes seasons. The Moon's gravitational pull also contributes to Earth's ocean tides.",
                    SurfaceCompositionText = "Earth's surface is made of rock, soil, ice, and water. Its crust is divided into moving plates which slowly shift over time. Under the crust are the mantle, outer core, and inner core. The core consists mostly of iron and nickel.",
                    FunFactsText = "• About 71% of Earth's surface is covered by water.\n• Earth's core is as hot as the surface of our Sun.\n• Earth is not a perfect sphere — it bulges slightly at the equator.\n• Light from the Sun takes about 8 minutes to reach Earth.\n• Earth is the only planet in our solar system with one moon."
                },
                new SpaceObject
                {
                    Name = "Mars",
                    Pronunciation = "maarz",
                    Subtitle = "The Red Planet",
                    TypeLabel = "Planet",
                    Category = "Solar System",
                    ShortDescription = "A deserted wasteland",
                    Stat1Label = "Diameter", Stat1Value = "6779km",
                    Stat2Label = "Distance", Stat2Value = "225 million km from Earth",
                    Stat3Label = "Mass", Stat3Value = "6.42×10²³ kg",
                    Stat4Label = "Gravity", Stat4Value = "3.72 m/s²",
                    ImageFile = "mars.png",
                    AboutText = "Mars formed around 4.5 billion years ago when gravity pulled cosmic dust, gas, and debris together into the fourth planet from the Sun. It is often called the Red Planet due to the iron-rich minerals in its soil which have rusted over time, giving the surface its distinctive red color.",
                    KeyFeaturesText = "Mars is home to enormous volcanoes, polar ice caps, vast deserts, and deep canyons. It contains Olympus Mons, the largest volcano in the solar system, nearly three times as tall as Mount Everest. Mars also has two small moons named Phobos and Deimos.",
                    OrbitMovementText = "Mars completes one orbit around the Sun every 687 Earth days and rotates once every 24.6 hours. Its tilted axis causes seasons similar to those on Earth, though they last longer due to Mars having a longer year.",
                    SurfaceCompositionText = "Mars has a rocky surface covered with sand, stones, and iron oxide dust. Under the surface are layers of frozen water and volcanic rock. The planet's thin atmosphere is made mostly of carbon dioxide, making it unable to support human life without technology.",
                    FunFactsText = "• Every 26 months, Mars is at its closest to Earth, and is visible all night.\n• Powerful dust storms can sometimes cover the entire planet.\n• Although Mars is cold and dry today, evidence suggests that liquid water once flowed across its surface in the form of rivers and lakes.\n• Because its core cooled down, Mars lost its magnetic field and most of its water.\n• Humans have sent rovers to Mars to study the planet."
                },
                new SpaceObject
                {
                    Name = "Sun",
                    Pronunciation = "sun",
                    Subtitle = "Our Star",
                    TypeLabel = "Star",
                    Category = "Stars",
                    ShortDescription = "The star at the center of our solar system",
                    Stat1Label = "Diameter", Stat1Value = "1.39 million km",
                    Stat2Label = "Distance", Stat2Value = "150 million km from Earth",
                    Stat3Label = "Mass", Stat3Value = "1.99×10³⁰ kg",
                    Stat4Label = "Gravity", Stat4Value = "274 m/s²",
                    ImageFile = "sun.png",
                    AboutText = "The Sun formed around 4.6 billion years ago when a giant cloud of gas and dust collapsed under its own gravity. It is the star at the center of our solar system and contains over 99% of all the mass in it. The Sun provides the light and heat that make life on Earth possible.",
                    KeyFeaturesText = "The Sun is made mostly of hydrogen and helium. Its surface, called the photosphere, is covered in constantly moving gas. The Sun also produces solar flares and powerful streams of charged particles known as the solar wind.",
                    OrbitMovementText = "The Sun rotates on its axis, taking about 25 days at its equator and around 35 days near its poles. It also travels through the Milky Way galaxy, completing one orbit around the galaxy approximately every 230 million years.",
                    SurfaceCompositionText = "The Sun is made of about 74% hydrogen and 24% helium, with small amounts of heavier elements. Deep within its core, hydrogen is converted into helium through nuclear fusion, releasing enormous amounts of energy that travel outward as heat and light.",
                    FunFactsText = "• Over one million Earths could fit inside the Sun.\n• One orbit around the Milky Way takes the Sun 230 million years — scientists call this a Galactic Year.\n• Light from the Sun takes about 8 minutes and 20 seconds to reach Earth.\n• The Sun's core reaches temperatures of around 15 million degrees Celsius.\n• Every second, the Sun converts about 600 million tons of hydrogen into energy."
                },
                new SpaceObject
                {
                    Name = "Orion",
                    Pronunciation = "uh-rye-un",
                    Subtitle = "The Hunter",
                    TypeLabel = "Constellation",
                    Category = "Stars",
                    ShortDescription = "One of the brightest and most recognizable constellations",
                    Stat1Label = "Distance", Stat1Value = "1,344 light-years from Earth",
                    Stat2Label = "Stars", Stat2Value = "7 main stars",
                    Stat3Label = "Best Visible", Stat3Value = "December to February",
                    Stat4Label = "Location", Stat4Value = "Celestial Equator",
                    ImageFile = "orion.png",
                    AboutText = "Orion is one of the most famous constellations in the night sky and has been recognized by cultures around the world for thousands of years. It is easily identified by the three bright stars that form Orion's Belt. It lies on the celestial equator, making it visible from both the Northern and Southern Hemispheres.",
                    KeyFeaturesText = "Orion contains many remarkable objects, including the bright star Rigel. It is also home to the Orion Nebula, a large cloud of gas and dust where new stars are being born. The three stars of Orion's Belt point toward other famous stars and constellations, making Orion a useful guide for stargazers.",
                    HistoryText = "Orion has been recognized by civilizations for thousands of years and appears in the myths and legends of many cultures. The constellation is named after a hunter from Greek mythology and has been used throughout history for navigation, storytelling, and astronomy.",
                    WhatsInsideText = "Orion is also home to the Orion Nebula, a massive cloud of gas and dust where new stars are forming, along with several star clusters and other nebulae. It visually spreads around 17 degrees wide in the night sky.",
                    FunFactsText = "• Orion's Belt consists of three bright stars lined up almost perfectly.\n• The red star Betelgeuse is a massive supergiant that could explode as a supernova in the future.\n• The Orion Nebula is one of the closest stellar nurseries to Earth.\n• Orion sits directly on the celestial equator, making it a useful navigation tool to tell time.\n• Orion typically disappears from the evening sky in late April or May."
                },
                new SpaceObject
                {
                    Name = "Andromeda Galaxy",
                    Pronunciation = "an-draa-muh-duh ga-luhk-see",
                    Subtitle = "Our Nearest Major Galaxy",
                    TypeLabel = "Galaxy",
                    Category = "Deep Space",
                    ShortDescription = "The closest large galaxy to the Milky Way",
                    Stat1Label = "Diameter", Stat1Value = "220,000 light-years",
                    Stat2Label = "Distance", Stat2Value = "2.5 million light-years from Earth",
                    Stat3Label = "Stars", Stat3Value = "1 trillion",
                    Stat4Label = "Speed", Stat4Value = "110 km/s toward Milky Way",
                    ImageFile = "andromedagalaxy.png",
                    AboutText = "The Andromeda Galaxy formed billions of years ago and is one of the largest galaxies in our local galactic neighbourhood. It is a massive spiral galaxy similar to the Milky Way and is one of the most distant objects visible to the naked eye under perfectly dark skies. The light we see from Andromeda today began its journey before humans even existed.",
                    KeyFeaturesText = "Andromeda is significantly larger than the Milky Way, as it contains around a trillion stars. It has bright spiral arms, a dense central core, and many smaller galaxies orbiting around it. Powerful regions of star formation and large clouds of gas and dust can also be found throughout the galaxy.",
                    OrbitMovementText = "Scientists predict that Andromeda and the Milky Way will begin merging in about 4.5 billion years. Although this sounds dramatic, the immense distances between stars mean direct stellar collisions will be extremely rare.",
                    SurfaceCompositionText = "Like most spiral galaxies, Andromeda is made up of stars, planets, gas, dust, dark matter, and black holes. Its central region contains a supermassive black hole millions of times more massive than our Sun. Large amounts of hydrogen gas throughout the galaxy provide the raw material for new stars to form.",
                    FunFactsText = "• Andromeda is the most distant object most people can see without a telescope.\n• Andromeda is on a collision course with the Milky Way, but the night sky will look dramatically different long before they actually merge.\n• Andromeda contains roughly twice as many stars as the Milky Way, making it the dominant galaxy in our local group.\n• It may have eaten another large galaxy several billion years ago.\n• The collision with the Milky Way has a nickname, Milkomeda."
                },
                new SpaceObject
                {
                    Name = "Phoenix A*",
                    Pronunciation = "fee-niks a-star",
                    Subtitle = "Ultramassive Black Hole in Phoenix A Galaxy Cluster",
                    TypeLabel = "Black Hole",
                    Category = "Deep Space",
                    ShortDescription = "The most massive black hole ever discovered",
                    Stat1Label = "Diameter", Stat1Value = "590 billion km",
                    Stat2Label = "Distance", Stat2Value = "5.8 billion light-years from Earth",
                    Stat3Label = "Location", Stat3Value = "Phoenix A galaxy cluster",
                    Stat4Label = "Mass", Stat4Value = "100 billion solar masses",
                    ImageFile = "phoenixastar.png",
                    AboutText = "Phoenix A* is one of the most massive black holes ever measured, located at the center of the Phoenix A galaxy cluster around 5.8 billion light-years from Earth. It contains an estimated mass equivalent to roughly 100 billion suns, though this figure carries scientific uncertainty and continues to be studied. It is actively growing by consuming enormous amounts of surrounding gas and matter.",
                    KeyFeaturesText = "Phoenix A* sits in the dense core of a massive galaxy cluster and is surrounded by vast clouds of superheated gas. This material flows inward and feeds the black hole, generating intense radiation and shaping the evolution of the entire cluster around it.",
                    OrbitMovementText = "Phoenix A* has no orbiting surface, but its extreme gravity governs the motion of nearby stars, gas, and entire galaxies. Material in its vicinity follows chaotic high-speed paths before spiralling inward and crossing the event horizon, the point of no return.",
                    SurfaceCompositionText = "Phoenix A* has no physical surface. It is defined by an event horizon, a boundary where gravity becomes so strong that nothing, including light, can escape. Beyond this boundary lies a singularity where known physics breaks down.",
                    FunFactsText = "• Phoenix A* triggers a starburst effect, causing its galaxy to form stars around 700 times faster than the Milky Way.\n• Its gravity influences galaxies across enormous distances within the cluster.\n• It devours the equivalent of roughly 60 stars every year.\n• Phoenix A* is so large that if it replaced our Sun, it would engulf every planet in the solar system and extend far beyond.\n• Objects falling toward it would be stretched into long thin streams in a process called spaghettification."
                }
            };
        }
    }
}