using AutoSim.Domain.Enums;

namespace ConsoleApp.Objects
{
    /// <summary>
    /// Contains average team metrics across analyzed rounds.
    /// </summary>
    public sealed record TeamAggregateAnalysis
    {
        public required TeamSide TeamSide { get; init; }
        public required double AverageKills { get; init; }
        public required double AverageDeaths { get; init; }
        public required double AverageDamageDealt { get; init; }
        public required double AverageHealingDone { get; init; }
        public required double AverageShieldingDone { get; init; }
        public required double AverageRetreats { get; init; }
        public required double AverageEscapes { get; init; }
        public required double AverageRespawns { get; init; }
    }
}
