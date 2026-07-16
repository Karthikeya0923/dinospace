namespace dinospace.Models
{
    // Difficulty drives both the badge on the question card and the ramp:
    // QuizPage deals easy questions first and saves the hard ones for the end.
    public enum QuizDifficulty { Easy, Medium, Hard }

    // One quiz question in either of the two formats the quiz plays:
    // four-option multiple choice, or a true/false statement.
    public class QuizQuestion
    {
        public string Question { get; set; } = "";

        // Multiple choice: fill all four options and set Correct to "A".."D".
        public string OptionA { get; set; } = "";
        public string OptionB { get; set; } = "";
        public string OptionC { get; set; } = "";
        public string OptionD { get; set; } = "";
        public string Correct { get; set; } = "";

        // True/False: set IsTrueFalse and TrueFalseAnswer, leave options blank.
        public bool IsTrueFalse { get; set; }
        public bool TrueFalseAnswer { get; set; }

        public QuizDifficulty Difficulty { get; set; } = QuizDifficulty.Easy;

        // One friendly sentence shown after answering - the learning moment.
        public string Explanation { get; set; } = "";
    }
}
