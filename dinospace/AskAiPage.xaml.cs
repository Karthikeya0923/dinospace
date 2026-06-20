using System.Text.Json;
using System.Linq;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;

namespace dinospace
{
    public partial class AskAiPage : ContentPage
    {
        private const string HistoryKey = "nova_chat_history";
        private bool _busy = false;
        private bool _chatStarted = false;
        private bool _subscribed = false;
        private List<ChatMessage> _messages = new List<ChatMessage>();

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
            Unsubscribe();   // the download itself keeps running in ModelManager
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
                    break;

                case DownloadState.Failed:
                    DownloadIntroLabel.IsVisible = true;
                    DownloadIntroLabel.Text = "The download stopped. Tap retry and it picks up where it left off.";
                    DownloadButton.IsVisible = true;
                    DownloadButton.IsEnabled = true;
                    DownloadButton.Text = "Retry download";
                    DownloadProgressArea.IsVisible = false;
                    break;

                default: // NotStarted
                    DownloadIntroLabel.IsVisible = true;
                    DownloadButton.IsVisible = true;
                    DownloadButton.IsEnabled = true;
                    DownloadButton.Text = "Download NovaSaur";
                    DownloadProgressArea.IsVisible = false;
                    break;
            }
        }

        private void OnDownloadClicked(object sender, EventArgs e)
        {
            ModelManager.Start();
            RefreshDownloadUi();
        }

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

        // ---------- MODEL ----------

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

        private async void OnSendClicked(object sender, EventArgs e)
        {
            if (_busy) return;
            string question = (QuestionEntry.Text ?? "").Trim();
            if (string.IsNullOrEmpty(question)) return;

            _busy = true;
            SendButton.IsEnabled = false;

            AddUserBubble(question);
            QuestionEntry.Text = "";
            await ScrollToBottom();

            var thinking = AddStatus("NovaSaur is thinking...");
            await ScrollToBottom();

#if ANDROID
            try
            {
                string raw = await Task.Run(() => Com.Novasaur.NovaSaurModule.Ask(RagService.BuildPrompt(question)));
                string answer = RagService.CleanAnswer(raw);
                RemoveBubble(thinking);
                AddNovaBubble(string.IsNullOrWhiteSpace(answer)
                    ? "Hmm, I didn't catch that. Try asking another way."
                    : answer);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("NovaSaur ask error: " + ex);
                RemoveBubble(thinking);
                AddNovaBubble("Something went wrong answering that. Please try again.");
            }
#else
            RemoveBubble(thinking);
#endif
            await ScrollToBottom();
            _busy = false;
            SendButton.IsEnabled = true;
            ShowSuggestions();
        }

        private async void OnBackClicked(object sender, EventArgs e) => await Navigation.PopAsync();

        private void OnClearClicked(object sender, EventArgs e)
        {
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