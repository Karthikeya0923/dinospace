using Microsoft.Maui.Controls;

namespace dinospace.Views
{
    // Hosts the persistent NovaView chat as a pushed page. One shared chat
    // instance keeps the conversation (and the loaded model) alive across
    // visits; each push gets a fresh page wrapper around it.
    public class NovaPage : ContentPage
    {
        private static NovaView? _sharedChat;
        private static NovaView Chat => _sharedChat ??= new NovaView();

        private readonly Grid _root;

        public NovaPage()
        {
            BackgroundColor = Theme.Bg;
            _root = new Grid();
            Content = _root;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Steal the shared chat from wherever it last lived, then wake it.
            if (Chat.Parent is Grid old && old != _root)
                old.Children.Remove(Chat);
            if (!_root.Children.Contains(Chat))
                _root.Add(Chat);
            Chat.OnSelected();
        }
    }
}
