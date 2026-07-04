using System;
using System.Collections.Generic;
using System.Linq;

namespace dinospace.Data
{
    // Quiz banks. Every question carries a difficulty and a one-line
    // explanation so answering teaches something, not just scores a point.
    public static class QuizData
    {
        public static List<QuizQuestion> Dino => _dino ??= BuildDino();
        public static List<QuizQuestion> Space => _space ??= BuildSpace();

        private static List<QuizQuestion>? _dino;
        private static List<QuizQuestion>? _space;

        // A shuffled, interleaved dino+space mix.
        public static List<QuizQuestion> Mixed()
        {
            var rng = new Random();
            var d = Dino.OrderBy(_ => rng.Next()).ToList();
            var s = Space.OrderBy(_ => rng.Next()).ToList();
            bool dinoFirst = rng.Next(2) == 0;
            var first = dinoFirst ? d : s;
            var second = dinoFirst ? s : d;

            var mixed = new List<QuizQuestion>();
            int max = Math.Max(first.Count, second.Count);
            for (int i = 0; i < max; i++)
            {
                if (i < first.Count) mixed.Add(first[i]);
                if (i < second.Count) mixed.Add(second[i]);
            }
            return mixed;
        }

        public static List<QuizQuestion> For(string mode) => mode switch
        {
            "Space" => Space,
            "Mixed" => Mixed(),
            _ => Dino,
        };

