
namespace dinospace
{
    public static class QuizData
    {
        // ============================================================
        //  DINOSAUR QUIZ
        // ============================================================
        public static List<QuizQuestion> GetDinoQuestions() => new List<QuizQuestion>
        {
            new QuizQuestion
            {
                Question = "What does the name \"Tyrannosaurus Rex\" mean?",
                OptionA = "Tyrant Lizard King",
                OptionB = "Giant Fast Runner",
                OptionC = "Three-Horned Face",
                OptionD = "Spine Lizard",
                Correct = "A"
            },
            new QuizQuestion
            {
                Question = "Which animal had the strongest bite force ever?",
                OptionA = "T. Rex",
                OptionB = "Megalodon",
                OptionC = "Giganotosaurus",
                OptionD = "Spinosaurus",
                Correct = "B"
            },
            new QuizQuestion
            {
                Question = "On which continent have the most dinosaur fossils been found?",
                OptionA = "Africa",
                OptionB = "Asia",
                OptionC = "Antarctica",
                OptionD = "North America",
                Correct = "D"
            },
            new QuizQuestion
            {
                Question = "Velociraptor was about the size of a turkey.",
                IsTrueFalse = true,
                TrueFalseAnswer = true
            },
            new QuizQuestion
            {
                Question = "Which dinosaur had two rows of bony plates along its back?",
                OptionA = "Triceratops",
                OptionB = "Stegosaurus",
                OptionC = "Brachiosaurus",
                OptionD = "Carnotaurus",
                Correct = "B"
            },
            new QuizQuestion
            {
                Question = "Triceratops was a meat-eater.",
                IsTrueFalse = true,
                TrueFalseAnswer = false
            },
            new QuizQuestion
            {
                Question = "What does the name \"Giganotosaurus\" mean?",
                OptionA = "Giant Southern Lizard",
                OptionB = "Smooth-Sided Teeth",
                OptionC = "Arm Lizard",
                OptionD = "Roofed Lizard",
                Correct = "A"
            },
            new QuizQuestion
            {
                Question = "Spinosaurus spent much of its time in and around water.",
                IsTrueFalse = true,
                TrueFalseAnswer = true
            },
            new QuizQuestion
            {
                Question = "Which long-necked dinosaur ate leaves from tall trees?",
                OptionA = "Brachiosaurus",
                OptionB = "Velociraptor",
                OptionC = "Stegosaurus",
                OptionD = "Triceratops",
                Correct = "A"
            },
            new QuizQuestion
            {
                Question = "Dinosaurs and humans lived on Earth at the same time.",
                IsTrueFalse = true,
                TrueFalseAnswer = false
            },
            new QuizQuestion
            {
                Question = "A dinosaur that ate only plants is called a what?",
                OptionA = "Carnivore",
                OptionB = "Herbivore",
                OptionC = "Omnivore",
                OptionD = "Predator",
                Correct = "B"
            },
            new QuizQuestion
            {
                Question = "How many horns did Triceratops have on its head?",
                OptionA = "One",
                OptionB = "Two",
                OptionC = "Three",
                OptionD = "Five",
                Correct = "C"
            },
        };

