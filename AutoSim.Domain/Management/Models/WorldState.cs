namespace AutoSim.Domain.Management.Models
{
    /// <summary>
    /// Represents the complete Management Layer world state.
    /// </summary>
    public sealed record WorldState
    {
        /// <summary>
        /// Gets all coaches.
        /// </summary>
        public IReadOnlyList<Coach> Coaches { get; init; } = [];

        /// <summary>
        /// Gets the human coach identifier.
        /// </summary>
        public required string HumanCoachId { get; init; }

        /// <summary>
        /// Gets all players.
        /// </summary>
        public IReadOnlyList<Player> Players { get; init; } = [];

        /// <summary>
        /// Gets the deterministic generation seed.
        /// </summary>
        public int Seed { get; init; }

        /// <summary>
        /// Gets the active season state.
        /// </summary>
        public required SeasonState Season { get; init; }

        /// <summary>
        /// Gets all competitive tiers.
        /// </summary>
        public IReadOnlyList<CompetitiveTier> Tiers { get; init; } = [];

        /// <summary>
        /// Gets world championship history.
        /// </summary>
        public WorldChampionshipHistory WorldChampionshipHistory { get; init; } = new();
    }
}
