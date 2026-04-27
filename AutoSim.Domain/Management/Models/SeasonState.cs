namespace AutoSim.Domain.Management.Models
{
    /// <summary>
    /// Represents the current season state.
    /// </summary>
    public sealed record SeasonState
    {
        /// <summary>
        /// Gets the current week.
        /// </summary>
        public int CurrentWeek { get; init; } = 1;

        /// <summary>
        /// Gets the season year.
        /// </summary>
        public int Year { get; init; } = 1;

        /// <summary>
        /// Gets globally reserved world championship matches.
        /// </summary>
        public IReadOnlyList<ScheduledMatch> WorldChampionshipSchedule { get; init; } = [];
    }
}
