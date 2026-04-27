using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Management.Models
{
    /// <summary>
    /// Represents a validated round setup passed to the RoundEngine.
    /// </summary>
    public sealed record RoundSetup
    {
        /// <summary>
        /// Gets blue-side champion selections.
        /// </summary>
        public IReadOnlyList<ChampionDefinition> BlueChampions { get; init; } = [];

        /// <summary>
        /// Gets the blue team identifier.
        /// </summary>
        public required string BlueTeamId { get; init; }

        /// <summary>
        /// Gets red-side champion selections.
        /// </summary>
        public IReadOnlyList<ChampionDefinition> RedChampions { get; init; } = [];

        /// <summary>
        /// Gets the red team identifier.
        /// </summary>
        public required string RedTeamId { get; init; }

        /// <summary>
        /// Gets the round number.
        /// </summary>
        public int RoundNumber { get; init; }

        /// <summary>
        /// Gets the deterministic round seed.
        /// </summary>
        public int Seed { get; init; }
    }
}