        private static List<QuizQuestion> BuildDino() => new()
        {
            new() { Difficulty = QuizDifficulty.Easy, Question = "What does the name Tyrannosaurus Rex mean?",
                OptionA = "Tyrant Lizard King", OptionB = "Giant Fast Runner", OptionC = "Three-Horned Face", OptionD = "Spine Lizard", Correct = "A",
                Explanation = "Tyrannosaurus Rex means 'Tyrant Lizard King' — a fitting name for the apex predator of its time." },
            new() { Difficulty = QuizDifficulty.Easy, Question = "A dinosaur that ate only plants is called a...?",
                OptionA = "Carnivore", OptionB = "Herbivore", OptionC = "Omnivore", OptionD = "Predator", Correct = "B",
                Explanation = "Plant-eaters are herbivores. Meat-eaters are carnivores, and animals that eat both are omnivores." },
            new() { Difficulty = QuizDifficulty.Easy, Question = "Velociraptor was about the size of a turkey.",
                IsTrueFalse = true, TrueFalseAnswer = true,
                Explanation = "True! Real Velociraptors were only turkey-sized. The movie 'raptors' were based on the bigger Deinonychus." },
            new() { Difficulty = QuizDifficulty.Easy, Question = "How many horns did Triceratops have on its head?",
                OptionA = "One", OptionB = "Two", OptionC = "Three", OptionD = "Five", Correct = "C",
                Explanation = "Triceratops means 'three-horned face' — two long brow horns and one shorter nose horn." },
            new() { Difficulty = QuizDifficulty.Easy, Question = "Which dinosaur had two rows of bony plates along its back?",
                OptionA = "Triceratops", OptionB = "Stegosaurus", OptionC = "Brachiosaurus", OptionD = "Carnotaurus", Correct = "B",
                Explanation = "Stegosaurus had two rows of tall plates on its back and four spikes on its tail called a thagomizer." },
            new() { Difficulty = QuizDifficulty.Easy, Question = "Triceratops was a meat-eater.",
                IsTrueFalse = true, TrueFalseAnswer = false,
                Explanation = "False — Triceratops was a herbivore. It used its beak to snip tough plants." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "Which animal had the strongest bite force ever measured?",
                OptionA = "Tyrannosaurus Rex", OptionB = "Megalodon", OptionC = "Giganotosaurus", OptionD = "Spinosaurus", Correct = "B",
                Explanation = "Megalodon's bite is estimated at over 40,000 pounds — the strongest of any known creature." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "On which continent have the most dinosaur fossils been found?",
                OptionA = "Africa", OptionB = "Asia", OptionC = "Antarctica", OptionD = "North America", Correct = "D",
                Explanation = "North America has yielded the most dinosaur fossils, including many famous T. Rex skeletons." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "Dinosaurs and humans lived on Earth at the same time.",
                IsTrueFalse = true, TrueFalseAnswer = false,
                Explanation = "False. Non-bird dinosaurs died out 66 million years ago; humans appeared only about 300,000 years ago." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "Spinosaurus spent much of its time in and around water.",
                IsTrueFalse = true, TrueFalseAnswer = true,
                Explanation = "True. Spinosaurus was semi-aquatic and used its long snout to catch fish." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "Which dinosaur had the longest claws of any land animal?",
                OptionA = "Therizinosaurus", OptionB = "Utahraptor", OptionC = "Allosaurus", OptionD = "Deinonychus", Correct = "A",
                Explanation = "Therizinosaurus had scythe-like claws over 3 feet long — used mostly for reaching plants and defence." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "Which flying reptile was as tall as a giraffe on the ground?",
                OptionA = "Pteranodon", OptionB = "Quetzalcoatlus", OptionC = "Dimorphodon", OptionD = "Archaeopteryx", Correct = "B",
                Explanation = "Quetzalcoatlus stood as tall as a giraffe and had a wingspan the size of a small plane." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "Ankylosaurus defended itself with a...?",
                OptionA = "Neck frill", OptionB = "Venom bite", OptionC = "Bony tail club", OptionD = "Loud roar", Correct = "C",
                Explanation = "Ankylosaurus swung a heavy club of fused bone at the end of its tail, hard enough to break bone." },
            new() { Difficulty = QuizDifficulty.Hard, Question = "Which dinosaur is closest in time to humans?",
                OptionA = "Stegosaurus", OptionB = "Brachiosaurus", OptionC = "Tyrannosaurus Rex", OptionD = "Allosaurus", Correct = "C",
                Explanation = "T. Rex lived closer in time to us than to Stegosaurus — Stegosaurus died out ~80 million years before T. Rex appeared." },
            new() { Difficulty = QuizDifficulty.Hard, Question = "The 'raptors' in famous dinosaur movies were mostly based on which real dinosaur?",
                OptionA = "Velociraptor", OptionB = "Deinonychus", OptionC = "Compsognathus", OptionD = "Gallimimus", Correct = "B",
                Explanation = "The movie 'raptors' match Deinonychus in size — much larger than the turkey-sized real Velociraptor." },
            new() { Difficulty = QuizDifficulty.Hard, Question = "Megalodon lived at the same time as the dinosaurs.",
                IsTrueFalse = true, TrueFalseAnswer = false,
                Explanation = "False. Megalodon appeared millions of years after the dinosaurs went extinct." },
            new() { Difficulty = QuizDifficulty.Hard, Question = "Which is the largest raptor ever discovered?",
                OptionA = "Velociraptor", OptionB = "Deinonychus", OptionC = "Utahraptor", OptionD = "Microraptor", Correct = "C",
                Explanation = "Utahraptor was the biggest raptor — around 18 feet long with a 23cm foot claw." },
            new() { Difficulty = QuizDifficulty.Hard, Question = "Birds are living dinosaurs.",
                IsTrueFalse = true, TrueFalseAnswer = true,
                Explanation = "True! Birds evolved directly from small feathered theropod dinosaurs, so they are technically dinosaurs." },
            new() { Difficulty = QuizDifficulty.Easy, Question = "Which dinosaur had a long neck for reaching tall trees?",
                OptionA = "Velociraptor", OptionB = "Brachiosaurus", OptionC = "Stegosaurus", OptionD = "Carnotaurus", Correct = "B",
                Explanation = "Brachiosaurus used its towering neck to browse leaves over 40 feet above the ground." },
            new() { Difficulty = QuizDifficulty.Easy, Question = "Pteranodon was a dinosaur.",
                IsTrueFalse = true, TrueFalseAnswer = false,
                Explanation = "False. Pteranodon was a flying reptile called a pterosaur — a cousin of the dinosaurs, not a dinosaur." },
            new() { Difficulty = QuizDifficulty.Easy, Question = "What does 'carnivore' mean?",
                OptionA = "Plant-eater", OptionB = "Meat-eater", OptionC = "Fast runner", OptionD = "Egg-layer", Correct = "B",
                Explanation = "A carnivore is a meat-eater, like T. Rex or Velociraptor." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "Which giant sea reptile could swallow prey whole?",
                OptionA = "Mosasaurus", OptionB = "Stegosaurus", OptionC = "Gallimimus", OptionD = "Iguanodon", Correct = "A",
                Explanation = "Mosasaurus had double-hinged jaws and extra teeth on the roof of its mouth to gulp prey whole." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "Smilodon is better known as the...?",
                OptionA = "Woolly rhino", OptionB = "Cave bear", OptionC = "Sabre-toothed cat", OptionD = "Dire wolf", Correct = "C",
                Explanation = "Smilodon is the famous sabre-toothed cat, with canines up to 28cm long." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "The woolly mammoth was a close relative of which animal?",
                OptionA = "The rhino", OptionB = "The elephant", OptionC = "The hippo", OptionD = "The bison", Correct = "B",
                Explanation = "Woolly mammoths were close cousins of today's elephants, wrapped in shaggy fur for the Ice Age." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "Which dinosaur mainly ate fish?",
                OptionA = "Triceratops", OptionB = "Baryonyx", OptionC = "Ankylosaurus", OptionD = "Gallimimus", Correct = "B",
                Explanation = "Baryonyx had a crocodile-like snout and hooked thumb claws for catching fish." },
            new() { Difficulty = QuizDifficulty.Hard, Question = "Dunkleosteus had rows of sharp teeth.",
                IsTrueFalse = true, TrueFalseAnswer = false,
                Explanation = "False. Instead of teeth it had self-sharpening bony blades that sheared together." },
            new() { Difficulty = QuizDifficulty.Hard, Question = "Which dinosaur had a dome of solid bone on its head?",
                OptionA = "Pachycephalosaurus", OptionB = "Parasaurolophus", OptionC = "Allosaurus", OptionD = "Titanoboa", Correct = "A",
                Explanation = "Pachycephalosaurus had a skull dome up to 25cm thick, possibly for head-butting contests." },
            new() { Difficulty = QuizDifficulty.Hard, Question = "About how long ago did the dinosaurs go extinct?",
                OptionA = "6 thousand years", OptionB = "66 million years", OptionC = "6 billion years", OptionD = "600 years", Correct = "B",
                Explanation = "An asteroid impact about 66 million years ago ended the age of the non-bird dinosaurs." },
        };

