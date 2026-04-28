namespace ConsoleApp.Presentation
{
    /// <summary>
    /// Represents a completed round prepared for later review.
    /// </summary>
    public sealed record RoundReview
    {
        /// <summary>
        /// Gets the blue-side score for the round.
        /// </summary>
        public required int BlueScore { get; init; }

        /// <summary>
        /// Gets available champion stats.
        /// </summary>
        public IReadOnlyList<ChampionRoundReviewStats> ChampionStats { get; init; } = [];

        /// <summary>
        /// Gets the round duration.
        /// </summary>
        public required TimeSpan Duration { get; init; }

        /// <summary>
        /// Gets key round moments.
        /// </summary>
        public IReadOnlyList<string> KeyMoments { get; init; } = [];

        /// <summary>
        /// Gets the red-side score for the round.
        /// </summary>
        public required int RedScore { get; init; }

        /// <summary>
        /// Gets the replay messages for the round.
        /// </summary>
        public IReadOnlyList<ReplayMessage> ReplayMessages { get; init; } = [];

        /// <summary>
        /// Gets the round number.
        /// </summary>
        public required int RoundNumber { get; init; }

        /// <summary>
        /// Gets the round winner display name.
        /// </summary>
        public required string WinnerTeamName { get; init; }
    }
}
