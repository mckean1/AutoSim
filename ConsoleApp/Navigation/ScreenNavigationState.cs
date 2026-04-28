namespace ConsoleApp.Navigation
{
    /// <summary>
    /// Tracks the currently selected console management screen.
    /// </summary>
    internal sealed class ScreenNavigationState
    {
        /// <summary>
        /// Gets or sets the current screen.
        /// </summary>
        public ScreenKind CurrentScreen { get; set; } = ScreenKind.Home;
    }
}
