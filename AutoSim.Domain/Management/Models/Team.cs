namespace AutoSim.Domain.Management.Models
{
    /// <summary>
    /// Represents a team organization, including its coach and contracted players.
    /// </summary>
    public sealed record Team
    {
        /// <summary>
        /// Gets the coach identifier.
        /// </summary>
        public required string CoachId { get; init; }

        /// <summary>
        /// Gets the division identifier.
        /// </summary>
        public required string DivisionId { get; init; }

        /// <summary>
        /// Gets the team identifier.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Gets the league identifier.
        /// </summary>
        public required string LeagueId { get; init; }

        /// <summary>
        /// Gets the team display name.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Gets contracted player identifiers.
        /// </summary>
        public IReadOnlyList<string> PlayerIds { get; init; } = [];
    }
}
