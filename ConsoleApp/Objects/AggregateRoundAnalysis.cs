using AutoSim.Domain.Enums;

namespace ConsoleApp.Objects
{
    /// <summary>
    /// Contains aggregate metrics across many analyzed round logs.
    /// </summary>
    public sealed record AggregateRoundAnalysis
    {
        public required int TotalLogsFound { get; init; }
        public required int RoundsAnalyzed { get; init; }
        public required int LogsSkipped { get; init; }
        public required int BlueWins { get; init; }
        public required int RedWins { get; init; }
        public required int UnknownWinners { get; init; }
        public required double AverageBlueKills { get; init; }
        public required double AverageRedKills { get; init; }
        public required double AverageRoundDurationSeconds { get; init; }
        public required double AverageTotalEvents { get; init; }
        public required double AverageFightsPerRound { get; init; }
        public required double AverageFightDurationSeconds { get; init; }
        public required double LongestFightSeconds { get; init; }
        public required double AverageFightsEndedByRoundEnd { get; init; }
        public required IReadOnlyDictionary<Lane, double> AverageFightsByLane { get; init; }
        public required IReadOnlyDictionary<Lane, int> TotalFightsByLane { get; init; }
        public required TeamAggregateAnalysis BlueTeam { get; init; }
        public required TeamAggregateAnalysis RedTeam { get; init; }
        public required IReadOnlyList<ChampionAggregateAnalysis> Champions { get; init; }
        public required IReadOnlyList<string> NotableFindings { get; init; }
        public required IReadOnlyList<string> SkippedLogs { get; init; }
    }
}
