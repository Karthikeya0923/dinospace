using System;
using System.Collections.Generic;

namespace dinospace.Data
{
    // "Did you know?" facts shown on Home and Play. Kept punchy and true.
    public static class FactData
    {
        // Random.Shared: safe from any thread, unlike a shared Random instance.
        public static string Random() => Facts[System.Random.Shared.Next(Facts.Count)];

        public static readonly List<string> Facts = new()
        {
            // Space
            "A neutron star is so dense that a sugar-cube-sized piece would weigh about a billion tons.",
            "There are more stars in the universe than grains of sand on every beach on Earth.",
            "The Sun makes up about 99.8% of all the mass in our solar system.",
            "A day on Venus is longer than a year on Venus.",
            "You always see the Sun as it looked about 8 minutes ago.",
            "Space is completely silent — there is no air to carry sound.",
            "Footprints on the Moon could last millions of years, since there is no wind to erase them.",
            "Saturn is so light for its size that it would float in a big enough bathtub.",
            "Over one million Earths could fit inside the Sun.",
            "A black hole's gravity is so strong that not even light can escape it.",
            "There are more trees on Earth than stars in the Milky Way.",
            "Jupiter is so massive that every other planet could fit inside it.",
            "Driving to the Sun at highway speed would take over 150 years.",
            "Neptune has the fastest winds in the solar system, over 2,000 km/h.",
            "A year on Mercury lasts just 88 Earth days.",
            "Uranus spins on its side, as if it were rolling around the Sun.",
            "Astronauts on the ISS see about 16 sunrises and sunsets every day.",
            "The Milky Way is so wide that light takes 100,000 years to cross it.",
            "Venus is the hottest planet, even though Mercury is closer to the Sun.",
            "Voyager 1's radio signals take over 22 hours to reach Earth.",
            "One teaspoon of the Sun's core would be hot enough to kill someone from 150 km away.",
            "The largest known volcano, Olympus Mons on Mars, is nearly three times as tall as Mount Everest.",
            "If two pieces of the same metal touch in space, they can weld together instantly.",
            "The footsteps of the Apollo astronauts are still on the Moon today.",
            "Halley's Comet won't be visible again until the year 2061.",
            "Betelgeuse is so big that if it replaced the Sun, it would swallow Mars.",
            "There may be a planet-sized diamond out in space — the crushed core of a dead star.",
            "The International Space Station travels at about 28,000 km/h.",
            "It rains diamonds on Neptune and Uranus, scientists think.",
            "The observable universe is about 93 billion light-years across.",
            "Sagittarius A*, our galaxy's central black hole, has the mass of 4 million Suns.",
            "Light from the Andromeda Galaxy left before humans existed.",
            "A full NASA spacesuit costs about as much as a small house.",
            "Saturn's rings are hundreds of thousands of km wide but only about 10 metres thick in places.",
            "The Moon is slowly drifting away from Earth, about 4cm each year.",
            "Jupiter's Great Red Spot is a storm bigger than the whole Earth.",
            "In space, astronauts can grow up to 5cm taller as their spines stretch.",
            "Pluto takes 248 Earth years to orbit the Sun just once.",
            "There is a giant cloud of alcohol floating in space near the center of our galaxy.",
            "The coldest known place in the universe is a nebula that is colder than deep space itself.",

            // Dinosaurs & prehistoric life
            "Birds are living dinosaurs, evolved directly from small feathered dinosaurs.",
            "Dinosaurs ruled the Earth for over 160 million years; humans for only about 300,000.",
            "T. Rex lived closer in time to humans than to Stegosaurus.",
            "Many dinosaurs were covered in feathers, not scales.",
            "The smallest known dinosaurs were about the size of a chicken.",
            "We've likely discovered only a tiny fraction of all dinosaur species that ever lived.",
            "The word dinosaur means 'terrible lizard', though they weren't actually lizards.",
            "Some dinosaurs swallowed stones to help grind up food in their stomachs.",
            "The largest dinosaurs could weigh as much as a dozen elephants combined.",
            "Spinosaurus's only near-complete skeleton was destroyed in a WWII bombing raid.",
            "Therizinosaurus had claws longer than an adult human's arm.",
            "Argentinosaurus may have been longer than a basketball court.",
            "A Velociraptor fossil was found locked in combat with a Protoceratops.",
            "Megalodon teeth could grow over 7 inches long.",
            "T. Rex had a bite force of around 12,800 pounds — enough to crush a car.",
            "Ankylosaurus could swing its bony tail club hard enough to break bone.",
            "Quetzalcoatlus stood as tall as a giraffe yet could still fly.",
            "Stegosaurus had a brain about the size of a walnut.",
            "Titanoboa was twice as long and eight times as heavy as a modern anaconda.",
            "Some titanosaurs laid eggs in huge shared nesting colonies.",
            "Pteranodon had no teeth at all, despite being a meat-eater.",
            "Deinosuchus, a giant crocodile, had a bite even stronger than T. Rex's.",
            "Compsognathus, once thought the smallest dinosaur, was about the size of a chicken.",
            "Dunkleosteus had no teeth — just self-sharpening bony blades.",
            "The woolly mammoth survived on a remote island until about 4,000 years ago.",
            "Smilodon's sabre teeth could grow up to 28cm long.",
            "Gallimimus could run about as fast as a racehorse.",
            "Scientists once put Iguanodon's thumb spike on its nose by mistake.",
            "Carnotaurus had the tiniest arms of any large predatory dinosaur.",
            "Brachiosaurus had to eat around 750kg of plants every single day.",
            "Utahraptor, the largest raptor, had a foot claw about 23cm long.",
            "Triceratops had one of the largest skulls of any land animal ever, up to 8 feet long.",
            "Parasaurolophus may have used its head crest like a trumpet.",
            "Some dinosaurs, like Baryonyx, mainly ate fish.",
            "Kronosaurus had a skull even bigger than a T. Rex's.",
            "The first dinosaur fossils were named 'dinosaurs' in 1842.",
            "Feathers likely first evolved for warmth and display, not for flying.",
            "Mosasaurus fossils have been found on every continent, even Antarctica.",
            "Allosaurus constantly grew new teeth to replace worn ones.",
            "An asteroid impact 66 million years ago helped end the age of dinosaurs.",
        };
    }
}
