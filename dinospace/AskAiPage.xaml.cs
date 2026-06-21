using System.Text;
using System.Text.Json;
using System.Linq;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;

namespace dinospace
{
    public partial class AskAiPage : ContentPage
    {
        private const string HistoryKey = "nova_chat_history";
        private bool _chatStarted = false;
        private bool _subscribed = false;
        private List<ChatMessage> _messages = new List<ChatMessage>();

        // streaming state
        private bool _streaming = false;
        private Label _streamLabel;
        private readonly StringBuilder _streamBuf = new StringBuilder();
#if ANDROID
        private StreamCallback _callback;
#endif

        public AskAiPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (ModelManager.IsModelDownloaded())
            {
                StartChat();
                return;
            }

            ShowDownloadState();
            Subscribe();
            RefreshDownloadUi();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Unsubscribe();
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
                    StartChat();
                    break;

                case DownloadState.Downloading:
                    DownloadIntroLabel.IsVisible = false;
                    DownloadButton.IsVisible = false;
                    DownloadProgressArea.IsVisible = true;
                    DownloadProgress.Progress = ModelManager.Progress;
                    DownloadStatus.Text = $"Downloading NovaSaur... {(int)(ModelManager.Progress * 100)}%";
                    PauseResumeButton.Text = "Pause";
                    break;

                case DownloadState.Paused:
                    DownloadIntroLabel.IsVisible = false;
                    DownloadButton.IsVisible = false;
                    DownloadProgressArea.IsVisible = true;
                    DownloadProgress.Progress = ModelManager.Progress;
                    DownloadStatus.Text = $"Paused at {(int)(ModelManager.Progress * 100)}%";
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

                default: // NotStarted
                    DownloadIntroLabel.IsVisible = true;
                    DownloadIntroLabel.Text = "An offline AI that answers your dinosaur and space questions, right on your phone. It needs a one-time download of about 3 GB, so use wifi if you can.";
                    DownloadButton.IsVisible = true;
                    DownloadButton.IsEnabled = true;
                    DownloadButton.Text = ModelManager.HasPartialDownload() ? "Resume download" : "Download NovaSaur";
                    DownloadProgressArea.IsVisible = false;
                    break;
            }
        }

        private void OnDownloadClicked(object sender, EventArgs e)
        {
            ModelManager.Start();
            RefreshDownloadUi();
        }

        private void OnPauseResumeClicked(object sender, EventArgs e)
        {
            if (ModelManager.State == DownloadState.Downloading)
                ModelManager.Pause();
            else
                ModelManager.Start();   // resume from where it left off
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

#if ANDROID
        private void RequestNotificationPermission()
        {
            try
            {
                if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Tiramisu) return;
                var activity = Platform.CurrentActivity;
                if (activity == null) return;
                if (activity.CheckSelfPermission("android.permission.POST_NOTIFICATIONS") != Android.Content.PM.Permission.Granted)
                    activity.RequestPermissions(new[] { "android.permission.POST_NOTIFICATIONS" }, 1001);
            }
            catch { }
        }
#else
        private void RequestNotificationPermission() { }
#endif

        private void StartChat()
        {
            if (_chatStarted) return;
            _chatStarted = true;
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
                    AddNovaBubble("Hi, I'm NovaSaur. Ask me anything about dinosaurs or space.");
                ShowSuggestions();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("NovaSaur init error: " + ex);
                AddNovaBubble("Sorry, I couldn't start up. Your device may not have enough free memory to run NovaSaur. Try closing other apps and reopening this page.");
                SendButton.IsEnabled = false;
            }
#else
            if (_messages.Count == 0)
                AddNovaBubble("NovaSaur runs on Android only right now.");
            SendButton.IsEnabled = false;
#endif
        }

        // ---------- SEND / STREAM ----------

        private void OnSendClicked(object sender, EventArgs e)
        {
            if (_streaming) return;

            string question = (QuestionEntry.Text ?? "").Trim();
            if (string.IsNullOrEmpty(question)) return;

            AddUserBubble(question);
            QuestionEntry.Text = "";
            StartStreaming(question);
        }

        private async void StartStreaming(string question)
        {
#if ANDROID
            _streaming = true;
            _streamBuf.Clear();
            SendButton.IsEnabled = false;

            _streamLabel = StartNovaBubble("...");
            await ScrollToBottom();

            _callback = new StreamCallback
            {
                Token = t => MainThread.BeginInvokeOnMainThread(() => OnToken(t)),
                Done = () => MainThread.BeginInvokeOnMainThread(OnStreamDone),
                Failed = msg => MainThread.BeginInvokeOnMainThread(() => OnStreamError(msg))
            };

            string prompt = RagService.BuildPrompt(question);
            try
            {
                await Task.Run(() => Com.Novasaur.NovaSaurModule.AskStream(prompt, _callback));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("NovaSaur stream error: " + ex);
                OnStreamError("Something went wrong answering that. Please try again.");
            }
#else
            AddNovaBubble("NovaSaur runs on Android only right now.");
#endif
        }

        private void OnToken(string token)
        {
            _streamBuf.Append(token);
            if (_streamLabel != null) _streamLabel.Text = _streamBuf.ToString();
            _ = ChatScroll.ScrollToAsync(0, ChatStack.Height, false);
        }

        private void OnStreamDone()
        {
            string final = RagService.CleanAnswer(_streamBuf.ToString());
            if (string.IsNullOrWhiteSpace(final))
                final = "Hmm, I didn't catch that. Try asking another way.";
            if (_streamLabel != null) _streamLabel.Text = final;

            _messages.Add(new ChatMessage { IsUser = false, Text = final });
            SaveHistory();

            _streaming = false;
            _streamLabel = null;
            SendButton.IsEnabled = true;
            ShowSuggestions();
            _ = ScrollToBottom();
        }

        private void OnStreamError(string msg)
        {
            string partial = RagService.CleanAnswer(_streamBuf.ToString());
            string text = string.IsNullOrWhiteSpace(partial) ? msg : partial;
            if (_streamLabel != null) _streamLabel.Text = text;

            if (!string.IsNullOrWhiteSpace(text))
            {
                _messages.Add(new ChatMessage { IsUser = false, Text = text });
                SaveHistory();
            }

            _streaming = false;
            _streamLabel = null;
            SendButton.IsEnabled = true;
            ShowSuggestions();
        }

        private async void OnBackClicked(object sender, EventArgs e) => await Navigation.PopAsync();

        private void OnClearClicked(object sender, EventArgs e)
        {
            if (_streaming) return;
            _messages.Clear();
            Preferences.Remove(HistoryKey);
            ChatStack.Children.Clear();
            AddNovaBubble("Hi, I'm NovaSaur. Ask me anything about dinosaurs or space.");
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
            if (_streaming) return;
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
            try { Preferences.Set(HistoryKey, JsonSerializer.Serialize(_messages)); }
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

            return new Frame
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

            ChatStack.Children.Add(frame);
            return label;
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

#if ANDROID
    class StreamCallback : Java.Lang.Object, Com.Novasaur.IStreamCallback
    {
        public Action<string> Token;
        public Action Done;
        public Action<string> Failed;

        public void OnToken(string token) => Token?.Invoke(token);
        public void OnDone() => Done?.Invoke();
        public void OnError(string error) => Failed?.Invoke(error);
    }
#endif
}