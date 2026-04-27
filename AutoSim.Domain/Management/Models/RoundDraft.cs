using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Management.Models
{
    /// <summary>
    /// Represents per-round champion selections for both sides.
    /// </summary>
    public sealed record RoundDraft
    {
        /// <summary>
        /// Gets blue-side champion selections.
        /// </summary>
        public IReadOnlyList<ChampionDefinition> BlueChampions { get; init; } = [];

        /// <summary>
        /// Gets red-side champion selections.
        /// </summary>
        public IReadOnlyList<ChampionDefinition> RedChampions { get; init; } = [];
    }
}
