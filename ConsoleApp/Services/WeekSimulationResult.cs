using AutoSim.Domain.Management.Models;
using ConsoleApp.Presentation;

namespace ConsoleApp.Services
{
    /// <summary>
    /// Represents the result of a completed week simulation.
    /// </summary>
    public sealed class WeekSimulationResult
    {
        /// <summary>
        /// Gets or sets the updated world state.
        /// </summary>
        public required WorldState World { get; init; }

        /// <summary>
        /// Gets or sets the resolved week number.
        /// </summary>
        public required int ResolvedWeek { get; init; }

        /// <summary>
        /// Gets or sets the human team match result.
        /// </summary>
        public MatchResult? HumanMatchResult { get; init; }

        /// <summary>
        /// Gets or sets the presented human match for replay.
        /// </summary>
        public PresentedMatch? PresentedMatch { get; init; }

        /// <summary>
        /// Gets or sets the scheduled match for the human team.
        /// </summary>
        public ScheduledMatch? ScheduledMatch { get; init; }

        /// <summary>
        /// Gets or sets all match results from the week.
        /// </summary>
        public required IReadOnlyList<MatchResult> AllMatchResults { get; init; }
    }
}
