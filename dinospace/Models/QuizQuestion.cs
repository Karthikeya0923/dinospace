namespace dinospace.Models
{
    public enum QuizDifficulty { Easy, Medium, Hard }

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
