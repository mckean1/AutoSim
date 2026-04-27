namespace AutoSim.Domain.Objects
{
    /// <summary>
    /// Defines a champion's active combat ability.
    /// </summary>
    public sealed record ChampionAbility
    {
        /// <summary>
        /// Gets the stable ability identifier.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Gets the ability display name.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Gets the base cooldown in seconds.
        /// </summary>
        public required double Cooldown { get; init; }

        /// <summary>
        /// Gets the cast time in seconds.
        /// </summary>
        public required double CastTime { get; init; }

        /// <summary>
        /// Gets the effects applied when the ability is used.
        /// </summary>
        public required IReadOnlyList<CombatEffect> Effects { get; init; }
    }
}