        private static List<QuizQuestion> BuildSpace() => new()
        {
            new() { Difficulty = QuizDifficulty.Easy, Question = "How many moons does Earth have?",
                OptionA = "1", OptionB = "2", OptionC = "3", OptionD = "0", Correct = "A",
                Explanation = "Earth has exactly one Moon, which creates our ocean tides." },
            new() { Difficulty = QuizDifficulty.Easy, Question = "The Sun is a star.",
                IsTrueFalse = true, TrueFalseAnswer = true,
                Explanation = "True. The Sun is an ordinary star that just happens to be very close to us." },
            new() { Difficulty = QuizDifficulty.Easy, Question = "Which planet is known as the Red Planet?",
                OptionA = "Earth", OptionB = "Mars", OptionC = "Venus", OptionD = "Jupiter", Correct = "B",
                Explanation = "Mars looks red because of rusted, iron-rich dust covering its surface." },
            new() { Difficulty = QuizDifficulty.Easy, Question = "What is at the center of our solar system?",
                OptionA = "Earth", OptionB = "The Moon", OptionC = "The Sun", OptionD = "Mars", Correct = "C",
                Explanation = "The Sun sits at the center, and its gravity holds all the planets in orbit." },
            new() { Difficulty = QuizDifficulty.Easy, Question = "Which is the largest planet in our solar system?",
                OptionA = "Earth", OptionB = "Saturn", OptionC = "Jupiter", OptionD = "Neptune", Correct = "C",
                Explanation = "Jupiter is the largest — every other planet could fit inside it." },
            new() { Difficulty = QuizDifficulty.Easy, Question = "A light-year measures distance, not time.",
                IsTrueFalse = true, TrueFalseAnswer = true,
                Explanation = "True. A light-year is how far light travels in a year — about 9.5 trillion km." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "Which planet is famous for its bright rings?",
                OptionA = "Jupiter", OptionB = "Saturn", OptionC = "Mars", OptionD = "Neptune", Correct = "B",
                Explanation = "Saturn's rings are made of billions of chunks of ice and rock." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "Which is the hottest planet in the solar system?",
                OptionA = "Mercury", OptionB = "Venus", OptionC = "Mars", OptionD = "Jupiter", Correct = "B",
                Explanation = "Venus is hottest because its thick atmosphere traps heat — even though Mercury is closer to the Sun." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "What do we call a group of stars that forms a pattern?",
                OptionA = "Galaxy", OptionB = "Constellation", OptionC = "Comet", OptionD = "Nebula", Correct = "B",
                Explanation = "A constellation is a pattern of stars, like Orion the Hunter." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "What is the name of the galaxy we live in?",
                OptionA = "Andromeda", OptionB = "The Milky Way", OptionC = "The Sombrero", OptionD = "Orion", Correct = "B",
                Explanation = "We live in the Milky Way, a barred spiral galaxy of hundreds of billions of stars." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "A day on Venus is longer than its year.",
                IsTrueFalse = true, TrueFalseAnswer = true,
                Explanation = "True. Venus spins so slowly that one day there lasts longer than one full orbit of the Sun." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "Which planet spins on its side?",
                OptionA = "Neptune", OptionB = "Saturn", OptionC = "Uranus", OptionD = "Mars", Correct = "C",
                Explanation = "Uranus is tipped over about 98 degrees, so it rolls around the Sun on its side." },
            new() { Difficulty = QuizDifficulty.Hard, Question = "What is a black hole?",
                OptionA = "A cold empty planet", OptionB = "A region where gravity traps even light", OptionC = "A dying comet", OptionD = "A giant star", Correct = "B",
                Explanation = "A black hole's gravity is so strong that not even light can escape once it crosses the event horizon." },
            new() { Difficulty = QuizDifficulty.Hard, Question = "Roughly how long does sunlight take to reach Earth?",
                OptionA = "8 seconds", OptionB = "8 minutes", OptionC = "8 hours", OptionD = "Instantly", Correct = "B",
                Explanation = "Light takes about 8 minutes and 20 seconds to travel from the Sun to Earth." },
            new() { Difficulty = QuizDifficulty.Hard, Question = "Which spacecraft is the most distant human-made object?",
                OptionA = "Voyager 1", OptionB = "The ISS", OptionC = "Hubble", OptionD = "Apollo 11", Correct = "A",
                Explanation = "Voyager 1, launched in 1977, has travelled beyond the solar system into interstellar space." },
            new() { Difficulty = QuizDifficulty.Hard, Question = "The Andromeda Galaxy is moving toward the Milky Way.",
                IsTrueFalse = true, TrueFalseAnswer = true,
                Explanation = "True. The two galaxies will begin merging in about 4.5 billion years." },
            new() { Difficulty = QuizDifficulty.Hard, Question = "How often does Halley's Comet return?",
                OptionA = "Every year", OptionB = "Every 10 years", OptionC = "About every 76 years", OptionD = "Once ever", Correct = "C",
                Explanation = "Halley's Comet loops back roughly every 76 years — last in 1986, next in 2061." },
            new() { Difficulty = QuizDifficulty.Easy, Question = "Which planet do we live on?",
                OptionA = "Mars", OptionB = "Earth", OptionC = "Venus", OptionD = "Jupiter", Correct = "B",
                Explanation = "We live on Earth, the only planet known to support life." },
            new() { Difficulty = QuizDifficulty.Easy, Question = "The Moon makes its own light.",
                IsTrueFalse = true, TrueFalseAnswer = false,
                Explanation = "False. The Moon has no light of its own — it reflects sunlight." },
            new() { Difficulty = QuizDifficulty.Easy, Question = "How many planets are in our solar system?",
                OptionA = "7", OptionB = "8", OptionC = "9", OptionD = "10", Correct = "B",
                Explanation = "There are eight planets. Pluto is now classed as a dwarf planet." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "Which planet is closest to the Sun?",
                OptionA = "Venus", OptionB = "Earth", OptionC = "Mercury", OptionD = "Mars", Correct = "C",
                Explanation = "Mercury is the closest planet to the Sun and has the shortest year." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "Which moon may hide an ocean under its ice?",
                OptionA = "Europa", OptionB = "Our Moon", OptionC = "Phobos", OptionD = "Titan", Correct = "A",
                Explanation = "Jupiter's moon Europa hides a deep saltwater ocean beneath its icy crust." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "A group of billions of stars is called a...?",
                OptionA = "Nebula", OptionB = "Galaxy", OptionC = "Comet", OptionD = "Crater", Correct = "B",
                Explanation = "A galaxy is a huge collection of stars, gas, and dust — like our Milky Way." },
            new() { Difficulty = QuizDifficulty.Medium, Question = "Saturn is light enough to float in water.",
                IsTrueFalse = true, TrueFalseAnswer = true,
                Explanation = "True. Saturn is so light for its size that it would float in a big enough bathtub." },
            new() { Difficulty = QuizDifficulty.Hard, Question = "What is the name of our galaxy's central black hole?",
                OptionA = "Phoenix A*", OptionB = "Sagittarius A*", OptionC = "Betelgeuse", OptionD = "Andromeda", Correct = "B",
                Explanation = "Sagittarius A*, with the mass of 4 million Suns, sits at the centre of the Milky Way." },
            new() { Difficulty = QuizDifficulty.Hard, Question = "Where do new stars form?",
                OptionA = "In black holes", OptionB = "In nebulae", OptionC = "On planets", OptionD = "In comets", Correct = "B",
                Explanation = "Stars are born inside nebulae — giant clouds of gas and dust pulled together by gravity." },
            new() { Difficulty = QuizDifficulty.Hard, Question = "Which star in Orion may explode as a supernova one day?",
                OptionA = "Rigel", OptionB = "Polaris", OptionC = "Betelgeuse", OptionD = "Sirius", Correct = "C",
                Explanation = "Betelgeuse, a red supergiant in Orion, is nearing the end of its life." },
        };
    }
}
