using AutoSim.Domain.Enums;

namespace ConsoleApp.Objects
{
    /// <summary>
    /// Contains aggregate metrics for one team.
    /// </summary>
    public sealed record TeamAnalysis
    {
        /// <summary>
        /// Gets the team side.
        /// </summary>
        public required TeamSide TeamSide { get; init; }

        /// <summary>
        /// Gets kills credited to the team.
        /// </summary>
        public required int Kills { get; init; }

        /// <summary>
        /// Gets deaths suffered by the team.
        /// </summary>
        public required int Deaths { get; init; }

        /// <summary>
        /// Gets damage dealt by the team.
        /// </summary>
        public required int DamageDealt { get; init; }

        /// <summary>
        /// Gets healing done by the team.
        /// </summary>
        public required int HealingDone { get; init; }

        /// <summary>
        /// Gets shielding done by the team.
        /// </summary>
        public required int ShieldingDone { get; init; }

        /// <summary>
        /// Gets retreats started by the team.
        /// </summary>
        public required int Retreats { get; init; }

        /// <summary>
        /// Gets escapes by the team.
        /// </summary>
        public required int Escapes { get; init; }

        /// <summary>
        /// Gets respawns by the team.
        /// </summary>
        public required int Respawns { get; init; }
    }
}