        // ============================================================
        //  MIXED QUIZ (dinosaurs and space together)
        // ============================================================
        public static List<QuizQuestion> GetMixedQuestions() => new List<QuizQuestion>
        {
            new QuizQuestion
            {
                Question = "What does the name \"Tyrannosaurus Rex\" mean?",
                OptionA = "Tyrant Lizard King",
                OptionB = "Giant Fast Runner",
                OptionC = "Three-Horned Face",
                OptionD = "Spine Lizard",
                Correct = "A"
            },
            new QuizQuestion
            {
                Question = "What was the speed of an Allosaurus?",
                OptionA = "40km/h",
                OptionB = "50km/h",
                OptionC = "30km/h",
                OptionD = "20km/h",
                Correct = "B"
            },
            new QuizQuestion
            {
                Question = "How fast does Earth move around the Sun?",
                OptionA = "42,000km/h",
                OptionB = "126,000km/h",
                OptionC = "107,000 km/h",
                OptionD = "89,500km/h",
                Correct = "C"
            },
            new QuizQuestion
            {
                Question = "Megalodon was a type of...?",
                OptionA = "Dinosaur",
                OptionB = "Shark",
                OptionC = "Whale",
                OptionD = "Flying reptile",
                Correct = "B"
            },
            new QuizQuestion
            {
                Question = "Birds are living dinosaurs.",
                IsTrueFalse = true,
                TrueFalseAnswer = true
            },
            new QuizQuestion
            {
                Question = "\"Stegosaurus\" was a herbivore. How did it protect itself from enemies?",
                OptionA = "Sat on them",
                OptionB = "Bit them with sharp teeth",
                OptionC = "Smacked them with its spiky tail",
                OptionD = "Stomped on them",
                Correct = "C"
            },
            new QuizQuestion
            {
                Question = "What is the closest planet to our Sun?",
                OptionA = "Earth",
                OptionB = "Mercury",
                OptionC = "Venus",
                OptionD = "Mars",
                Correct = "B"
            },
            new QuizQuestion
            {
                Question = "Which of these lived in the ocean?",
                OptionA = "Stegosaurus",
                OptionB = "Mosasaurus",
                OptionC = "Pteranodon",
                OptionD = "Brachiosaurus",
                Correct = "B"
            },
            new QuizQuestion
            {
                Question = "T. Rex and Stegosaurus lived at the same time.",
                IsTrueFalse = true,
                TrueFalseAnswer = false
            },
            new QuizQuestion
            {
                Question = "Which of these is a constellation?",
                OptionA = "Orion",
                OptionB = "Mars",
                OptionC = "Megalodon",
                OptionD = "Stegosaurus",
                Correct = "A"
            },
        };

        // ============================================================
        //  SPACE QUIZ
        // ============================================================
        public static List<QuizQuestion> GetSpaceQuestions() => new List<QuizQuestion>
        {
            new QuizQuestion
            {
                Question = "How many moons does Earth have?",
                OptionA = "1",
                OptionB = "2",
                OptionC = "3",
                OptionD = "4",
                Correct = "A"
            },
            new QuizQuestion
            {
                Question = "The Sun is a star.",
                IsTrueFalse = true,
                TrueFalseAnswer = true
            },
            new QuizQuestion
            {
                Question = "Which planet is known as the Red Planet?",
                OptionA = "Earth",
                OptionB = "Mars",
                OptionC = "The Moon",
                OptionD = "The Sun",
                Correct = "B"
            },
            new QuizQuestion
            {
                Question = "A light-year measures distance, not time.",
                IsTrueFalse = true,
                TrueFalseAnswer = true
            },
            new QuizQuestion
            {
                Question = "What is at the center of our solar system?",
                OptionA = "Earth",
                OptionB = "The Moon",
                OptionC = "The Sun",
                OptionD = "Mars",
                Correct = "C"
            },
            new QuizQuestion
            {
                Question = "We could fit every planet side by side between the distance of the Earth and Moon.",
                IsTrueFalse = true,
                TrueFalseAnswer = true
            },
            new QuizQuestion
            {
                Question = "The Moon is larger than the Sun.",
                IsTrueFalse = true,
                TrueFalseAnswer = false
            },
            new QuizQuestion
            {
                Question = "Which planet's strong gravity helps shield Earth by pulling in many comets and asteroids?",
                OptionA = "Saturn",
                OptionB = "Uranus",
                OptionC = "Neptune",
                OptionD = "Jupiter",
                Correct = "D"
            },
            new QuizQuestion
            {
                Question = "Which planet is known as the Red Planet?",
                OptionA = "Venus",
                OptionB = "Mars",
                OptionC = "Jupiter",
                OptionD = "Saturn",
                Correct = "B"
            },
            new QuizQuestion
            {
                Question = "The Sun is actually a star.",
                IsTrueFalse = true,
                TrueFalseAnswer = true
            },
            new QuizQuestion
            {
                Question = "Which is the largest planet in our solar system?",
                OptionA = "Earth",
                OptionB = "Saturn",
                OptionC = "Jupiter",
                OptionD = "Neptune",
                Correct = "C"
            },
            new QuizQuestion
            {
                Question = "What do we call a group of stars that forms a pattern?",
                OptionA = "Galaxy",
                OptionB = "Constellation",
                OptionC = "Comet",
                OptionD = "Nebula",
                Correct = "B"
            },
        };
    }
}