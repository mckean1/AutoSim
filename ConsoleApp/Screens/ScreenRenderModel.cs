namespace ConsoleApp.Screens
{
    /// <summary>
    /// Defines the data needed to render a full console screen.
    /// </summary>
    internal sealed record ScreenRenderModel
    {
        /// <summary>
        /// Gets the available commands for the screen.
        /// </summary>
        public IReadOnlyList<string> Commands { get; init; } = [];

        /// <summary>
        /// Gets the screen content lines.
        /// </summary>
        public IReadOnlyList<string> ContentLines { get; init; } = [];

        /// <summary>
        /// Gets the shared screen header.
        /// </summary>
        public ScreenHeaderModel Header { get; init; } = new();

        /// <summary>
        /// Gets the transient message shown above the content.
        /// </summary>
        public string? Message { get; init; }

        /// <summary>
        /// Gets the screen title.
        /// </summary>
        public string Title { get; init; } = "Home";
    }
}
