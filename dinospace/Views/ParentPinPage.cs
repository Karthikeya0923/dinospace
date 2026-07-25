using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The 4-digit PIN pad for parent mode. One page, two jobs:
    //   Set    — choose a PIN, type it twice, parent mode turns on.
    //   Unlock — type the existing PIN to open the parent controls.
    // On success the page pops itself and runs the caller's onSuccess.
    public class ParentPinPage : ContentPage
    {
        public enum PinMode { Set, Unlock }

        private readonly PinMode _mode;
        private readonly Action? _onSuccess;
        private string _current = "";
        private string? _first;          // Set flow: the first of the two entries
        private Label _prompt = null!;
        private readonly Border[] _dots = new Border[4];
        private bool _busy;

        public ParentPinPage(PinMode mode, Action? onSuccess = null)
        {
            _mode = mode;
            _onSuccess = onSuccess;
            Shell.SetNavBarIsVisible(this, false);
            Build();
            SwipeBack.Attach(this);
        }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 0, Padding = new Thickness(24, 8, 24, 24) };

            _prompt = new Label
            {
                Text = Ui.T(_mode == PinMode.Set ? "Choose a 4-digit PIN" : "Enter your PIN"),
                FontFamily = Ui.Fonts, FontSize = Ui.S(15.5),
                TextColor = Theme.TextSecondary,
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 18, 0, 22)
            };
            stack.Add(_prompt);

            // the four dots
            var dots = new HorizontalStackLayout { Spacing = 18, HorizontalOptions = LayoutOptions.Center };
            for (int i = 0; i < 4; i++)
            {
                _dots[i] = new Border
                {
                    WidthRequest = 20, HeightRequest = 20,
                    BackgroundColor = Theme.SurfaceAlt,
                    Stroke = Theme.Hairline, StrokeThickness = 1.2,
                    StrokeShape = new RoundRectangle { CornerRadius = 10 }
                };
                dots.Add(_dots[i]);
            }
            stack.Add(dots);

            // the keypad
            var pad = new Grid { ColumnSpacing = 18, RowSpacing = 14, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 34, 0, 0) };
            for (int c = 0; c < 3; c++) pad.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            for (int r = 0; r < 4; r++) pad.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            void Key(View content, int col, int row, Action onTap)
            {
                var key = new Border
                {
                    WidthRequest = 76, HeightRequest = 76,
                    BackgroundColor = Theme.Surface,
                    Stroke = Theme.CardStroke, StrokeThickness = 1.4,
                    StrokeShape = new RoundRectangle { CornerRadius = 38 },
                    Content = content,
                    Shadow = Theme.CardShadow()
                };
                Ui.OnTap(key, (_, _) => onTap());
                pad.Add(key, col, row);
            }

            Label Digit(string d) => new()
            {
                Text = d, FontFamily = Ui.Display, FontSize = Ui.S(26),
                TextColor = Theme.TextPrimary,
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
            };

            int n = 1;
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                {
                    string d = n.ToString(); n++;
                    Key(Digit(d), c, r, () => OnDigit(d));
                }
            Key(Digit("0"), 1, 3, () => OnDigit("0"));
            var back = Ui.Icon(Ui.IconBack, 26);
            back.HorizontalOptions = LayoutOptions.Center;
            back.VerticalOptions = LayoutOptions.Center;
            Key(back, 2, 3, OnBackspace);
            stack.Add(pad);

            var body = new Grid();
            body.Add(new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never });
            Content = Ui.PageRoot(Nav.DetailScaffoldFixed("parent mode", body));
        }

        private void RefreshDots()
        {
            for (int i = 0; i < 4; i++)
            {
                bool filled = i < _current.Length;
                _dots[i].BackgroundColor = filled ? Theme.Accent : Theme.SurfaceAlt;
                _dots[i].Stroke = filled ? Colors.Transparent : Theme.Hairline;
            }
        }

        private async void OnDigit(string d)
        {
            if (_busy || _current.Length >= 4) return;
            _current += d;
            RefreshDots();
            if (_current.Length == 4) await HandleFull();
        }

        private void OnBackspace()
        {
            if (_busy || _current.Length == 0) return;
            _current = _current[..^1];
            RefreshDots();
        }

        private async Task HandleFull()
        {
            _busy = true;
            await Task.Delay(140);   // let the last dot paint before acting

            if (_mode == PinMode.Unlock)
            {
                if (ParentMode.Check(_current)) { await Succeed(); return; }
                _prompt.Text = Ui.T("That's not it — try again");
                Reset();
            }
            else if (_first == null)
            {
                _first = _current;
                _prompt.Text = Ui.T("Type it again to confirm");
                Reset();
            }
            else if (_first == _current)
            {
                ParentMode.Enable(_first);
                await Succeed();
            }
            else
            {
                _first = null;
                _prompt.Text = Ui.T("Those didn't match — choose a PIN");
                Reset();
            }
        }

        private void Reset()
        {
            _current = "";
            RefreshDots();
            _busy = false;
        }

        private async Task Succeed()
        {
            try
            {
                var nav = Shell.Current?.Navigation;
                if (nav != null && nav.NavigationStack.Count > 1) await nav.PopAsync(animated: false);
            }
            catch { }
            _onSuccess?.Invoke();
        }
    }
}
