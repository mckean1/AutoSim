using AutoSim.Domain.Enums;

namespace AutoSim.Domain.Management.Models
{
    /// <summary>
    /// Represents a division inside a league.
    /// </summary>
    public sealed record Division
    {
        /// <summary>
        /// Gets the division identifier.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Gets the division name.
        /// </summary>
        public required DivisionName Name { get; init; }

        /// <summary>
        /// Gets the team identifiers assigned to this division.
        /// </summary>
        public IReadOnlyList<string> TeamIds { get; init; } = [];
    }
}
