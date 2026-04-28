namespace ConsoleApp.Presentation
{
    /// <summary>
    /// Represents one player-facing replay message.
    /// </summary>
    public sealed record ReplayMessage
    {
        /// <summary>
        /// Gets the replay message category.
        /// </summary>
        public required ReplayMessageCategory Category { get; init; }

        /// <summary>
        /// Gets the replay message severity.
        /// </summary>
        public required ReplayMessageSeverity Severity { get; init; }

        /// <summary>
        /// Gets the message timestamp.
        /// </summary>
        public required TimeSpan Timestamp { get; init; }

        /// <summary>
        /// Gets the readable message text.
        /// </summary>
        public required string Text { get; init; }
    }
}
