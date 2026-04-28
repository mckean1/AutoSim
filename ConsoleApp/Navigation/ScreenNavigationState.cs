namespace ConsoleApp.Navigation
{
    /// <summary>
    /// Tracks the currently selected console management screen.
    /// </summary>
    internal sealed class ScreenNavigationState
    {
        private readonly Stack<ScreenKind> _history = [];

        /// <summary>
        /// Gets or sets the current screen.
        /// </summary>
        public ScreenKind CurrentScreen { get; set; } = ScreenKind.Home;

        /// <summary>
        /// Pushes the current screen onto the history stack before navigation.
        /// </summary>
        /// <param name="screen">The destination screen.</param>
        public void NavigateTo(ScreenKind screen)
        {
            if (CurrentScreen != screen)
            {
                _history.Push(CurrentScreen);
            }

            CurrentScreen = screen;
        }

        /// <summary>
        /// Clears navigation history and sets the current screen.
        /// </summary>
        /// <param name="screen">The destination screen.</param>
        public void ResetTo(ScreenKind screen)
        {
            _history.Clear();
            CurrentScreen = screen;
        }

        /// <summary>
        /// Attempts to go back to the previous screen.
        /// </summary>
        /// <param name="screen">The resolved previous screen.</param>
        /// <returns><see langword="true"/> when a previous screen exists.</returns>
        public bool TryGoBack(out ScreenKind screen)
        {
            if (_history.Count == 0)
            {
                screen = CurrentScreen;
                return false;
            }

            screen = _history.Pop();
            CurrentScreen = screen;
            return true;
        }
    }
}
