using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

namespace dinospace.Views
{
    // NovaSaur chat. Owns three states: the one-time model setup/download
    // overlay, the "waking up" model init, and the live conversation with a
    // typewriter reveal. Kept as a persistent tab so the chat is preserved.
    public class NovaView : ContentView, ITabView
    {
        private const string HistoryKey = "nova_history_v2";
        private const int MaxSaved = 60;

        private const string Welcome =
            "Hey, I'm NovaSaur! Ask me anything about dinosaurs or space — like how big a T. Rex was, or why Mars is red. I run right here on your device.";

        // A detail page can queue a question and switch to this tab.
        private static string? _pending;
        public static void Ask(string question) => _pending = question;

        // Settings' full reset wipes the saved conversation too; the shared
        // chat instance is dropped so it can't re-save from memory.
        public static void DeleteSavedChat()
        {
            Preferences.Remove(HistoryKey);
            NovaPage.ResetShared();
        }

        private List<ChatMessage> _messages = new();
        private List<string> _lastEntities = new();

        private VerticalStackLayout _chatStack = null!;
        private ScrollView _chatScroll = null!;
        private Entry _entry = null!;
        private Border _sendBtn = null!;
        private Microsoft.Maui.Controls.Shapes.Path _sendIcon = null!;
        private HorizontalStackLayout _suggestions = null!;
        private ScrollView _suggestionScroll = null!;
        private Grid _inputArea = null!;

        // download overlay
        private Grid _overlay = null!;
        private Label _overlayTitle = null!, _overlayBody = null!, _overlayStatus = null!;
        private Border _downloadBtn = null!;
        private Label _downloadBtnLabel = null!;
        private ProgressBar _progress = null!;
        private VerticalStackLayout _progressArea = null!;
        private HorizontalStackLayout _pauseRow = null!;
        private Label _pauseLabel = null!;
        private IDispatcherTimer? _packTimer;
        private bool _subscribed;

        // answering
        private bool _busy;
        private int _gen;
        private bool _chatStarted;
        private bool _modelInited;
        private bool _modelReady;

        // thinking status
        private View? _thinking;
        private Label? _thinkingLabel;
        private IDispatcherTimer? _thinkingTimer;
        private int _thinkingTicks;

        // typewriter reveal
        private Label? _revealLabel;
        private string[]? _revealWords;
        private int _revealIndex;
        private bool _revealActive;
        private readonly StringBuilder _revealSb = new();
        private IDispatcherTimer? _revealTimer;

        // The live instance + reported bottom inset, so the input bar can sit
        // above the navigation/gesture area instead of under it.
        private static NovaView? _live;
        private static double _bottomInset;

        public NovaView() { _live = this; Build(); }

        public static void SetBottomInset(double bottom)
        {
            _bottomInset = bottom;
            _live?.ApplyBottomInset();
        }

        private void ApplyBottomInset()
        {
            if (_inputArea != null)
                _inputArea.Padding = new Thickness(16, 8, 16, 14 + _bottomInset);
        }

        public void OnSelected()
        {
            if (ModelManager.IsModelDownloaded())
            {
                StartChat();
            }
            else
            {
                ModelManager.TryBeginBundledInstall();
                ShowOverlay();
                Subscribe();
                RefreshOverlay();
                StartPackRecheck();
            }
        }

        // ---------- layout ----------
        private void Build()
        {
            _chatStack = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(16, 12, 16, 8) };
            _chatScroll = new ScrollView { Content = _chatStack, VerticalScrollBarVisibility = ScrollBarVisibility.Never };

            // header
            var header = BuildHeader();

            // suggestions
            _suggestions = new HorizontalStackLayout { Spacing = 8 };
            _suggestionScroll = new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = _suggestions, Padding = new Thickness(16, 4) };

            // input
            _inputArea = BuildInput();

            var main = new Grid { RowSpacing = 0 };
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.Add(header, 0, 0);
            main.Add(_chatScroll, 0, 1);
            main.Add(_suggestionScroll, 0, 2);
            main.Add(_inputArea, 0, 3);

            _overlay = BuildOverlay();

