namespace dinospace
{
    public class QuizQuestion
    {
        public string Question { get; set; }

        // ----- For MULTIPLE CHOICE questions -----
        // Fill in all four options, then set Correct to the letter of the right one.
        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }
        public string Correct { get; set; }   // "A", "B", "C", or "D"

        // ----- For TRUE / FALSE questions -----
        // Set IsTrueFalse = true, then set TrueFalseAnswer.
        // (Leave the four options above blank.)
        public bool IsTrueFalse { get; set; }
        public bool TrueFalseAnswer { get; set; }   // true = the statement is TRUE
    }
}