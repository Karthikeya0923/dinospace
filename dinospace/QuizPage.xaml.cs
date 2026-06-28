namespace dinospace
{
    public partial class QuizPage : ContentPage
    {
        private readonly string _mode;
        private readonly int _count;
        private readonly Random _rng = new Random();

        private List<QuizQuestion> _questions;
        private int _index;
        private int _score;
        private bool _answered;
        private int _correctIndex;
        private int _visibleCount;

        public QuizPage(string mode, int count)
        {
            InitializeComponent();
            SwipeBack.Attach(this);
            _mode = mode;
            _count = count;
            Title = $"{mode} Quiz";
            StartQuiz();
        }

        private void StartQuiz()
        {
            if (_mode == "Mixed")
            {
                _questions = QuizData.GetMixedQuestions().Take(_count).ToList();
            }
            else
            {
                _questions = LoadQuestions(_mode).OrderBy(_ => Guid.NewGuid()).Take(_count).ToList();
            }

            _index = 0;
            _score = 0;

            if (_questions.Count == 0)
            {
                QuizPanel.IsVisible = false;
                ResultsPanel.IsVisible = true;
                ScoreLabel.Text = "No questions yet. Add some in QuizData.cs and try again!";
                BestLabel.Text = "";
                return;
            }

            QuizPanel.IsVisible = true;
            ResultsPanel.IsVisible = false;
            ShowQuestion();
        }

        private List<QuizQuestion> LoadQuestions(string mode)
        {
            if (mode == "Dinosaurs") return QuizData.GetDinoQuestions();
            if (mode == "Space") return QuizData.GetSpaceQuestions();
            return QuizData.GetMixedQuestions();
        }

        private void ShowQuestion()
        {
            _answered = false;
            var q = _questions[_index];

            QuizProgress.Progress = (double)(_index + 1) / _questions.Count;
            ProgressLabel.Text = $"Question {_index + 1} of {_questions.Count}     Score: {_score}";
            QuestionLabel.Text = q.Question;

            var frames = new[] { Opt0, Opt1, Opt2, Opt3 };
            var labels = new[] { Opt0Label, Opt1Label, Opt2Label, Opt3Label };

            List<string> options;
            if (q.IsTrueFalse)
            {
                options = new List<string> { "True", "False" };
                _correctIndex = q.TrueFalseAnswer ? 0 : 1;
                _visibleCount = 2;
            }
            else
            {
                var choices = new List<(string Text, bool IsCorrect)>
                {
                    (q.OptionA, q.Correct == "A"),
                    (q.OptionB, q.Correct == "B"),
                    (q.OptionC, q.Correct == "C"),
                    (q.OptionD, q.Correct == "D")
                };

                choices = choices.OrderBy(x => _rng.Next()).ToList();
                options = choices.Select(x => x.Text).ToList();
                _correctIndex = choices.FindIndex(x => x.IsCorrect);
                _visibleCount = 4;
            }

            for (int i = 0; i < 4; i++)
            {
                if (i < _visibleCount)
                {
                    frames[i].IsVisible = true;
                    labels[i].Text = options[i];
                    labels[i].TextColor = Theme.TextPrimary;
                    frames[i].BackgroundColor = Theme.ChipBg;
                }
                else
                {
                    frames[i].IsVisible = false;
                }
            }

            NextButton.IsVisible = false;
        }

        private void OnOpt0(object s, EventArgs e) => Answer(0);
        private void OnOpt1(object s, EventArgs e) => Answer(1);
        private void OnOpt2(object s, EventArgs e) => Answer(2);
        private void OnOpt3(object s, EventArgs e) => Answer(3);

        private void Answer(int idx)
        {
            if (_answered) return;
            if (idx >= _visibleCount) return;

            _answered = true;

            var frames = new[] { Opt0, Opt1, Opt2, Opt3 };
            var labels = new[] { Opt0Label, Opt1Label, Opt2Label, Opt3Label };

            frames[_correctIndex].BackgroundColor = Theme.QuizCorrect;
            labels[_correctIndex].TextColor = Colors.White;

            if (idx == _correctIndex)
            {
                _score++;
            }
            else
            {
                frames[idx].BackgroundColor = Theme.Danger;
                labels[idx].TextColor = Colors.White;
            }

            NextButton.IsVisible = true;
        }

        private void OnNext(object sender, EventArgs e)
        {
            _index++;
            if (_index < _questions.Count) ShowQuestion();
            else ShowResults();
        }

        private void ShowResults()
        {
            QuizPanel.IsVisible = false;
            ProgressLabel.IsVisible = false;
            ResultsPanel.IsVisible = true;

            int total = _questions.Count;
            ScoreLabel.Text = $"You scored {_score} out of {total}!";

            string correctKey = $"quiz_correct_{_mode}";
            string questionsKey = $"quiz_questions_{_mode}";
            int comeCorrect = Preferences.Get(correctKey, 0) + _score;
            int comeAnswered = Preferences.Get(questionsKey, 0) + total;
            Preferences.Set(correctKey, comeCorrect);
            Preferences.Set(questionsKey, comeAnswered);

            int pct = comeAnswered > 0 ? (int)Math.Round(100.0 * comeCorrect / comeAnswered) : 0;
            BestLabel.Text = $"Overall {_mode} accuracy: {pct}%";
        }

        private void OnPlayAgain(object sender, EventArgs e) => StartQuiz();

        private async void OnBack(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PopAsync();
        }
    }
}