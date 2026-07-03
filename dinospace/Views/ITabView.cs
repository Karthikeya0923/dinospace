namespace dinospace.Views
{
    // A tab in the RootPage host. OnSelected fires each time the tab becomes
    // visible so it can refresh streaks, featured items, and progress.
    public interface ITabView
    {
        void OnSelected();
    }
}
