namespace AutoSim.Domain.Management.Models
{
    /// <summary>
    /// Represents one completed world championship.
    /// </summary>
    public sealed record WorldChampionshipRecord
    {
        /// <summary>
        /// Gets the runner-up team identifier.
        /// </summary>
        public string? RunnerUpTeamId { get; init; }

        /// <summary>
        /// Gets the season year.
        /// </summary>
        public int Year { get; init; }

        /// <summary>
        /// Gets the champion team identifier.
        /// </summary>
        public required string ChampionTeamId { get; init; }
    }
}
