using AutoSim.Domain.Enums;

namespace ConsoleApp.Objects
{
    /// <summary>
    /// Contains aggregate metrics about fights in one round.
    /// </summary>
    public sealed record FightAnalysis
    {
        /// <summary>
        /// Gets the number of fights that started.
        /// </summary>
        public required int TotalFights { get; init; }

        /// <summary>
        /// Gets the average completed fight duration in seconds.
        /// </summary>
        public required double AverageDurationSeconds { get; init; }

        /// <summary>
        /// Gets the longest completed fight duration in seconds.
        /// </summary>
        public required double LongestFightSeconds { get; init; }

        /// <summary>
        /// Gets the lane for the longest completed fight when known.
        /// </summary>
        public required Lane? LongestFightLane { get; init; }

        /// <summary>
        /// Gets fight start counts by lane.
        /// </summary>
        public required IReadOnlyDictionary<Lane, int> FightsByLane { get; init; }

        /// <summary>
        /// Gets blue fight win count.
        /// </summary>
        public required int BlueFightWins { get; init; }

        /// <summary>
        /// Gets red fight win count.
        /// </summary>
        public required int RedFightWins { get; init; }

        /// <summary>
        /// Gets the number of fights ended because the round ended.
        /// </summary>
        public required int FightsEndedByRoundEnd { get; init; }
    }
}
