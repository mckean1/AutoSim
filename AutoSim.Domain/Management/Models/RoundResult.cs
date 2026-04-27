namespace AutoSim.Domain.Management.Models
{
    /// <summary>
    /// Represents a Management Layer round result inside a match.
    /// </summary>
    public sealed record RoundResult
    {
        /// <summary>
        /// Gets the blue champion identifiers selected for this round.
        /// </summary>
        public IReadOnlyList<string> BlueChampionIds { get; init; } = [];

        /// <summary>
        /// Gets the blue team identifier.
        /// </summary>
        public required string BlueTeamId { get; init; }

        /// <summary>
        /// Gets the losing team identifier.
        /// </summary>
        public required string LosingTeamId { get; init; }

        /// <summary>
        /// Gets the red champion identifiers selected for this round.
        /// </summary>
        public IReadOnlyList<string> RedChampionIds { get; init; } = [];

        /// <summary>
        /// Gets the red team identifier.
        /// </summary>
        public required string RedTeamId { get; init; }

        /// <summary>
        /// Gets the round number inside the match.
        /// </summary>
        public int RoundNumber { get; init; }

        /// <summary>
        /// Gets a compact round summary.
        /// </summary>
        public string Summary { get; init; } = string.Empty;

        /// <summary>
        /// Gets the winning team identifier.
        /// </summary>
        public required string WinningTeamId { get; init; }
    }
}
