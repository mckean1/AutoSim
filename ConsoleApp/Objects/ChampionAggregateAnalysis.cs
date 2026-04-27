namespace ConsoleApp.Objects
{
    /// <summary>
    /// Contains average champion metrics across analyzed rounds.
    /// </summary>
    public sealed record ChampionAggregateAnalysis
    {
        public required string ChampionId { get; init; }
        public required string ChampionName { get; init; }
        public required int Games { get; init; }
        public required int Wins { get; init; }
        public required double WinRate { get; init; }
        public required double AverageKills { get; init; }
        public required double AverageDeaths { get; init; }
        public required double AverageDamageDealt { get; init; }
        public required double AverageHealingDone { get; init; }
        public required double AverageShieldingDone { get; init; }
        public required double AverageRetreats { get; init; }
        public required double AverageEscapes { get; init; }
    }
}
