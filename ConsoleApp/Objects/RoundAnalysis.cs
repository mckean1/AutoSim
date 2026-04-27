using AutoSim.Domain.Enums;

namespace ConsoleApp.Objects
{
    /// <summary>
    /// Contains aggregate metrics computed from one round log.
    /// </summary>
    public sealed record RoundAnalysis
    {
        /// <summary>
        /// Gets the total number of parsed events.
        /// </summary>
        public required int TotalEvents { get; init; }

        /// <summary>
        /// Gets the round duration in seconds.
        /// </summary>
        public required double DurationSeconds { get; init; }

        /// <summary>
        /// Gets the winning team side when known.
        /// </summary>
        public required TeamSide? Winner { get; init; }

        /// <summary>
        /// Gets the blue team kill count.
        /// </summary>
        public required int BlueKills { get; init; }

        /// <summary>
        /// Gets the red team kill count.
        /// </summary>
        public required int RedKills { get; init; }

        /// <summary>
        /// Gets aggregate fight metrics.
        /// </summary>
        public required FightAnalysis FightSummary { get; init; }

        /// <summary>
        /// Gets blue team aggregate metrics.
        /// </summary>
        public required TeamAnalysis BlueTeam { get; init; }

        /// <summary>
        /// Gets red team aggregate metrics.
        /// </summary>
        public required TeamAnalysis RedTeam { get; init; }

        /// <summary>
        /// Gets champion aggregate metrics.
        /// </summary>
        public required IReadOnlyList<ChampionAnalysis> Champions { get; init; }

        /// <summary>
        /// Gets concise notable findings.
        /// </summary>
        public required IReadOnlyList<string> NotableEvents { get; init; }
    }
}
