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
using Microsoft.Maui.Storage;

namespace dinospace.Views
{
    // NovaSaur chat — a fully offline dinosaur & space buddy.
    //
    // The chat opens instantly and answers every question right on the device:
    // the vetted encyclopedia (LocalAnswer) covers the facts, and NovaCreative
    // covers jokes, little stories, poems and open-ended chat. NO download is
    // required. If the optional large language model happens to be installed
    // (a Play asset-pack build), open-ended answers stream from it for extra
    // richness — but the chat never waits on it and never blocks behind it.
    //
    // Kept as a persistent instance so the conversation survives tab switches.
    public class NovaView : ContentView, ITabView
    {
        private const string HistoryKey = "nova_history_v2";
        private const int MaxSaved = 60;

        private const string Welcome =
            "Hi, I'm NovaSaur! Ask me anything about prehistoric creatures or space — how big a T. Rex was, why Mars is red, or even for a joke or a story. I work right here on your device, no internet needed!";

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

        // answering
        private bool _busy;
        private bool _streaming;
        private int _gen;
        private DateTime _lastSend = DateTime.MinValue;
        private bool _chatStarted;
        private bool _modelInited;

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

        public void OnSelected() => StartChat();

        // ---------- layout ----------
        private void Build()
        {
            _chatStack = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(16, 12, 16, 8) };
            _chatScroll = new ScrollView { Content = _chatStack, VerticalScrollBarVisibility = ScrollBarVisibility.Never };

            var header = BuildHeader();

            _suggestions = new HorizontalStackLayout { Spacing = 8 };
            _suggestionScroll = new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = _suggestions, Padding = new Thickness(16, 4) };

            _inputArea = BuildInput();

            // The model card sits under the header until the model is
            // installed, then disappears — the chat itself never needs it.
            var modelCard = new NovaModelCard(hideWhenInstalled: true) { Margin = new Thickness(16, 2, 16, 4) };

            var main = new Grid { RowSpacing = 0 };
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.Add(header, 0, 0);
            main.Add(modelCard, 0, 1);
            main.Add(_chatScroll, 0, 2);
            main.Add(_suggestionScroll, 0, 3);
            main.Add(_inputArea, 0, 4);

            Content = main;
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