            var root = new Grid();
            root.Add(main);
            root.Add(_overlay);
            Content = root;
        }

        private View BuildHeader()
        {
            var back = new Label
            {
                Text = "‹", FontSize = 32, TextColor = Theme.TextPrimary,
                VerticalOptions = LayoutOptions.Center, Padding = new Thickness(0, 0, 6, 2)
            };
            Ui.OnTap(back, async (_, _) =>
            {
                try
                {
                    var nav = Shell.Current?.Navigation;
                    if (nav != null && nav.NavigationStack.Count > 1) await nav.PopAsync();
                }
                catch { }
            });
            Ui.Describe(back, "Go back");

            var dot = new Border
            {
                WidthRequest = 40, HeightRequest = 40,
                BackgroundColor = Ui.MultiplyAlpha(Theme.AccentNova, 0.18f),
                Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Content = new Label { Text = "✦", FontSize = 20, TextColor = Theme.AccentNova, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }
            };
            var title = new VerticalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center };
            title.Add(new Label { Text = "NovaSaur", FontFamily = Ui.Display, FontSize = 19, TextColor = Theme.TextPrimary });
            title.Add(new Label { Text = "Offline dino & space guide", FontFamily = Ui.Fonts, FontSize = 11.5, TextColor = Theme.TextSecondary });

            var clear = new Label { Text = "Clear", FontFamily = Ui.Fonts, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextSecondary, VerticalOptions = LayoutOptions.Center };
            Ui.OnTap(clear, (_, _) => ClearChat());

            var grid = new Grid { Padding = new Thickness(10, 14, 16, 8), ColumnSpacing = 10 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(back, 0, 0); grid.Add(dot, 1, 0); grid.Add(title, 2, 0); grid.Add(clear, 3, 0);
            return grid;
        }

        private Grid BuildInput()
        {
            _entry = new Entry { Placeholder = "Ask about dinosaurs or space...", BackgroundColor = Colors.Transparent, TextColor = Theme.TextPrimary, PlaceholderColor = Theme.TextHint, ReturnType = ReturnType.Send };
            _entry.Completed += (_, _) => OnSend();
            var entryWrap = new Border
            {
                Content = _entry,
                BackgroundColor = Theme.Surface,
                Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 22 },
                Padding = new Thickness(16, 0)
            };

            _sendIcon = Ui.Icon(Ui.IconSend, 20, Theme.TextOnAccent);
            _sendBtn = new Border
            {
                Content = _sendIcon,
                WidthRequest = 46, HeightRequest = 46,
                BackgroundColor = Theme.AccentNova,
                Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 23 }
            };
            Ui.OnTap(_sendBtn, (_, _) => OnSend());
            Ui.Describe(_sendBtn, "Send question");

            var grid = new Grid { Padding = new Thickness(16, 8, 16, 14 + _bottomInset), ColumnSpacing = 10 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(entryWrap, 0, 0);
            grid.Add(_sendBtn, 1, 0);
            return grid;
        }

        // ---------- chat ----------
        private void StartChat()
        {
            if (_chatStarted) { MaybeRunPending(); return; }
            _chatStarted = true;
            StopPackRecheck();
            _overlay.IsVisible = false;
            LoadHistory();
            InitModel();
        }

        private void InitModel()
        {
            if (!NovaSaurService.SupportedPlatform)
            {
                if (_messages.Count == 0) AddNova("NovaSaur runs on Android right now. The rest of DinoSpace works everywhere!");
                SetSendEnabled(false);
                _suggestionScroll.IsVisible = false;
                return;
            }

            // NovaSaur can answer most dinosaur and space questions instantly
            // from its built-in knowledge, so the chat is usable right away —
            // no waiting on the large model to load. The model is only needed
            // for rarer open-ended questions, and it warms up in the background
            // and loads on demand (see Answer()).
            _modelReady = true;
            SetSendEnabled(true);
            if (_messages.Count == 0) AddNova(Welcome);
            ShowSuggestions();

            if (!_modelInited && !NovaSaurService.IsReady)
            {
                _modelInited = true;
                _ = Task.Run(async () =>
                {
                    try { await NovaSaurService.InitAsync(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Nova init: " + ex); }
                });
            }

            MaybeRunPending();
        }

        private void SetSendEnabled(bool enabled)
        {
            _sendBtn.IsEnabled = enabled;
            _sendBtn.Opacity = enabled ? 1 : 0.45;
        }

        private void MaybeRunPending()
        {
            if (_pending == null || _busy || !_modelReady) return;
            string q = _pending; _pending = null;
            AddUser(q);
            Answer(q);
        }

        private void OnSend()
        {
            if (_busy) { StopGeneration(); return; }
            if (!_modelReady) return;
            string q = (_entry.Text ?? "").Trim();
            if (q.Length == 0) return;
            _entry.Text = "";
            AddUser(q);
            Answer(q);
        }

        private async void Answer(string question)
        {
            _busy = true;
            Ui.SetIcon(_sendIcon, Ui.IconStop, Theme.TextOnAccent);
            int myGen = ++_gen;
            StartThinking();
            _ = FailsafeAsync(myGen);   // hard guarantee: "thinking…" can never last forever
            await ScrollToEnd();

            NovaTurn turn;
            try { turn = PromptBuilder.Build(question, _messages, _lastEntities); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Nova prompt: " + ex); FinishAnswer(myGen, NovaSaurService.ErrorMessage); return; }

            if (turn.Entities.Count > 0) _lastEntities = new List<string>(turn.Entities);

            if (turn.InstantReply != null)
            {
                await Task.Delay(250);
                if (myGen != _gen) return;
                StopThinking();
                Reveal(turn.InstantReply); // typewriter for instant grounded replies
                return;
            }

            // This question needs the language model. Hard rule: NEVER make the
            // user wait for it to load. If it isn't ready yet, answer instantly
            // with a redirect and keep warming it up in the background — the
            // encyclopedia already covers everything askable by name.
            if (!NovaSaurService.IsReady)
            {
                _ = NovaSaurService.InitAsync();
                await Task.Delay(250);
                FinishAnswer(myGen, "Ooh, that's a big open question — my deep-thinking brain is still waking up, so ask me that one again in a couple of minutes. Meanwhile I can instantly answer anything about a specific creature or place: try \"how strong was T. Rex?\" or \"what's in the sky tonight?\"");
                return;
            }

            // Stream the model's answer straight into a live bubble — words
            // appear as they're generated instead of after a long silence.
            StopThinking();
            var live = StartNovaBubble();
            var liveSb = new StringBuilder();
            bool gotTokens = false;

            string answer;
            try
            {
                answer = await NovaSaurService.AskStreamAsync(turn.Prompt!, token =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (myGen != _gen) return;
                        gotTokens = true;
                        liveSb.Append(token);
                        live.Text = liveSb.ToString();
                        try { _ = _chatScroll.ScrollToAsync(0, _chatStack.Height, false); } catch { }
                    });
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Nova ask: " + ex);
                answer = NovaSaurService.ErrorMessage;
            }

            if (myGen != _gen) return;

            // final text is the cleaned version (or a friendly failure line)
            live.Text = answer;
            if (answer.Length > 0) { _messages.Add(new ChatMessage { IsUser = false, Text = answer }); SaveHistory(); }
            _busy = false;
            Ui.SetIcon(_sendIcon, Ui.IconSend, Theme.TextOnAccent);
            ShowSuggestions();
            _ = ScrollToEnd();
            _ = gotTokens;   // (kept for future "was it streamed" telemetry-free logic)
        }

        private void FinishAnswer(int myGen, string finalAnswer)
        {
            if (myGen != _gen) return;
            StopThinking();
            Reveal(finalAnswer);
        }

        // Backstop for anything unforeseen (a wedged native call, a swallowed
        // exception): if this turn is somehow still "thinking" after 100
        // seconds, end it with a friendly timeout instead of hanging the chat.
        private async Task FailsafeAsync(int myGen)
        {
            await Task.Delay(TimeSpan.FromSeconds(100));
            if (myGen == _gen && _busy && !_revealActive)
                FinishAnswer(myGen, NovaSaurService.TimeoutMessage);
        }

        private void StopGeneration()
        {
            if (_revealActive && _revealLabel != null) { FinishRevealNow(); return; }
            _gen++;
            StopThinking();
            _busy = false;
            Ui.SetIcon(_sendIcon, Ui.IconSend, Theme.TextOnAccent);
            ShowSuggestions();
        }

        // ---------- thinking status ----------
        private void StartThinking()
        {
            _thinkingTicks = 0;
            var label = new Label { Text = "NovaSaur is thinking…", FontFamily = Ui.Fonts, FontSize = Ui.S(13), FontAttributes = FontAttributes.Italic, TextColor = Theme.TextSecondary, HorizontalOptions = LayoutOptions.Start };
            _thinkingLabel = label;
            _thinking = label;
            _chatStack.Add(label);

            // Reassure on long prompt-processing waits instead of looking hung.
            _thinkingTimer ??= MakeTimer(TimeSpan.FromSeconds(8), () =>
            {
                _thinkingTicks++;
                if (_thinkingLabel == null) return;
                _thinkingLabel.Text = _thinkingTicks switch
                {
                    1 => "NovaSaur is thinking… reading up on your question.",
                    2 => "Still thinking — big questions take a moment on-device…",
                    _ => "Almost there — writing the answer…",
                };
            });
            _thinkingTimer.Start();
        }

        private void StopThinking()
        {
            _thinkingTimer?.Stop();
            _thinkingLabel = null;
            if (_thinking != null) { Remove(_thinking); _thinking = null; }
        }

        // ---------- typewriter reveal ----------
        private void Reveal(string text)
        {
            _revealWords = text.Split(' ');
            _revealIndex = 0;
            _revealSb.Clear();
            _revealLabel = StartNovaBubble();
            _revealActive = true;

            _revealTimer ??= MakeTimer(TimeSpan.FromMilliseconds(40), RevealTick);
            _revealTimer.Start();
        }

        private void RevealTick()
        {
            // A Clear can null things out from under a queued tick.
            if (!_revealActive || _revealWords == null || _revealLabel == null) { _revealTimer?.Stop(); return; }
            if (_revealIndex >= _revealWords.Length) { CompleteReveal(); return; }
            _revealSb.Append(_revealWords[_revealIndex]);
            if (_revealIndex < _revealWords.Length - 1) _revealSb.Append(' ');
            _revealIndex++;
            _revealLabel.Text = _revealSb.ToString();
            try { _ = _chatScroll.ScrollToAsync(0, _chatStack.Height, false); } catch { }
        }

        private void FinishRevealNow()
        {
            if (_revealWords != null && _revealLabel != null)
            {
                _revealSb.Clear();
                _revealSb.Append(string.Join(" ", _revealWords));
                _revealLabel.Text = _revealSb.ToString();
            }
            CompleteReveal();
        }

        private void CompleteReveal()
        {
            _revealTimer?.Stop();
            if (!_revealActive) return;
            _revealActive = false;
            string full = _revealSb.ToString().Trim();
            if (full.Length > 0) { _messages.Add(new ChatMessage { IsUser = false, Text = full }); SaveHistory(); }
            _revealLabel = null; _revealWords = null;
            _busy = false; Ui.SetIcon(_sendIcon, Ui.IconSend, Theme.TextOnAccent);
            ShowSuggestions();
            _ = ScrollToEnd();
        }

        // ---------- bubbles ----------
        private void AddUser(string text) => AddMessage(text, true);
        private void AddNova(string text) => AddMessage(text, false);

        private void AddMessage(string text, bool isUser)
        {
            _messages.Add(new ChatMessage { IsUser = isUser, Text = text });
            _chatStack.Add(Bubble(text, isUser));
            SaveHistory();
            _ = ScrollToEnd();
        }

        private View Bubble(string text, bool isUser)
        {
            var label = new Label { Text = text, FontFamily = Ui.Fonts, FontSize = Ui.S(15), LineHeight = 1.4, TextColor = isUser ? Theme.TextOnAccent : Theme.TextPrimary };
            var bubble = new Border
            {
                Content = label,
                Padding = new Thickness(14, 11),
                BackgroundColor = isUser ? Theme.AccentNova : Theme.Surface,
                Stroke = isUser ? Colors.Transparent : Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = isUser ? new CornerRadius(16, 16, 16, 4) : new CornerRadius(16, 16, 4, 16) },
                HorizontalOptions = isUser ? LayoutOptions.End : LayoutOptions.Start,
                MaximumWidthRequest = 300
            };
            if (!isUser) Ui.OnTap(bubble, async (_, _) => await AnswerMenu(label.Text), haptic: false);
            return bubble;
        }

        private Label StartNovaBubble()
        {
            var label = new Label { Text = "", FontFamily = Ui.Fonts, FontSize = Ui.S(15), LineHeight = 1.4, TextColor = Theme.TextPrimary };
            var bubble = new Border
            {
                Content = label,
                Padding = new Thickness(14, 11),
                BackgroundColor = Theme.Surface,
                Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16, 16, 4, 16) },
                HorizontalOptions = LayoutOptions.Start,
                MaximumWidthRequest = 300
            };
            Ui.OnTap(bubble, async (_, _) => await AnswerMenu(label.Text), haptic: false);
            _chatStack.Add(bubble);
            return label;
        }

        private View AddStatus(string text)
        {
            var label = new Label { Text = text, FontFamily = Ui.Fonts, FontSize = Ui.S(13), FontAttributes = FontAttributes.Italic, TextColor = Theme.TextSecondary, HorizontalOptions = LayoutOptions.Start };
            _chatStack.Add(label);
            _ = ScrollToEnd();
            return label;
        }

        private void Remove(View v) { if (v != null && _chatStack.Contains(v)) _chatStack.Remove(v); }

        private async Task AnswerMenu(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return;
            string choice = await page.DisplayActionSheetAsync(null, "Cancel", null, "Copy", "Report this answer");
            if (choice == "Copy") { try { await Clipboard.Default.SetTextAsync(text); } catch { } }
            else if (choice == "Report this answer") await ReportAnswer(page, text);
        }

        // Opens the user's own email app with the answer pre-filled — nothing
        // is sent unless they hit send themselves, keeping the app's
        // zero-data-collection promise intact.
        private static async Task ReportAnswer(Page page, string text)
        {
            string body = Uri.EscapeDataString("This answer looked wrong or strange:\n\n" + text);
            try
            {
                await Launcher.OpenAsync($"mailto:dinospace.app@gmail.com?subject=DinoSpace%3A%20report%20an%20AI%20answer&body={body}");
            }
            catch
            {
                try { await Clipboard.Default.SetTextAsync(text); } catch { }
                try { await page.DisplayAlertAsync("Thanks for flagging it", "No email app was found, so the answer was copied instead — paste it in a message to dinospace.app@gmail.com.", "OK"); } catch { }
            }
        }

        // ---------- suggestions ----------
        private void ShowSuggestions()
        {
            _suggestions.Children.Clear();
            foreach (var q in SuggestedQuestions.Pick(4))
            {
                var label = new Label { Text = q, FontFamily = Ui.Fonts, FontSize = 12.5, TextColor = Theme.ChipText };
                var chip = new Border
                {
                    Content = label,
                    BackgroundColor = Theme.ChipBg, Stroke = Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = 16 },
                    Padding = new Thickness(14, 8)
                };
                Ui.OnTap(chip, (_, _) => { if (_busy) return; _entry.Text = q; OnSend(); });
                _suggestions.Add(chip);
            }
            _suggestionScroll.IsVisible = true;
        }

        private void ClearChat()
        {
            _gen++;
            _revealTimer?.Stop();
            _revealActive = false; _revealLabel = null; _revealWords = null;
            StopThinking();
            _busy = false; Ui.SetIcon(_sendIcon, Ui.IconSend, Theme.TextOnAccent);
            _messages.Clear();
            _lastEntities.Clear();
            Preferences.Remove(HistoryKey);
            _chatStack.Children.Clear();
            AddNova(Welcome);
            ShowSuggestions();
        }

        // ---------- history ----------
        private void SaveHistory()
        {
            try
            {
                if (_messages.Count > MaxSaved) _messages.RemoveRange(0, _messages.Count - MaxSaved);
                Preferences.Set(HistoryKey, JsonSerializer.Serialize(_messages));
            }
            catch { }
        }

        private void LoadHistory()
        {
            try
            {
                var json = Preferences.Get(HistoryKey, "");
                if (string.IsNullOrEmpty(json)) return;
                var saved = JsonSerializer.Deserialize<List<ChatMessage>>(json);
                if (saved == null) return;
                _messages = saved;
                foreach (var m in _messages) _chatStack.Add(Bubble(m.Text, m.IsUser));
            }
            catch { }
        }

        private async Task ScrollToEnd()
        {
            try
            {
                await Task.Delay(40);
                if (_chatScroll != null && _chatStack != null)
                    await _chatScroll.ScrollToAsync(0, _chatStack.Height, true);
            }
            catch { }
        }

        private IDispatcherTimer MakeTimer(TimeSpan interval, Action tick)
        {
            var t = Dispatcher.CreateTimer();
            t.Interval = interval;
            t.Tick += (_, _) => tick();
            return t;
        }

        // ---------- download overlay ----------
        private Grid BuildOverlay()
        {
            _overlayTitle = new Label { Text = "Meet NovaSaur", FontFamily = Ui.Display, FontSize = 26, TextColor = Theme.TextPrimary, HorizontalTextAlignment = TextAlignment.Center };
            _overlayBody = new Label { FontFamily = Ui.Fonts, FontSize = 14.5, LineHeight = 1.45, TextColor = Theme.TextSecondary, HorizontalTextAlignment = TextAlignment.Center };
            _overlayStatus = new Label { FontFamily = Ui.Fonts, FontSize = 13, TextColor = Theme.TextSecondary, HorizontalTextAlignment = TextAlignment.Center };

            _progress = new ProgressBar { Progress = 0, HeightRequest = 8, ProgressColor = Theme.Accent, BackgroundColor = Theme.SurfaceAlt };
            _pauseLabel = new Label { Text = "Pause", FontFamily = Ui.Fonts, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Theme.Accent };
            Ui.OnTap(_pauseLabel, (_, _) => OnPauseResume());
            var stopLabel = new Label { Text = "Stop", FontFamily = Ui.Fonts, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextSecondary };
            Ui.OnTap(stopLabel, (_, _) => OnStop());
            _pauseRow = new HorizontalStackLayout { Spacing = 24, HorizontalOptions = LayoutOptions.Center, Children = { _pauseLabel, stopLabel } };

            _progressArea = new VerticalStackLayout { Spacing = 12, IsVisible = false };
            _progressArea.Add(_overlayStatus);
            _progressArea.Add(_progress);
            _progressArea.Add(_pauseRow);

            _downloadBtnLabel = new Label { Text = "Download NovaSaur", FontFamily = Ui.Fonts, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextOnAccent, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
            _downloadBtn = new Border
            {
                Content = _downloadBtnLabel,
                BackgroundColor = Theme.AccentNova, Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(20, 14)
            };
            Ui.OnTap(_downloadBtn, (_, _) => OnDownload());

            var icon = new Border
            {
                WidthRequest = 72, HeightRequest = 72,
                BackgroundColor = Ui.MultiplyAlpha(Theme.AccentNova, 0.18f),
                Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 36 },
                HorizontalOptions = LayoutOptions.Center,
                Content = new Label { Text = "✦", FontSize = 34, TextColor = Theme.AccentNova, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }
            };

            var card = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(24), VerticalOptions = LayoutOptions.Center };
            card.Add(icon);
            card.Add(_overlayTitle);
            card.Add(_overlayBody);
            card.Add(_downloadBtn);
            card.Add(_progressArea);

            var g = new Grid { BackgroundColor = Theme.Bg, Padding = new Thickness(20) };
            g.Add(new ScrollView { Content = card });
            g.IsVisible = false;
            return g;
        }

        private void ShowOverlay() => _overlay.IsVisible = true;

        private void Subscribe()
        {
            if (_subscribed) return;
            ModelManager.Changed += OnModelChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            ModelManager.Changed -= OnModelChanged;
            _subscribed = false;
        }

        private void OnModelChanged() => MainThread.BeginInvokeOnMainThread(RefreshOverlay);

        private void StartPackRecheck()
        {
            _packTimer ??= MakeTimer(TimeSpan.FromSeconds(5), () =>
            {
                if (_chatStarted || ModelManager.State == DownloadState.Downloading) return;
                if (ModelManager.TryBeginBundledInstall()) RefreshOverlay();
            });
            _packTimer.Start();
        }

        private void StopPackRecheck() => _packTimer?.Stop();

        private void RefreshOverlay()
        {
            switch (ModelManager.State)
            {
                case DownloadState.Completed:
                    Unsubscribe(); StopPackRecheck(); StartChat();
                    break;
                case DownloadState.Downloading:
                    _downloadBtn.IsVisible = false;
                    _progressArea.IsVisible = true;
                    _progress.Progress = ModelManager.Progress;
                    if (ModelManager.IsLocalInstall)
                    {
                        _overlayTitle.Text = "Getting NovaSaur ready";
                        _overlayBody.Text = "One-time setup, about a minute. Keep DinoSpace open while it finishes.";
                        _overlayStatus.Text = ProgressText("Setting up");
                        _pauseRow.IsVisible = false;
                    }
                    else
                    {
                        _overlayTitle.Text = "Downloading NovaSaur";
                        _overlayBody.Text = "Keep exploring the app while it downloads. If you leave, it picks up right where it left off next time.";
                        _overlayStatus.Text = ProgressText("Downloading");
                        _pauseRow.IsVisible = true;
                        _pauseLabel.Text = "Pause";
                    }
                    break;
                case DownloadState.Paused:
                    _downloadBtn.IsVisible = false;
                    _progressArea.IsVisible = true;
                    _progress.Progress = ModelManager.Progress;
                    _overlayStatus.Text = ProgressText("Paused at");
                    _pauseLabel.Text = "Resume";
                    break;
                case DownloadState.Failed:
                    _overlayTitle.Text = "Download paused";
                    _overlayBody.Text = "The download stopped. Tap resume and it picks up where it left off.";
                    _downloadBtn.IsVisible = true;
                    _downloadBtnLabel.Text = "Resume download";
                    _progressArea.IsVisible = false;
                    break;
                default:
                    _overlayTitle.Text = "Meet NovaSaur";
                    if (ModelManager.BundledPartsFound() > 0)
                    {
                        _overlayBody.Text = "Google Play is finishing NovaSaur's setup in the background. It'll appear here automatically in a few minutes.";
                        _downloadBtn.IsVisible = false;
                    }
                    else
                    {
                        _overlayBody.Text = "An offline AI that answers your dinosaur and space questions, right on your phone. It's a one-time download of about 3 GB (wifi recommended).";
                        _downloadBtn.IsVisible = true;
                        _downloadBtnLabel.Text = ModelManager.HasPartialDownload() ? "Resume download" : "Download NovaSaur";
                    }
                    _progressArea.IsVisible = false;
                    break;
            }
        }

        private string ProgressText(string prefix)
        {
            int pct = (int)(ModelManager.Progress * 100);
            long total = ModelManager.TotalBytes;
            if (total > 0)
            {
                double done = ModelManager.DoneBytes / 1_000_000_000.0;
                double tot = total / 1_000_000_000.0;
                return $"{prefix} {pct}% ({done:0.0} of {tot:0.0} GB)";
            }
            return $"{prefix} {pct}%";
        }

        private async void OnDownload()
        {
            if (ModelManager.State == DownloadState.Downloading) return;
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            long free = ModelManager.GetFreeSpaceBytes();
            long needed = Math.Max(500_000_000, ModelManager.RequiredFreeBytes - ModelManager.GetPartialSizeBytes());
            if (free >= 0 && free < needed && page != null)
            {
                await page.DisplayAlertAsync("Not enough space", $"NovaSaur needs about {needed / 1_000_000_000.0:0.0} GB free. Free up some space and try again.", "OK");
                return;
            }
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet && page != null)
            {
                await page.DisplayAlertAsync("No internet", "Connect to the internet to download NovaSaur.", "OK");
                return;
            }
            if (!Connectivity.Current.ConnectionProfiles.Contains(ConnectionProfile.WiFi) && page != null)
            {
                bool go = await page.DisplayAlertAsync("Not on wifi", "This is a large download (about 3 GB). Downloading over mobile data may use up your plan. Download anyway?", "Download", "Wait for wifi");
                if (!go) return;
            }
            ModelManager.Start();
            RefreshOverlay();
        }

        private void OnPauseResume()
        {
            if (ModelManager.State == DownloadState.Downloading) ModelManager.Pause();
            else ModelManager.Start();
            RefreshOverlay();
        }

        private async void OnStop()
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return;
            bool sure = await page.DisplayAlertAsync("Stop download?", "This deletes what's downloaded so far. You'd start over next time.", "Stop", "Keep downloading");
            if (!sure) return;
            ModelManager.Stop();
            RefreshOverlay();
        }
    }
}
