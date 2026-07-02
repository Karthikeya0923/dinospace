using System.Collections.Generic;

namespace dinospace
{
    public static class SuggestedQuestions
    {
        // 3 are shown at random each time. Every question here is grounded:
        // it names an encyclopedia entry (or a superlative NovaSaur can look
        // up), so the model always answers with real facts from the app.
        public static List<string> All = new List<string>
        {
            // Dinosaurs
            "How big was the T. Rex?",
            "Could a Spinosaurus beat a T. Rex?",
            "What was the biggest dinosaur ever?",
            "What is the fastest dinosaur?",
            "Did dinosaurs have feathers?",
            "Which dinosaur had the strongest bite?",
            "How fast was a Velociraptor?",
            "Did Velociraptors hunt in packs?",
            "What did Stegosaurus eat?",
            "How many horns did Triceratops have?",
            "Could a T. Rex beat a Triceratops?",
            "How long was a Brachiosaurus neck?",
            "What did T. Rex use its tiny arms for?",
            "Was Pteranodon a dinosaur?",
            "How big were Therizinosaurus claws?",
            "What did Parasaurolophus use its crest for?",
            "Why did Carnotaurus have horns?",
            "Was Giganotosaurus bigger than T. Rex?",
            "How heavy was Argentinosaurus?",
            "Why did the dinosaurs go extinct?",

            // Prehistoric sea and swamp giants
            "Could a Megalodon beat a Mosasaurus?",
            "How big was the Megalodon?",
            "How long was Titanoboa?",
            "How big was Deinosuchus?",
            "What did Liopleurodon hunt?",

            // Space
            "What is a black hole?",
            "How big is the biggest black hole?",
            "Why is Mars red?",
            "How hot is the Sun?",
            "How far away is the Moon?",
            "Why does the Moon change shape?",
            "How do stars form?",
            "What is the Andromeda Galaxy?",
            "How big is the Sun compared to Earth?",
            "What is the Orion constellation?",
            "Why is Earth called the blue planet?",
            "Could people live on Mars?",
            "What is inside the Sun?",
            "How old is Earth?",
            "Do aliens exist?",
        };
    }
}
