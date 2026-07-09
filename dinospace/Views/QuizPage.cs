using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The quiz runner: multiple-choice and true/false questions with instant
    // feedback, an explanation for every answer, and a results screen.
    public class QuizPage : ContentPage
    {
        private readonly string _mode;
        private readonly List<QuizQuestion> _questions;
        private readonly Color _accent;
        private int _index;
        private int _score;
        private bool _answered;

        private ProgressBar _progress = null!;
        private Label _counter = null!, _scoreLabel = null!;
        private VerticalStackLayout _body = null!;

        public QuizPage(string mode, int count)
        {
            _mode = mode;
            _accent = mode == "Space" ? Theme.AccentSpace : mode == "Mixed" ? Theme.AccentNova : Theme.AccentDino;
            _questions = BuildQuestions(mode, count);
            Build();
            SwipeBack.Attach(this);
            ShowQuestion();
        }

        private static List<QuizQuestion> BuildQuestions(string mode, int count)
        {
            var rng = new Random();
            var result = new List<QuizQuestion>();
            // Cycle reshuffled banks until we reach the requested count, so big
            // sizes (50, 100) still work with a finite bank.
            while (result.Count < count)
            {
                var bank = QuizData.For(mode).OrderBy(_ => rng.Next()).ToList();
                foreach (var q in bank)
                {
                    result.Add(q);
                    if (result.Count >= count) break;
                }
                if (bank.Count == 0) break;
            }
            return result;
        }

        private void Build()
        {
            var back = new Label { Text = "‹", FontSize = 32, TextColor = Theme.TextPrimary, VerticalOptions = LayoutOptions.Center, Padding = new Thickness(4, 0, 12, 0) };
            Ui.OnTap(back, async (_, _) => await Close());

            _counter = new Label { FontFamily = Ui.Fonts, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextPrimary, VerticalOptions = LayoutOptions.Center };
            _scoreLabel = new Label { FontFamily = Ui.Fonts, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = _accent, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.End };

            var barGrid = new Grid { Padding = new Thickness(8, 10, 16, 4) };
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            barGrid.Add(back, 0, 0); barGrid.Add(_counter, 1, 0); barGrid.Add(_scoreLabel, 2, 0);

            _progress = new ProgressBar { Progress = 0, HeightRequest = 6, ProgressColor = _accent, Margin = new Thickness(16, 0, 16, 8) };

            _body = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(16, 8, 16, 24) };

            var main = new Grid { RowSpacing = 0 };
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            main.Add(barGrid, 0, 0);
            main.Add(_progress, 0, 1);
            main.Add(new ScrollView { Content = _body }, 0, 2);

            Content = Ui.PageRoot(main);
        }

        private void ShowQuestion()
        {
            _answered = false;
            _answerBtns.Clear();
            var q = _questions[_index];
            _counter.Text = $"Question {_index + 1} of {_questions.Count}";
            _scoreLabel.Text = $"Score {_score}";
            _progress.Progress = (double)_index / _questions.Count;
            _body.Children.Clear();

            // difficulty chip
            _body.Add(Ui.TintChip(q.Difficulty.ToString().ToUpperInvariant(), DifficultyColor(q.Difficulty)));

            // question
            _body.Add(new Border
            {
                Content = new Label { Text = q.Question, FontFamily = Ui.Display, FontSize = Ui.S(21), LineHeight = 1.3, TextColor = Theme.TextPrimary },
                BackgroundColor = Theme.Surface, Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 18 }, Padding = new Thickness(18)
            });

            if (q.IsTrueFalse)
            {
                _body.Add(AnswerButton("True", true == q.TrueFalseAnswer, q));
                _body.Add(AnswerButton("False", false == q.TrueFalseAnswer, q));
            }
            else
            {
                foreach (var (letter, text) in new[] { ("A", q.OptionA), ("B", q.OptionB), ("C", q.OptionC), ("D", q.OptionD) })
                    if (!string.IsNullOrEmpty(text))
                        _body.Add(AnswerButton(text, letter == q.Correct, q, letter));
            }
        }

        // Every answer button this question, with whether it's the right one —
        // so the reveal can paint the truth onto the buttons themselves.
        private readonly List<(Border btn, bool right)> _answerBtns = new();

        private Border AnswerButton(string text, bool isRight, QuizQuestion q, string? letter = null)
        {
            var label = new Label { Text = text, FontFamily = Ui.Fonts, FontSize = Ui.S(15.5), TextColor = Theme.TextPrimary, VerticalOptions = LayoutOptions.Center };
            View content = label;
            if (letter != null)
            {
                var badge = new Border
                {
                    WidthRequest = 30, HeightRequest = 30,
                    BackgroundColor = Theme.SurfaceAlt, Stroke = Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = 9 }, VerticalOptions = LayoutOptions.Center,
                    Content = new Label { Text = letter, FontFamily = Ui.Fonts, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextSecondary, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }
                };
                var g = new Grid { ColumnSpacing = 12 };
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                g.Add(badge, 0, 0); g.Add(label, 1, 0);
                content = g;
            }

            var border = new Border
            {
                Content = content,
                BackgroundColor = Theme.Surface, Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 14 }, Padding = new Thickness(16, 14)
            };
            _answerBtns.Add((border, isRight));
            Ui.OnTap(border, (_, _) => { if (!_answered) Answer(isRight, q, border); });
            return border;
        }

        private void Answer(bool correct, QuizQuestion q, Border chosen)
        {
            _answered = true;
            if (correct) { _score++; AppSettings.Tap(); } else AppSettings.LongPress();
            _scoreLabel.Text = $"Score {_score}";

            // Paint the reveal onto the buttons: the right answer always goes
            // green; a wrong pick goes red, so the eye gets both facts at once.
            foreach (var (btn, right) in _answerBtns)
            {
                if (right)
                {
                    btn.BackgroundColor = Ui.MultiplyAlpha(Theme.Success, 0.16f);
                    btn.Stroke = Theme.Success;
                    btn.StrokeThickness = 2;
                }
                else if (btn == chosen)
                {
                    btn.BackgroundColor = Ui.MultiplyAlpha(Theme.Danger, 0.14f);
                    btn.Stroke = Theme.Danger;
                    btn.StrokeThickness = 2;
                }
                else
                {
                    btn.Opacity = 0.55;
                }
            }

            // explanation card
            var head = new Label
            {
                Text = correct ? "✓ Correct!" : "✗ Not quite",
                FontFamily = Ui.Display, FontSize = Ui.S(18),
                TextColor = correct ? Theme.Success : Theme.Danger
            };
            var expl = new Label { Text = q.Explanation, FontFamily = Ui.Fonts, FontSize = Ui.S(14), LineHeight = 1.4, TextColor = Theme.TextPrimary };
            var col = new VerticalStackLayout { Spacing = 8, Children = { head, expl } };
            _body.Add(new Border
            {
                Content = col,
                BackgroundColor = correct ? Ui.MultiplyAlpha(Theme.Success, 0.12f) : Ui.MultiplyAlpha(Theme.Danger, 0.12f),
                Stroke = correct ? Ui.MultiplyAlpha(Theme.Success, 0.5f) : Ui.MultiplyAlpha(Theme.Danger, 0.5f), StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 14 }, Padding = new Thickness(16)
            });

            bool last = _index >= _questions.Count - 1;
            var nextLabel = new Label { Text = last ? "See results" : "Next question", FontFamily = Ui.Fonts, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextOnAccent, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
            var next = new Border
            {
                Content = nextLabel,
                BackgroundColor = _accent, Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(16, 14)
            };
            Ui.OnTap(next, (_, _) => { if (last) ShowResults(); else { _index++; ShowQuestion(); } });
            _body.Add(next);
        }

        private void ShowResults()
        {
            StatsStore.RecordQuiz(_mode, _score, _questions.Count);
            AppSettings.LongPress();

            int pct = (int)Math.Round(100.0 * _score / _questions.Count);
            _progress.Progress = 1;
            _counter.Text = "Complete";
            _body.Children.Clear();

            string verdict = pct switch
            {
                100 => "Perfect score! You're a true expert. 🦖",
                >= 80 => "Amazing work! You really know your stuff.",
                >= 60 => "Nice job! You're learning fast.",
                >= 40 => "Good effort — keep exploring to level up!",
                _ => "Every expert starts somewhere. Try again!"
            };

            var ring = new Border
            {
                WidthRequest = 130, HeightRequest = 130,
                BackgroundColor = Ui.MultiplyAlpha(_accent, 0.14f),
                Stroke = _accent, StrokeThickness = 3,
                StrokeShape = new RoundRectangle { CornerRadius = 65 },
                HorizontalOptions = LayoutOptions.Center,
                Content = new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center, Spacing = 0,
                    Children =
                    {
                        new Label { Text = $"{pct}%", FontFamily = Ui.Display, FontSize = 34, TextColor = Theme.TextPrimary, HorizontalTextAlignment = TextAlignment.Center },
                        new Label { Text = $"{_score}/{_questions.Count}", FontFamily = Ui.Fonts, FontSize = 13, TextColor = Theme.TextSecondary, HorizontalTextAlignment = TextAlignment.Center }
                    }
                }
            };

            _body.Add(new Label { Text = "Quiz complete!", FontFamily = Ui.Display, FontSize = 26, TextColor = Theme.TextPrimary, HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0, 20, 0, 8) });
            _body.Add(ring);
            _body.Add(new Label { Text = verdict, FontFamily = Ui.Fonts, FontSize = 15, LineHeight = 1.4, TextColor = Theme.TextSecondary, HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(20, 8) });

            var retry = new Border
            {
                Content = new Label { Text = "Play again", FontFamily = Ui.Fonts, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextOnAccent, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center },
                BackgroundColor = _accent, Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(16, 14), Margin = new Thickness(0, 16, 0, 0)
            };
            Ui.OnTap(retry, async (_, _) => { await Navigation.PopAsync(); await Nav.Push(() => new QuizPage(_mode, _questions.Count)); });
            _body.Add(retry);

            var done = new Border
            {
                Content = new Label { Text = "Done", FontFamily = Ui.Fonts, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextPrimary, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center },
                BackgroundColor = Colors.Transparent, Stroke = Theme.Hairline, StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(16, 14)
            };
            Ui.OnTap(done, async (_, _) => await Close());
            _body.Add(done);
        }

        private static Color DifficultyColor(QuizDifficulty d) => d switch
        {
            QuizDifficulty.Easy => Theme.Success,
            QuizDifficulty.Medium => Theme.AccentDino,
            _ => Theme.Danger
        };

        private async System.Threading.Tasks.Task Close()
        {
            try { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); } catch { }
        }
    }
}
