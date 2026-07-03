using System.Collections.Generic;

namespace dinospace.Data
{
    // A curated fact snippet used to ground NovaSaur on common questions that
    // don't map to a single encyclopedia entry (extinction, how stars form,
    // what a light-year is). Retrieval matches on Keywords, and the Fact text
    // is dropped into the prompt's NOTES so the small model answers correctly.
    public class KnowledgeNugget
    {
        public string Topic { get; set; } = "";
        public string[] Keywords { get; set; } = System.Array.Empty<string>();
        public string Fact { get; set; } = "";
    }

    public static class KnowledgeBase
    {
        public static readonly List<KnowledgeNugget> Nuggets = new()
        {
            new() { Topic = "Dinosaur extinction",
                Keywords = new[] { "extinct", "extinction", "die out", "died out", "wiped out", "asteroid", "why did the dinosaurs", "end of the dinosaurs", "how did the dinosaurs die", "meteor" },
                Fact = "About 66 million years ago a huge asteroid, roughly 10km wide, hit Earth near what is now Mexico. It threw dust and soot into the sky that blocked sunlight for years, cooling the planet and killing off the plants many dinosaurs ate. This ended the age of the non-bird dinosaurs. Birds are the dinosaurs that survived." },
            new() { Topic = "Birds are dinosaurs",
                Keywords = new[] { "birds dinosaurs", "are birds dinosaurs", "birds evolved", "living dinosaurs", "did dinosaurs turn into birds", "closest to dinosaurs" },
                Fact = "Birds evolved from small feathered theropod dinosaurs, so scientists say birds ARE dinosaurs. The chicken and the T. Rex are distant cousins. This is why we call birds 'living dinosaurs'." },
            new() { Topic = "How stars form",
                Keywords = new[] { "how stars form", "how are stars made", "how do stars form", "star born", "stars are born", "star formation", "how is a star made" },
                Fact = "Stars form inside giant clouds of gas and dust called nebulae. Gravity slowly pulls the gas into a dense clump. As it squeezes together it heats up, and when the center gets hot enough, nuclear fusion begins and the star starts to shine." },
            new() { Topic = "What is a light-year",
                Keywords = new[] { "light year", "light-year", "lightyear", "what is a light year", "how far is a light year" },
                Fact = "A light-year is a distance, not a time. It is how far light travels in one year — about 9.5 trillion kilometres. Because space is so huge, astronomers measure distances in light-years." },
            new() { Topic = "What is a black hole",
                Keywords = new[] { "what is a black hole", "how do black holes work", "black hole", "how are black holes made", "what happens in a black hole" },
                Fact = "A black hole is a place where gravity is so strong that nothing, not even light, can escape. Many form when a giant star runs out of fuel and its core collapses. The edge, past which nothing can get out, is called the event horizon." },
            new() { Topic = "Why the sky is dark at night",
                Keywords = new[] { "why is space black", "why is the sky dark", "why is space dark", "why is the night sky dark" },
                Fact = "Space looks black because it is nearly empty, with no air to scatter light. During the day, Earth's sky is blue because sunlight scatters in our atmosphere; at night, with the Sun gone, we see the darkness of space between the stars." },
            new() { Topic = "How big is the universe",
                Keywords = new[] { "how big is the universe", "size of the universe", "how large is the universe", "biggest thing in the universe", "edge of the universe" },
                Fact = "The part of the universe we can see, called the observable universe, is about 93 billion light-years across, and it holds trillions of galaxies. The whole universe may be even bigger, or endless — we don't know." },
            new() { Topic = "How old is Earth",
                Keywords = new[] { "how old is earth", "age of earth", "when did earth form", "how did earth form" },
                Fact = "Earth is about 4.5 billion years old. It formed when gravity pulled together dust and gas left over from the young Sun, slowly building up into the rocky planet we live on." },
            new() { Topic = "How old is the universe",
                Keywords = new[] { "how old is the universe", "age of the universe", "big bang", "start of the universe", "beginning of the universe", "how did the universe start" },
                Fact = "The universe is about 13.8 billion years old. It began in an event called the Big Bang, when everything started from an incredibly hot, dense point and has been expanding and cooling ever since." },
            new() { Topic = "Can we live on Mars",
                Keywords = new[] { "live on mars", "life on mars", "humans on mars", "colonize mars", "move to mars", "can people live on mars" },
                Fact = "Not yet. Mars is very cold, has almost no breathable air, and little protection from radiation. Astronauts would need sealed habitats, spacesuits, and their own oxygen and water. Scientists are studying how a future crew might visit or stay." },
            new() { Topic = "Do aliens exist",
                Keywords = new[] { "do aliens exist", "are aliens real", "is there life in space", "alien life", "extraterrestrial", "life on other planets" },
                Fact = "Nobody has found alien life yet. But the universe has billions of galaxies and countless planets, so many scientists think simple life could exist somewhere. Places like Mars and Jupiter's moon Europa are being searched for tiny living things." },
            new() { Topic = "Why is the Sun hot",
                Keywords = new[] { "why is the sun hot", "how hot is the sun", "what makes the sun hot", "how does the sun burn", "what is the sun made of" },
                Fact = "The Sun is a giant ball of hydrogen and helium gas. In its core, nuclear fusion squeezes hydrogen atoms together into helium, releasing enormous energy. The core reaches about 15 million degrees Celsius, and the surface is around 5,500 degrees Celsius." },
            new() { Topic = "Why do we have seasons",
                Keywords = new[] { "why do we have seasons", "what causes seasons", "why are there seasons" },
                Fact = "Seasons happen because Earth is tilted on its axis. As Earth orbits the Sun, different parts lean toward it. When your part of the world tilts toward the Sun you get summer; when it tilts away you get winter." },
            new() { Topic = "Why does the Moon change shape",
                Keywords = new[] { "moon change shape", "moon phases", "why does the moon change", "phases of the moon", "why does the moon look different" },
                Fact = "The Moon doesn't really change shape — we just see different amounts of its sunlit side as it orbits Earth. These are called phases, from a thin crescent to a full moon and back, over about 29 days." },
            new() { Topic = "What is a shooting star",
                Keywords = new[] { "shooting star", "falling star", "what is a meteor", "meteor", "meteorite", "what is a shooting star" },
                Fact = "A shooting star isn't a star at all. It's a small piece of space rock or dust that burns up as it zooms into Earth's atmosphere, making a bright streak of light. If a piece survives and lands, it's called a meteorite." },
            new() { Topic = "Feathered dinosaurs",
                Keywords = new[] { "feathers", "feathered dinosaurs", "did dinosaurs have feathers", "were dinosaurs feathered" },
                Fact = "Many dinosaurs, especially small meat-eaters like Velociraptor and Deinonychus, were covered in feathers. Feathers first helped with warmth and display, and only later helped some dinosaurs' descendants — birds — to fly." },
            new() { Topic = "How do we know about dinosaurs",
                Keywords = new[] { "how do we know", "fossils", "how are fossils made", "what is a fossil", "how do scientists know", "paleontologist" },
                Fact = "We learn about dinosaurs from fossils — bones, teeth, eggs, and footprints preserved in rock over millions of years. Scientists called paleontologists dig them up and study them to figure out how dinosaurs looked, moved, and lived." },
            new() { Topic = "Biggest and smallest dinosaurs",
                Keywords = new[] { "biggest dinosaur", "largest dinosaur", "smallest dinosaur", "heaviest dinosaur", "tallest dinosaur" },
                Fact = "Argentinosaurus was one of the largest and heaviest dinosaurs, maybe longer than a basketball court. Some of the smallest dinosaurs were only about the size of a chicken, like Compsognathus." },
            new() { Topic = "Solar system planets",
                Keywords = new[] { "how many planets", "planets in the solar system", "name the planets", "order of the planets", "list of planets" },
                Fact = "Our solar system has eight planets. In order from the Sun they are Mercury, Venus, Earth, Mars, Jupiter, Saturn, Uranus, and Neptune. Pluto is now called a dwarf planet." },
            new() { Topic = "Why Mars is red",
                Keywords = new[] { "why is mars red", "mars red", "why is mars the red planet", "what makes mars red" },
                Fact = "Mars looks red because its soil is full of iron that has rusted, just like old metal turns reddish. A thin layer of this rusty dust covers the whole planet, giving it the nickname the Red Planet." },
        };
    }
}
