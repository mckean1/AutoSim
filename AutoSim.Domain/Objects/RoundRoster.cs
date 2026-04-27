namespace AutoSim.Domain.Objects
{
    /// <summary>
    /// Defines the selected champions participating in one round.
    /// </summary>
    public sealed record RoundRoster
    {
        /// <summary>
        /// Gets the selected blue team champions.
        /// </summary>
        public required IReadOnlyList<ChampionDefinition> BlueChampions { get; init; }

        /// <summary>
        /// Gets the selected red team champions.
        /// </summary>
        public required IReadOnlyList<ChampionDefinition> RedChampions { get; init; }
    }
}
