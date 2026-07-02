using System.Text;
using System.Text.Json;
using System.Linq;
using System.Threading;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Dispatching;

namespace dinospace
{
    public partial class AskAiPage : ContentPage
    {
        private const string HistoryKey = "nova_chat_history";
        private const string WelcomeMessage =
            "Hi, I'm NovaSaur! Ask me anything about dinosaurs or space — like how big a T. Rex was, or why Mars is red.";
        private const int MaxSavedMessages = 60;
        private static readonly TimeSpan AnswerTimeout = TimeSpan.FromSeconds(120);

        private bool _chatStarted = false;
        private bool _subscribed = false;
        private IDispatcherTimer _packRecheckTimer;
        private List<ChatMessage> _messages = new List<ChatMessage>();

        // Which encyclopedia entries the last question was about, so
        // follow-ups like "how fast was it?" know what "it" means.
        private List<string> _lastEntities = new List<string>();

        // answering state
        private bool _busy = false;
        private int _gen = 0;
        private static readonly SemaphoreSlim _aiLock = new SemaphoreSlim(1, 1);

        // word-by-word reveal
        private View _thinkingStatus;
        private Label _revealLabel;
        private string[] _revealWords;
        private int _revealIndex;
        private bool _revealActive = false;
        private readonly StringBuilder _revealSb = new StringBuilder();
        private IDispatcherTimer _revealTimer;

        public AskAiPage()
        {
            InitializeComponent();
            SwipeBack.Attach(this);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (ModelManager.IsModelDownloaded())
            {
                StartChat();
                return;
            }

            // The model normally ships inside the Play Store install as asset
            // packs. If they're on the device, this quietly assembles them
            // into the model file (a one-time ~1 minute local step).
            ModelManager.TryBeginBundledInstall();

            ShowDownloadState();
            Subscribe();
            RefreshDownloadUi();
            StartPackRecheck();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Unsubscribe();
            StopPackRecheck();
        }

        // While the overlay is up, keep checking whether Google Play has
        // finished delivering the bundled model packs in the background.
        private void StartPackRecheck()
        {
            if (_packRecheckTimer == null)
            {
                _packRecheckTimer = Dispatcher.CreateTimer();
                _packRecheckTimer.Interval = TimeSpan.FromSeconds(5);
                _packRecheckTimer.Tick += (s, e) =>
                {
                    if (_chatStarted || ModelManager.State == DownloadState.Downloading)
                        return;
                    if (ModelManager.TryBeginBundledInstall())
                        RefreshDownloadUi();
                    else if (DownloadOverlay.IsVisible && ModelManager.State == DownloadState.NotStarted)
                        RefreshDownloadUi(); // keeps the "still arriving" text fresh
                };
            }
            _packRecheckTimer.Start();
        }

        private void StopPackRecheck()
        {
            _packRecheckTimer?.Stop();
        }

        // ---------- DOWNLOAD ----------

        private void Subscribe()
        {
            if (_subscribed) return;
            ModelManager.Changed += OnDownloadChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            ModelManager.Changed -= OnDownloadChanged;
            _subscribed = false;
        }

        private void OnDownloadChanged() => MainThread.BeginInvokeOnMainThread(RefreshDownloadUi);

        private void ShowDownloadState()
        {
            ChatScroll.IsVisible = false;
            InputArea.IsVisible = false;
            DownloadOverlay.IsVisible = true;
        }

