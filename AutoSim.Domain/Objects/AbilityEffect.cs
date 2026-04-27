using AutoSim.Domain.Enums;

namespace AutoSim.Domain.Objects
{
    /// <summary>
    /// Defines an ability effect and its target rules.
    /// </summary>
    public sealed record AbilityEffect
    {
        /// <summary>
        /// Gets the effect type.
        /// </summary>
        public required CombatEffectType Type { get; init; }

        /// <summary>
        /// Gets the flat ability effect amount.
        /// </summary>
        public required int AbilityPower { get; init; }

        /// <summary>
        /// Gets the target mode used to build the candidate pool.
        /// </summary>
        public required TargetMode TargetMode { get; init; }

        /// <summary>
        /// Gets the target scope used to select candidates.
        /// </summary>
        public required TargetScope TargetScope { get; init; }

        /// <summary>
        /// Gets the optional effect duration in seconds.
        /// </summary>
        public double? Duration { get; init; }
    }
}
