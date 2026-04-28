namespace ConsoleApp.Presentation
{
    /// <summary>
    /// Represents a completed match prepared for later review.
    /// </summary>
    public sealed record MatchReview
    {
        /// <summary>
        /// Gets the best-of label.
        /// </summary>
        public required string BestOfLabel { get; init; }

        /// <summary>
        /// Gets blue-side match round wins.
        /// </summary>
        public required int BlueRoundWins { get; init; }

        /// <summary>
        /// Gets the blue team display name.
        /// </summary>
        public required string BlueTeamName { get; init; }

        /// <summary>
        /// Gets the match messages.
        /// </summary>
        public IReadOnlyList<ReplayMessage> MatchMessages { get; init; } = [];

        /// <summary>
        /// Gets the stable match review identifier.
        /// </summary>
        public required Guid MatchId { get; init; }

        /// <summary>
        /// Gets the match type display text.
        /// </summary>
        public required string MatchType { get; init; }

        /// <summary>
        /// Gets red-side match round wins.
        /// </summary>
        public required int RedRoundWins { get; init; }

        /// <summary>
        /// Gets the red team display name.
        /// </summary>
        public required string RedTeamName { get; init; }

        /// <summary>
        /// Gets reviewed rounds.
        /// </summary>
        public IReadOnlyList<RoundReview> Rounds { get; init; } = [];

        /// <summary>
        /// Gets the week number.
        /// </summary>
        public required int WeekNumber { get; init; }

        /// <summary>
        /// Gets the winner display name.
        /// </summary>
        public required string WinnerTeamName { get; init; }
    }
}
