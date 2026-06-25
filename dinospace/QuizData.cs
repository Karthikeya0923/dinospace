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

        // ============================================================
        //  MIXED QUIZ — built automatically from the two pools above.
        //  Each pool is shuffled, then interleaved. The starting type
        //  (dino or space) is random each run, then they alternate.
        // ============================================================
        public static List<QuizQuestion> GetMixedQuestions()
        {
            var rng = new Random();
            var dinos = GetDinoQuestions().OrderBy(_ => rng.Next()).ToList();
            var space = GetSpaceQuestions().OrderBy(_ => rng.Next()).ToList();

            // Coin flip: true = start with a dinosaur, false = start with space
            bool dinoFirst = rng.Next(2) == 0;
            var first = dinoFirst ? dinos : space;
            var second = dinoFirst ? space : dinos;

            var mixed = new List<QuizQuestion>();
            int max = Math.Max(first.Count, second.Count);
            for (int i = 0; i < max; i++)
            {
                if (i < first.Count) mixed.Add(first[i]);
                if (i < second.Count) mixed.Add(second[i]);
            }
            return mixed;
        }
    }
}