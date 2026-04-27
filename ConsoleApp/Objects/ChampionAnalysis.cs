using AutoSim.Domain.Enums;

namespace ConsoleApp.Objects
{
    /// <summary>
    /// Contains aggregate metrics for one champion in one round.
    /// </summary>
    public sealed record ChampionAnalysis
    {
        /// <summary>
        /// Gets the champion identifier.
        /// </summary>
        public required string ChampionId { get; init; }

        /// <summary>
        /// Gets the champion display name.
        /// </summary>
        public required string ChampionName { get; init; }

        /// <summary>
        /// Gets the team side.
        /// </summary>
        public required TeamSide TeamSide { get; init; }

        /// <summary>
        /// Gets champion kills.
        /// </summary>
        public int Kills { get; init; }

        /// <summary>
        /// Gets champion deaths.
        /// </summary>
        public int Deaths { get; init; }

        /// <summary>
        /// Gets champion damage dealt.
        /// </summary>
        public int DamageDealt { get; init; }

        /// <summary>
        /// Gets champion healing done.
        /// </summary>
        public int HealingDone { get; init; }

        /// <summary>
        /// Gets champion shielding done.
        /// </summary>
        public int ShieldingDone { get; init; }

        /// <summary>
        /// Gets champion retreat count.
        /// </summary>
        public int Retreats { get; init; }

        /// <summary>
        /// Gets champion escape count.
        /// </summary>
        public int Escapes { get; init; }

        /// <summary>
        /// Gets champion respawn count.
        /// </summary>
        public int Respawns { get; init; }

        /// <summary>
        /// Gets champion level-up count.
        /// </summary>
        public int LevelsGained { get; init; }
    }
}
