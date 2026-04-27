namespace AutoSim.Domain.Management.Models
{
    /// <summary>
    /// Tracks world championship winners and championship counts.
    /// </summary>
    public sealed record WorldChampionshipHistory
    {
        /// <summary>
        /// Gets the completed championship entries.
        /// </summary>
        public IReadOnlyList<WorldChampionshipRecord> Records { get; init; } = [];

        /// <summary>
        /// Gets the reigning world champion team identifier.
        /// </summary>
        public string? ReigningChampionTeamId { get; init; }

        /// <summary>
        /// Gets championship counts by team identifier.
        /// </summary>
        public IReadOnlyDictionary<string, int> TeamChampionshipCounts { get; init; } =
            new Dictionary<string, int>(StringComparer.Ordinal);
    }
}
