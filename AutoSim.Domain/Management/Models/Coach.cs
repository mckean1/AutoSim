namespace AutoSim.Domain.Management.Models
{
    /// <summary>
    /// Represents a coach who controls team-level direction.
    /// </summary>
    public sealed record Coach
    {
        /// <summary>
        /// Gets the coach identifier.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Gets a value indicating whether this coach is controlled by the human player.
        /// </summary>
        public bool IsHuman { get; init; }

        /// <summary>
        /// Gets the coach display name.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Gets the contracted team identifier.
        /// </summary>
        public string? TeamId { get; init; }
    }
}