        private void RefreshDownloadUi()
        {
            switch (ModelManager.State)
            {
                case DownloadState.Completed:
                    Unsubscribe();
                    StopPackRecheck();
                    StartChat();
                    break;

                case DownloadState.Downloading:
                    DownloadIntroLabel.IsVisible = false;
                    DownloadButton.IsVisible = false;
                    DownloadProgressArea.IsVisible = true;
                    DownloadProgress.Progress = ModelManager.Progress;
                    if (ModelManager.IsLocalInstall)
                    {
                        // Assembling the bundled model - quick, local, no network.
                        DownloadStatus.Text = ProgressText("Setting up NovaSaur...");
                        DownloadButtonsRow.IsVisible = false;
                        DownloadFooterLabel.Text = "One-time setup, about a minute. Feel free to leave - it finishes in the background.";
                    }
                    else
                    {
                        DownloadStatus.Text = ProgressText("Downloading NovaSaur...");
                        DownloadButtonsRow.IsVisible = true;
                        DownloadFooterLabel.Text = "Feel free to leave and keep using the app. The download keeps going in the background, even if you close DinoSpace.";
                        PauseResumeButton.Text = "Pause";
                    }
                    break;

                case DownloadState.Paused:
                    DownloadIntroLabel.IsVisible = false;
                    DownloadButton.IsVisible = false;
                    DownloadProgressArea.IsVisible = true;
                    DownloadProgress.Progress = ModelManager.Progress;
                    DownloadStatus.Text = ProgressText("Paused at");
                    PauseResumeButton.Text = "Resume";
                    break;

                case DownloadState.Failed:
                    DownloadIntroLabel.IsVisible = true;
                    DownloadIntroLabel.Text = "The download stopped. Tap resume and it picks up where it left off.";
                    DownloadButton.IsVisible = true;
                    DownloadButton.IsEnabled = true;
                    DownloadButton.Text = "Resume download";
                    DownloadProgressArea.IsVisible = false;
                    break;

                default:
                    DownloadIntroLabel.IsVisible = true;
                    if (ModelManager.BundledPartsFound() > 0)
                    {
                        // Play delivered some of the bundled packs already;
                        // the rest are on their way. No user action needed.
                        DownloadIntroLabel.Text = "Google Play is finishing the NovaSaur install in the background. This usually takes a few minutes after installing DinoSpace - it will appear here automatically when it's ready.";
                        DownloadButton.IsVisible = false;
                    }
                    else
                    {
                        DownloadIntroLabel.Text = "An offline AI that answers your dinosaur and space questions, right on your phone. If you installed DinoSpace from Google Play, NovaSaur may still be arriving in the background and will appear here on its own. You can also download it directly (about 3 GB, wifi recommended).";
                        DownloadButton.IsVisible = true;
                        DownloadButton.IsEnabled = true;
                        DownloadButton.Text = ModelManager.HasPartialDownload() ? "Resume download" : "Download NovaSaur";
                    }
                    DownloadProgressArea.IsVisible = false;
                    break;
            }
        }

        // "Downloading NovaSaur... 42% (1.3 of 3.1 GB)" when the size is known.
        private string ProgressText(string prefix)
        {
            int pct = (int)(ModelManager.Progress * 100);
            long total = ModelManager.TotalBytes;
            if (total > 0)
            {
                double doneGb = ModelManager.DoneBytes / 1_000_000_000.0;
                double totalGb = total / 1_000_000_000.0;
                return $"{prefix} {pct}% ({doneGb:0.0} of {totalGb:0.0} GB)";
            }
            return $"{prefix} {pct}%";
        }

        private async void OnDownloadClicked(object sender, EventArgs e)
        {
            if (ModelManager.State == DownloadState.Downloading) return;
            if (!await PreflightDownloadAsync()) return;
            ModelManager.Start();
            RefreshDownloadUi();
        }

        // Checks storage, connection, and notification permission before the
        // 3 GB download starts, so it doesn't fail halfway or surprise anyone
        // on mobile data.
        private async Task<bool> PreflightDownloadAsync()
        {
            // 1) Enough free storage?
            long free = ModelManager.GetFreeSpaceBytes();
            long needed = ModelManager.RequiredFreeBytes - ModelManager.GetPartialSizeBytes();
            if (needed < 500_000_000) needed = 500_000_000;
            if (free >= 0 && free < needed)
            {
                double needGb = needed / 1_000_000_000.0;
                await DisplayAlert("Not enough space",
                    $"NovaSaur needs about {needGb:0.0} GB of free space and this device doesn't have enough right now. Free up some space and try again.",
                    "OK");
                return false;
            }

            // 2) Online? On wifi?
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await DisplayAlert("No internet", "Connect to the internet to download NovaSaur.", "OK");
                return false;
            }
            bool onWifi = Connectivity.Current.ConnectionProfiles.Contains(ConnectionProfile.WiFi);
            if (!onWifi)
            {
                bool proceed = await DisplayAlert("Not on wifi",
                    "This is a large download (about 3 GB) and you're not on wifi. Downloading over mobile data may use up your data plan. Download anyway?",
                    "Download", "Wait for wifi");
                if (!proceed) return false;
            }

            // 3) Notification permission (Android 13+), so download progress can show.
            try { await Permissions.RequestAsync<Permissions.PostNotifications>(); } catch { }

            return true;
        }

        private void OnPauseResumeClicked(object sender, EventArgs e)
        {
            if (ModelManager.State == DownloadState.Downloading)
                ModelManager.Pause();
            else
                ModelManager.Start();
            RefreshDownloadUi();
        }

        private async void OnStopClicked(object sender, EventArgs e)
        {
            bool sure = await DisplayAlert(
                "Stop download?",
                "This deletes what has downloaded so far, and you would start over next time.",
                "Stop", "Keep downloading");
            if (!sure) return;
            ModelManager.Stop();
            RefreshDownloadUi();
        }

