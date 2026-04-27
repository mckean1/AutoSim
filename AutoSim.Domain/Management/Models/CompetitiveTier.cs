using AutoSim.Domain.Enums;

namespace AutoSim.Domain.Management.Models
{
    /// <summary>
    /// Represents a competitive tier containing regional leagues.
    /// </summary>
    public sealed record CompetitiveTier
    {
        /// <summary>
        /// Gets the tier identifier.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Gets the tier name.
        /// </summary>
        public required CompetitiveTierName Name { get; init; }

        /// <summary>
        /// Gets the regional leagues in this tier.
        /// </summary>
        public IReadOnlyList<League> Leagues { get; init; } = [];
    }
}
