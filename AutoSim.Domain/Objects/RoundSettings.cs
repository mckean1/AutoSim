namespace AutoSim.Domain.Objects
{
    /// <summary>
    /// Defines round simulation settings.
    /// </summary>
    public sealed record RoundSettings
    {
        /// <summary>
        /// Gets the maximum round duration in seconds.
        /// </summary>
        public double RoundDurationSeconds { get; init; } = 300.0;

        /// <summary>
        /// Gets the default simulation tick rate in seconds.
        /// </summary>
        public double TickRateSeconds { get; init; } = 0.1;

        /// <summary>
        /// Gets the distance used to create or join fights.
        /// </summary>
        public double EngageRange { get; init; } = 10.0;

        /// <summary>
        /// Gets the distance from a fight midpoint to each backline anchor.
        /// </summary>
        public double BacklineOffset { get; init; } = 5.0;

        /// <summary>
        /// Gets the lane movement speed.
        /// </summary>
        public double LaneMoveSpeed { get; init; } = 3.0;

        /// <summary>
        /// Gets the fight formation movement speed.
        /// </summary>
        public double FightMoveSpeed { get; init; } = 5.0;

        /// <summary>
        /// Gets the retreat movement speed.
        /// </summary>
        public double RetreatMoveSpeed { get; init; } = 6.0;

        /// <summary>
        /// Gets passive health regeneration per second.
        /// </summary>
        public double PassiveHealthRegenPerSecond { get; init; } = 1.0;

        /// <summary>
        /// Gets base healing as a maximum health percent per second.
        /// </summary>
        public double BaseHealPercentPerSecond { get; init; } = 0.10;

        /// <summary>
        /// Gets the distance from base where base healing applies.
        /// </summary>
        public double BaseHealRange { get; init; } = 10.0;

        /// <summary>
        /// Gets the health percentage below which champions retreat.
        /// </summary>
        public double RetreatHealthThreshold { get; init; } = 0.35;

        /// <summary>
        /// Gets the respawn duration in seconds.
        /// </summary>
        public double RespawnDurationSeconds { get; init; } = 10.0;

        /// <summary>
        /// Gets the gold earned per second while farming.
        /// </summary>
        public double FarmGoldPerSecond { get; init; } = 1.0;

        /// <summary>
        /// Gets the experience earned per second while farming.
        /// </summary>
        public double FarmXpPerSecond { get; init; } = 5.0;

        /// <summary>
        /// Gets the experience awarded for a killing blow.
        /// </summary>
        public int KillXp { get; init; } = 50;

        /// <summary>
        /// Gets the experience awarded to living active fight winners.
        /// </summary>
        public int FightWinXp { get; init; } = 25;

        /// <summary>
        /// Gets maximum health gained per level.
        /// </summary>
        public int HealthPerLevel { get; init; } = 10;

        /// <summary>
        /// Gets AttackPower gained per level.
        /// </summary>
        public int PowerPerLevel { get; init; } = 2;

        /// <summary>
        /// Gets the maximum champion level.
        /// </summary>
        public int MaxLevel { get; init; } = 10;
    }
}