        private void StartChat()
        {
            if (_chatStarted) return;
            _chatStarted = true;
            StopPackRecheck();
            DownloadOverlay.IsVisible = false;
            ChatScroll.IsVisible = true;
            InputArea.IsVisible = true;
            LoadHistory();
            InitModel();
        }

        // ---------- MODEL INIT ----------

        private async void InitModel()
        {
#if ANDROID
            try
            {
                if (!Com.Novasaur.NovaSaurModule.IsReady)
                {
                    SendButton.IsEnabled = false;
                    var status = AddStatus("NovaSaur is waking up... (first time takes a moment)");
                    var ctx = Android.App.Application.Context;
                    await Task.Run(() => Com.Novasaur.NovaSaurModule.Init(ctx));
                    RemoveBubble(status);
                }
                SendButton.IsEnabled = true;
                if (_messages.Count == 0)
                    AddNovaBubble(WelcomeMessage);
                ShowSuggestions();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("NovaSaur init error: " + ex);
                AddNovaBubble("Sorry, I couldn't start up. Your device may not have enough free memory to run NovaSaur. Try closing other apps and reopening this page.");
                SendButton.IsEnabled = false;
                SuggestionScroll.IsVisible = false;
            }
#else
            if (_messages.Count == 0)
                AddNovaBubble("NovaSaur runs on Android only right now.");
            SendButton.IsEnabled = false;
            SuggestionScroll.IsVisible = false;
#endif
        }

        // ---------- ASK ----------

        private void OnSendClicked(object sender, EventArgs e)
        {
            if (_busy)
            {
                // The Send button doubles as a Stop button while answering.
                StopGeneration();
                return;
            }

            string question = (QuestionEntry.Text ?? "").Trim();
            if (string.IsNullOrEmpty(question)) return;

            AddUserBubble(question);
            QuestionEntry.Text = "";
            Answer(question);
        }

        // Stop while thinking = drop the answer. Stop while the answer is
        // typing out = show the rest of it instantly.
        private void StopGeneration()
        {
            if (_revealActive && _revealLabel != null)
            {
                FinishRevealNow();
                return;
            }

            _gen++; // any in-flight result gets discarded
            if (_thinkingStatus != null) { RemoveBubble(_thinkingStatus); _thinkingStatus = null; }
            _busy = false;
            SendButton.Text = "Send";
        }

