using AutoSim.Domain.Enums;

namespace AutoSim.Domain.Management.Models
{
    /// <summary>
    /// Represents a regional league in a competitive tier.
    /// </summary>
    public sealed record League
    {
        /// <summary>
        /// Gets the league identifier.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Gets the divisions in this league.
        /// </summary>
        public IReadOnlyList<Division> Divisions { get; init; } = [];

        /// <summary>
        /// Gets the parent tier name.
        /// </summary>
        public required CompetitiveTierName TierName { get; init; }

        /// <summary>
        /// Gets the league region.
        /// </summary>
        public required LeagueRegion Region { get; init; }

        /// <summary>
        /// Gets the scheduled matches for this league.
        /// </summary>
        public IReadOnlyList<ScheduledMatch> Schedule { get; init; } = [];

        /// <summary>
        /// Gets current league standings.
        /// </summary>
        public IReadOnlyList<LeagueStanding> Standings { get; init; } = [];

        /// <summary>
        /// Gets all teams in this league.
        /// </summary>
        public IReadOnlyList<Team> Teams { get; init; } = [];
    }
}
