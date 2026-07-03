using System;
using System.Collections.Generic;
using System.Linq;

namespace dinospace.Data
{
    // Prompt starters for the Nova chat. Every one is grounded: it names an
    // encyclopedia entry, a curated knowledge topic, or a superlative Nova can
    // look up, so answers stay accurate.
    public static class SuggestedQuestions
    {
        private static readonly Random _rng = new();

        public static List<string> Pick(int n)
            => All.OrderBy(_ => _rng.Next()).Take(n).ToList();

        public static readonly List<string> All = new()
        {
            // Dinosaurs
            "How big was the T. Rex?",
            "Could a Spinosaurus beat a T. Rex?",
            "What was the biggest dinosaur ever?",
            "What is the fastest dinosaur?",
            "Did dinosaurs have feathers?",
            "Why did the dinosaurs go extinct?",
            "How fast was a Velociraptor?",
            "What did Stegosaurus eat?",
            "How many horns did Triceratops have?",
            "Could a T. Rex beat a Triceratops?",
            "How big were Therizinosaurus claws?",
            "Was Giganotosaurus bigger than T. Rex?",
            "How did Ankylosaurus defend itself?",
            "How big was Quetzalcoatlus?",
            "Are birds really dinosaurs?",
            "How big was the Megalodon?",
            "How long was Titanoboa?",

            // Space
            "What is a black hole?",
            "How do stars form?",
            "Why is Mars red?",
            "How hot is the Sun?",
            "Why does the Moon change shape?",
            "How big is the universe?",
            "What is a light-year?",
            "How many planets are there?",
            "Why is Venus the hottest planet?",
            "What is the Milky Way?",
            "Could people live on Mars?",
            "How old is the universe?",
            "What is a shooting star?",
            "Why does Uranus spin on its side?",
            "How fast does the ISS travel?",
            "Do aliens exist?",
        };
    }
}