        private async void Answer(string question)
        {
#if ANDROID
            _busy = true;
            SendButton.Text = "Stop";
            int myGen = ++_gen;

            _thinkingStatus = AddStatus("NovaSaur is thinking...");
            await ScrollToBottom();

            PromptResult pr = null;
            try
            {
                pr = RagService.BuildPrompt(question, _messages, _lastEntities);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("NovaSaur prompt error: " + ex);
            }

            if (pr == null)
            {
                FinishThinking(myGen, "Something went wrong answering that. Please try again.");
                return;
            }

            // Remember what this question was about, for follow-ups.
            if (pr.MatchedEntities.Count > 0)
                _lastEntities = new List<string>(pr.MatchedEntities);

            // Smalltalk, safety redirects, and off-topic refusals come back
            // instantly without waking the model.
            if (pr.InstantReply != null)
            {
                await Task.Delay(350);
                FinishThinking(myGen, pr.InstantReply);
                return;
            }

            string answer = null;
            bool timedOut = false;
            try
            {
                string prompt = pr.Prompt;

                // The lock lives inside the task, so an abandoned (timed out)
                // call keeps holding it until the model actually finishes.
                // That stops two inferences from ever overlapping.
                var askTask = Task.Run(async () =>
                {
                    await _aiLock.WaitAsync();
                    try { return Com.Novasaur.NovaSaurModule.Ask(prompt); }
                    finally { _aiLock.Release(); }
                });

                var finished = await Task.WhenAny(askTask, Task.Delay(AnswerTimeout));
                if (finished == askTask)
                {
                    string raw = await askTask;
                    if (raw != null && raw.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
                        System.Diagnostics.Debug.WriteLine("NovaSaur bridge error: " + raw);
                    else
                        answer = RagService.CleanAnswer(raw);
                }
                else
                {
                    timedOut = true;
                    _ = askTask.ContinueWith(t => { var _ignored = t.Exception; },
                        TaskContinuationOptions.OnlyOnFaulted);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("NovaSaur ask error: " + ex);
            }

            // A Clear, Stop, or newer question happened while generating.
            if (myGen != _gen) return;

            if (timedOut)
            {
                answer = "That one is taking me too long to think about. Try asking a shorter or simpler question.";
            }
            else if (string.IsNullOrWhiteSpace(answer))
            {
                answer = "Something went wrong answering that. Please try again.";
            }
            else
            {
                // Final safety pass on whatever the model produced.
                string replaced = NovaGuard.CheckAnswer(answer);
                if (replaced != null) answer = replaced;
            }

            FinishThinking(myGen, answer);
#else
            AddNovaBubble("NovaSaur runs on Android only right now.");
#endif
        }

        private void FinishThinking(int myGen, string answer)
        {
            if (myGen != _gen) return;
            if (_thinkingStatus != null) { RemoveBubble(_thinkingStatus); _thinkingStatus = null; }
            RevealAnswer(answer);
        }

        // Types the finished answer out word by word.
        private void RevealAnswer(string text)
        {
            _revealWords = text.Split(' ');
            _revealIndex = 0;
            _revealSb.Clear();
            _revealLabel = StartNovaBubble("");
            _revealActive = true;

            if (_revealTimer == null)
            {
                _revealTimer = Dispatcher.CreateTimer();
                _revealTimer.Interval = TimeSpan.FromMilliseconds(45);
                _revealTimer.Tick += (s, e) => RevealTick();
            }
            _revealTimer.Start();
        }

        private void RevealTick()
        {
            if (_revealWords == null || _revealIndex >= _revealWords.Length)
            {
                CompleteReveal();
                return;
            }

            _revealSb.Append(_revealWords[_revealIndex]);
            if (_revealIndex < _revealWords.Length - 1) _revealSb.Append(' ');
            _revealIndex++;
            if (_revealLabel != null) _revealLabel.Text = _revealSb.ToString();
            _ = ChatScroll.ScrollToAsync(0, ChatStack.Height, false);
        }

        // Skip the typing animation and show the whole answer at once.
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
            if (!string.IsNullOrEmpty(full))
            {
                _messages.Add(new ChatMessage { IsUser = false, Text = full });
                SaveHistory();
            }
            _revealLabel = null;
            _revealWords = null;
            _busy = false;
            SendButton.Text = "Send";
            SendButton.IsEnabled = true;
            ShowSuggestions();
            _ = ScrollToBottom();
        }

        private async void OnBackClicked(object sender, EventArgs e) => await Navigation.PopAsync();

        // Clear works at any moment, even mid-answer.
        private void OnClearClicked(object sender, EventArgs e)
        {
            _gen++;                 // invalidate any in-flight answer
            _revealTimer?.Stop();   // stop a reveal in progress
            _revealActive = false;
            _revealLabel = null;
            _revealWords = null;
            if (_thinkingStatus != null) { RemoveBubble(_thinkingStatus); _thinkingStatus = null; }
            _busy = false;
            SendButton.Text = "Send";
            SendButton.IsEnabled = true;

            _messages.Clear();
            _lastEntities = new List<string>();
            Preferences.Remove(HistoryKey);
            ChatStack.Children.Clear();
            AddNovaBubble(WelcomeMessage);
            ShowSuggestions();
        }

        // ---------- SUGGESTIONS ----------

        private void ShowSuggestions()
        {
            if (SuggestedQuestions.All == null || SuggestedQuestions.All.Count == 0)
            {
                SuggestionScroll.IsVisible = false;
                return;
            }

            SuggestionStack.Children.Clear();
            foreach (var q in SuggestedQuestions.All.OrderBy(_ => Guid.NewGuid()).Take(3))
            {
                var chip = new Button
                {
                    Text = q,
                    FontSize = 13,
                    Padding = new Thickness(12, 6),
                    CornerRadius = 16,
                    HeightRequest = 36,
                    BackgroundColor = Theme.Surface,
                    TextColor = Theme.TextPrimary,
                    BorderColor = Theme.Border,
                    BorderWidth = 1
                };
                chip.Clicked += OnSuggestionClicked;
                SuggestionStack.Children.Add(chip);
            }
            SuggestionScroll.IsVisible = true;
        }

        private void OnSuggestionClicked(object sender, EventArgs e)
        {
            if (_busy) return;
            if (sender is Button b)
            {
                QuestionEntry.Text = b.Text;
                OnSendClicked(b, EventArgs.Empty);
            }
        }

        // ---------- HISTORY ----------

        private void AddUserBubble(string text) => AddMessage(text, true);
        private void AddNovaBubble(string text) => AddMessage(text, false);

        private void AddMessage(string text, bool isUser)
        {
            _messages.Add(new ChatMessage { IsUser = isUser, Text = text });
            ChatStack.Children.Add(BuildBubble(text, isUser));
            SaveHistory();
        }

        private void SaveHistory()
        {
            try
            {
                // Keep the saved chat from growing forever.
                if (_messages.Count > MaxSavedMessages)
                    _messages.RemoveRange(0, _messages.Count - MaxSavedMessages);
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
                foreach (var m in _messages)
                    ChatStack.Children.Add(BuildBubble(m.Text, m.IsUser));
            }
            catch { }
        }

        // ---------- UI HELPERS ----------

        private View BuildBubble(string text, bool isUser)
        {
            var label = new Label
            {
                Text = text,
                TextColor = isUser ? Colors.White : Theme.TextPrimary,
                FontSize = 15,
                LineHeight = 1.35
            };

            var frame = new Frame
            {
                Content = label,
                Padding = new Thickness(12, 10),
                CornerRadius = 16,
                HasShadow = false,
                BackgroundColor = isUser ? Color.FromArgb("#512BD4") : Theme.Surface,
                BorderColor = isUser ? Color.FromArgb("#512BD4") : Theme.Border,
                HorizontalOptions = isUser ? LayoutOptions.End : LayoutOptions.Start,
                MaximumWidthRequest = 320
            };

            if (!isUser) AttachAnswerMenu(frame, label);
            return frame;
        }

        private Label StartNovaBubble(string initial)
        {
            var label = new Label
            {
                Text = initial,
                TextColor = Theme.TextPrimary,
                FontSize = 15,
                LineHeight = 1.35
            };

            var frame = new Frame
            {
                Content = label,
                Padding = new Thickness(12, 10),
                CornerRadius = 16,
                HasShadow = false,
                BackgroundColor = Theme.Surface,
                BorderColor = Theme.Border,
                HorizontalOptions = LayoutOptions.Start,
                MaximumWidthRequest = 320
            };

            AttachAnswerMenu(frame, label);
            ChatStack.Children.Add(frame);
            return label;
        }

        // Tap any NovaSaur bubble to copy or report that answer.
        // (Reporting is part of playing fair with AI answers - if something
        // comes out wrong, it's one tap to tell us.)
        private void AttachAnswerMenu(Frame frame, Label label)
        {
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) => await ShowAnswerMenu(label.Text);
            frame.GestureRecognizers.Add(tap);
        }

