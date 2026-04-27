namespace AutoSim.Domain.Enums
{
    /// <summary>
    /// Defines meaningful gameplay events emitted during a round.
    /// </summary>
    public enum RoundEventType
    {
        /// <summary>
        /// The round started.
        /// </summary>
        RoundStarted,

        /// <summary>
        /// The round ended.
        /// </summary>
        RoundEnded,

        /// <summary>
        /// A fight started.
        /// </summary>
        FightStarted,

        /// <summary>
        /// A champion joined an existing fight.
        /// </summary>
        FightJoined,

        /// <summary>
        /// A fight ended.
        /// </summary>
        FightEnded,

        /// <summary>
        /// A basic attack resolved.
        /// </summary>
        AttackResolved,

        /// <summary>
        /// An ability cast started.
        /// </summary>
        AbilityCastStarted,

        /// <summary>
        /// An ability resolved.
        /// </summary>
        AbilityResolved,

        /// <summary>
        /// Damage was applied.
        /// </summary>
        DamageDealt,

        /// <summary>
        /// Healing was applied.
        /// </summary>
        HealingDone,

        /// <summary>
        /// A shield was applied.
        /// </summary>
        ShieldApplied,

        /// <summary>
        /// A champion began retreating.
        /// </summary>
        ChampionRetreated,

        /// <summary>
        /// A retreating champion escaped a fight.
        /// </summary>
        ChampionEscaped,

        /// <summary>
        /// A champion was killed.
        /// </summary>
        ChampionKilled,

        /// <summary>
        /// A champion respawned.
        /// </summary>
        ChampionRespawned,

        /// <summary>
        /// A champion leveled up.
        /// </summary>
        ChampionLeveledUp
    }
}
