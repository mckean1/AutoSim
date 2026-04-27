namespace AutoSim.Domain.Management.Models
{
    /// <summary>
    /// Represents a match scheduled by the Management Layer.
    /// </summary>
    public sealed record ScheduledMatch
    {
        /// <summary>
        /// Gets the away team identifier.
        /// </summary>
        public required string AwayTeamId { get; init; }

        /// <summary>
        /// Gets the best-of round count.
        /// </summary>
        public int BestOf { get; init; }

        /// <summary>
        /// Gets the home team identifier.
        /// </summary>
        public required string HomeTeamId { get; init; }

        /// <summary>
        /// Gets the match identifier.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Gets the league identifier when the match belongs to a league.
        /// </summary>
        public string? LeagueId { get; init; }

        /// <summary>
        /// Gets the match type.
        /// </summary>
        public AutoSim.Domain.Enums.MatchType MatchType { get; init; }

        /// <summary>
        /// Gets the resolved match result, if played.
        /// </summary>
        public MatchResult? Result { get; init; }

        /// <summary>
        /// Gets the season week.
        /// </summary>
        public int Week { get; init; }
    }
}