        private async Task ShowAnswerMenu(string answerText)
        {
            if (string.IsNullOrWhiteSpace(answerText)) return;

            string choice = await DisplayActionSheet(null, "Cancel", null, "Copy answer", "Report this answer");

            if (choice == "Copy answer")
            {
                try { await Clipboard.Default.SetTextAsync(answerText); } catch { }
            }
            else if (choice == "Report this answer")
            {
                string question = FindQuestionFor(answerText);
                string subject = Uri.EscapeDataString("DinoSpace - Report an AI answer");
                string body = Uri.EscapeDataString(
                    "I want to report this NovaSaur answer.\n\n" +
                    (string.IsNullOrEmpty(question) ? "" : "Question: " + question + "\n") +
                    "Answer: " + answerText + "\n\n" +
                    "What was wrong with it (optional): ");
                try
                {
                    await Launcher.OpenAsync("mailto:dinospace.app@gmail.com?subject=" + subject + "&body=" + body);
                }
                catch
                {
                    await DisplayAlert("No Email App",
                        "No email app was found on this device. You can also report answers from Settings > Send Feedback.",
                        "OK");
                }
            }
        }

        private string FindQuestionFor(string answerText)
        {
            for (int i = _messages.Count - 1; i >= 0; i--)
            {
                if (!_messages[i].IsUser && _messages[i].Text == answerText)
                {
                    for (int j = i - 1; j >= 0; j--)
                        if (_messages[j].IsUser) return _messages[j].Text;
                    return "";
                }
            }
            return "";
        }

        private View AddStatus(string text)
        {
            var label = new Label
            {
                Text = text,
                TextColor = Theme.TextSecondary,
                FontSize = 13,
                FontAttributes = FontAttributes.Italic,
                HorizontalOptions = LayoutOptions.Start
            };
            ChatStack.Children.Add(label);
            return label;
        }

        private void RemoveBubble(View v)
        {
            if (v != null && ChatStack.Children.Contains(v))
                ChatStack.Children.Remove(v);
        }

        private async Task ScrollToBottom()
        {
            await Task.Delay(50);
            await ChatScroll.ScrollToAsync(0, ChatStack.Height, true);
        }
    }

    public class ChatMessage
    {
        public bool IsUser { get; set; }
        public string Text { get; set; } = "";
    }
}
