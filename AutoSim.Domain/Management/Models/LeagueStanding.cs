namespace AutoSim.Domain.Management.Models
{
    /// <summary>
    /// Represents a team's regular-season standing within a league.
    /// </summary>
    public sealed record LeagueStanding
    {
        /// <summary>
        /// Gets the division match losses.
        /// </summary>
        public int DivisionLosses { get; init; }

        /// <summary>
        /// Gets the division match wins.
        /// </summary>
        public int DivisionWins { get; init; }

        /// <summary>
        /// Gets the match losses.
        /// </summary>
        public int MatchLosses { get; init; }

        /// <summary>
        /// Gets the match wins.
        /// </summary>
        public int MatchWins { get; init; }

        /// <summary>
        /// Gets regular-season points.
        /// </summary>
        public int Points { get; init; }

        /// <summary>
        /// Gets the round losses.
        /// </summary>
        public int RoundLosses { get; init; }

        /// <summary>
        /// Gets the round wins.
        /// </summary>
        public int RoundWins { get; init; }

        /// <summary>
        /// Gets the team identifier.
        /// </summary>
        public required string TeamId { get; init; }
    }
}
