namespace AutoSim.Domain.Management.Models
{
    /// <summary>
    /// Represents the high-level result of a scheduled match.
    /// </summary>
    public sealed record MatchResult
    {
        /// <summary>
        /// Gets the best-of round count.
        /// </summary>
        public int BestOf { get; init; }

        /// <summary>
        /// Gets the blue team identifier.
        /// </summary>
        public required string BlueTeamId { get; init; }

        /// <summary>
        /// Gets the blue team round wins.
        /// </summary>
        public int BlueRoundWins { get; init; }

        /// <summary>
        /// Gets the losing team identifier.
        /// </summary>
        public required string LosingTeamId { get; init; }

        /// <summary>
        /// Gets the match type.
        /// </summary>
        public AutoSim.Domain.Enums.MatchType MatchType { get; init; }

        /// <summary>
        /// Gets the red team identifier.
        /// </summary>
        public required string RedTeamId { get; init; }

        /// <summary>
        /// Gets the red team round wins.
        /// </summary>
        public int RedRoundWins { get; init; }

        /// <summary>
        /// Gets the completed round results.
        /// </summary>
        public IReadOnlyList<RoundResult> RoundResults { get; init; } = [];

        /// <summary>
        /// Gets the scheduled match identifier.
        /// </summary>
        public required string MatchId { get; init; }

        /// <summary>
        /// Gets the scheduled match identifier.
        /// </summary>
        public string ScheduledMatchId => MatchId;

        /// <summary>
        /// Gets the winning team identifier.
        /// </summary>
        public required string WinningTeamId { get; init; }

        /// <summary>
        /// Gets the away team round wins for scheduled-match compatibility.
        /// </summary>
        public int AwayRoundWins => RedRoundWins;

        /// <summary>
        /// Gets the home team round wins for scheduled-match compatibility.
        /// </summary>
        public int HomeRoundWins => BlueRoundWins;
    }
}