            // NovaSaur's face — the mascot art when it lands, a chat-bubble
            // sticker until then.
            double dotSize = 46;
            var dot = new Border
            {
                WidthRequest = dotSize, HeightRequest = dotSize,
                BackgroundColor = Ui.MultiplyAlpha(Theme.AccentNova, 0.18f),
                Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = dotSize / 2 },
                Content = Ui.Mascot("mascot_ask", 28, "st_bub_green_dots.png")
            };
            var title = new VerticalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center };
            title.Add(new Label { Text = "ask novasaur", FontFamily = Ui.Display, FontSize = 21, TextColor = Theme.TextPrimary });
            title.Add(new Label { Text = Ui.T("Your dino & space buddy"), FontFamily = Ui.Fonts, FontSize = 11.5, TextColor = Theme.TextSecondary });

            var clear = new Label { Text = Ui.T("Clear"), FontFamily = Ui.Fonts, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextSecondary, VerticalOptions = LayoutOptions.Center };
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
            _entry = new Entry { Placeholder = Ui.T("Ask me anything…"), BackgroundColor = Colors.Transparent, TextColor = Theme.TextPrimary, PlaceholderColor = Theme.TextHint, ReturnType = ReturnType.Send };
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
            LoadHistory();

            if (_messages.Count == 0) AddNova(Welcome);
            ShowSuggestions();

            // The big model is entirely optional. On a Play build that shipped
            // it as asset packs, assemble and warm it up quietly in the
            // background so open-ended answers can stream from it; on a normal
            // install there's nothing to do and the offline brain handles
            // everything. Either way the chat is usable right now.
            WarmModelIfPresent();

            MaybeRunPending();
        }

        private void WarmModelIfPresent()
        {
            if (_modelInited || !NovaSaurService.SupportedPlatform) return;
            _modelInited = true;
            NovaSaurService.EnsureAutoInit();   // load the model the moment a download completes
            _ = Task.Run(() =>
            {
                try
                {
                    ModelManager.TryBeginBundledInstall();
                    if (ModelManager.IsModelDownloaded() && !NovaSaurService.IsReady)
                        _ = NovaSaurService.InitAsync();
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Nova warm: " + ex); }
            });
        }

        private void MaybeRunPending()
        {
            if (_pending == null || _busy) return;
            string q = _pending; _pending = null;
            AddUser(q);
            Answer(q);
        }

        // A suggestion chip was tapped. Debounced so spam-tapping can't stack
        // up, and it cleanly interrupts an answer in progress before starting
        // the new one.
        private void SendFromChip(string q)
        {
            if ((DateTime.Now - _lastSend).TotalMilliseconds < 500) return;
            if (_busy) StopGeneration();
            _entry.Text = q;
            OnSend();
        }

        private void OnSend()
        {
            if (_busy) { StopGeneration(); return; }
            string q = (_entry.Text ?? "").Trim();
            if (q.Length == 0) return;
            _lastSend = DateTime.Now;
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
            // Fire-and-forget on purpose: ScrollToAsync can wedge while the
            // keyboard is animating, and awaiting it here silently froze the
            // whole turn — no answer, just the failsafe apology 20s later.
            _ = ScrollToEnd();

            NovaTurn turn;
            try { turn = PromptBuilder.Build(question, _messages, _lastEntities); }
            catch (Exception ex) { Log("prompt build failed: " + ex.Message); FinishAnswer(myGen, NovaSaurService.ErrorMessage); return; }
            _turn = turn;   // the failsafe answers from this turn's offline brain, never an apology

            if (turn.Entities.Count > 0) _lastEntities = new List<string>(turn.Entities);

            // Grounded instant reply from the encyclopedia — the common case.
            if (turn.InstantReply != null)
            {
                Log("instant reply");
                await Task.Delay(200);
                if (myGen != _gen) return;
                StopThinking();
                Reveal(turn.InstantReply);
                return;
            }

            // Open-ended question. If the model is loaded AND warm, stream from
            // it for a richer answer; otherwise answer instantly from the
            // offline brain (a story, a joke, an honest catch-all). We never
            // wait on the model — a cold engine takes a minute to produce its
            // first token, and pointing questions at it is what used to leave
            // the chat "thinking…" until the timeout.
            if (!NovaSaurService.IsWarm)
            {
                Log("model cold — offline fallback");
                if (myGen != _gen) return;
                StopThinking();
                Reveal(turn.OfflineFallback ?? NovaSaurService.ErrorMessage);
                return;
            }
            Log("model warm — streaming");

            // The bubble is created on the FIRST token, not up front — until
            // then the "thinking…" line stays, so a slow engine warm-up reads
            // as thinking instead of a mysterious empty bubble.
            _streaming = true;
            Label? live = null;
            var liveSb = new StringBuilder();

            string answer;
            try
            {
                answer = await NovaSaurService.AskStreamAsync(turn.Prompt!, token =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // Ignore stragglers after this turn was finalised (a
                        // timed-out stream can keep emitting for a moment).
                        if (myGen != _gen || !_streaming) return;
                        if (live == null) { StopThinking(); live = StartNovaBubble(); }
                        liveSb.Append(token);
                        live.Text = liveSb.ToString();
                        try { _ = _chatScroll.ScrollToAsync(0, _chatStack.Height, false); } catch { }
                    });
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Log("stream threw: " + ex.Message);
                answer = turn.OfflineFallback ?? NovaSaurService.ErrorMessage;
            }

            _streaming = false;
            if (myGen != _gen) return;

            // If the model came back empty or with a failure line, fall back to
            // the offline answer so the turn is never a dead end. The child
            // never reads a timeout apology when a real answer exists.
            if (string.IsNullOrWhiteSpace(answer) || answer == NovaSaurService.ErrorMessage || answer == NovaSaurService.TimeoutMessage)
            {
                Log($"stream failed ({(string.IsNullOrWhiteSpace(answer) ? "empty" : answer == NovaSaurService.TimeoutMessage ? "timeout" : "error")}) — using offline fallback");
                answer = turn.OfflineFallback ?? answer;
            }
            else Log("stream answered");

            StopThinking();                     // still up if no token ever arrived
            live ??= StartNovaBubble();
            live.Text = answer;
            if (answer.Length > 0) { _messages.Add(new ChatMessage { IsUser = false, Text = answer }); SaveHistory(); }
            _busy = false;
            Ui.SetIcon(_sendIcon, Ui.IconSend, Theme.TextOnAccent);
            ShowSuggestions();
            _ = ScrollToEnd();
        }

        private void FinishAnswer(int myGen, string finalAnswer)
        {
            if (myGen != _gen) return;
            _gen++;               // invalidate the in-flight turn so a late
            _streaming = false;   // stream result can't post a second bubble
            StopThinking();
            Reveal(finalAnswer);
        }

        // Backstop for anything unforeseen: if this turn is somehow still
        // "thinking" — not streaming, not revealing — after the watchdog
        // window, end it with the turn's own offline answer (an apology only
        // when there is truly nothing better to say).
        private async Task FailsafeAsync(int myGen)
        {
            await Task.Delay(TimeSpan.FromSeconds(20));
            if (myGen == _gen && _busy && !_revealActive && !_streaming)
            {
                Log("failsafe fired");
                FinishAnswer(myGen, _turn?.OfflineFallback ?? NovaSaurService.TimeoutMessage);
            }
        }

        // adb-visible diagnostics: `adb logcat -s NovaChat` tells the whole
        // story of every turn — which path answered and why.
        private static void Log(string msg)
        {
#if ANDROID
            try { Android.Util.Log.Info("NovaChat", msg); } catch { }
#endif
            System.Diagnostics.Debug.WriteLine("NovaChat: " + msg);
        }

        private NovaTurn? _turn;

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

            _thinkingTimer ??= MakeTimer(TimeSpan.FromSeconds(8), () =>
            {
                _thinkingTicks++;
                if (_thinkingLabel == null) return;
                _thinkingLabel.Text = _thinkingTicks switch
                {
                    1 => "NovaSaur is thinking… reading up on your question.",
                    2 => "Still thinking — big questions take a moment…",
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
            double r = AppLayout.Playful ? 22 : 16;
            var bubble = new Border
            {
                Content = label,
                Padding = new Thickness(14, 11),
                BackgroundColor = isUser ? Theme.AccentNova : Theme.Surface,
                Stroke = isUser ? Colors.Transparent : Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = isUser ? new CornerRadius(r, r, r, 4) : new CornerRadius(r, r, 4, r) },
                HorizontalOptions = isUser ? LayoutOptions.End : LayoutOptions.Start,
                MaximumWidthRequest = 300
            };
            if (!isUser) Ui.OnTap(bubble, async (_, _) => await AnswerMenu(label.Text), haptic: false);
            return bubble;
        }

        private Label StartNovaBubble()
        {
            var label = new Label { Text = "", FontFamily = Ui.Fonts, FontSize = Ui.S(15), LineHeight = 1.4, TextColor = Theme.TextPrimary };
            double r = AppLayout.Playful ? 22 : 16;
            var bubble = new Border
            {
                Content = label,
                Padding = new Thickness(14, 11),
                BackgroundColor = Theme.Surface,
                Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(r, r, 4, r) },
                HorizontalOptions = LayoutOptions.Start,
                MaximumWidthRequest = 300
            };
            Ui.OnTap(bubble, async (_, _) => await AnswerMenu(label.Text), haptic: false);
            _chatStack.Add(bubble);
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
                Ui.OnTap(chip, (_, _) => SendFromChip(q));
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
    }
}
