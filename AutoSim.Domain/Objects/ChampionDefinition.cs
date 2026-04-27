using AutoSim.Domain.Enums;

namespace AutoSim.Domain.Objects
{
    /// <summary>
    /// Defines immutable champion combat data.
    /// </summary>
    public sealed record ChampionDefinition
    {
        /// <summary>
        /// Gets the stable champion identifier.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Gets the champion display name.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Gets the champion role.
        /// </summary>
        public required ChampionRole Role { get; init; }

        /// <summary>
        /// Gets the champion's default match formation position.
        /// </summary>
        public required FormationPosition DefaultPosition { get; init; }

        /// <summary>
        /// Gets the champion's maximum health.
        /// </summary>
        public required int Health { get; init; }

        /// <summary>
        /// Gets the champion's power value.
        /// </summary>
        public required int Power { get; init; }

        /// <summary>
        /// Gets the champion's attacks per second.
        /// </summary>
        public required double AttackSpeed { get; init; }

        /// <summary>
        /// Gets the champion's basic attack.
        /// </summary>
        public required ChampionAttack Attack { get; init; }

        /// <summary>
        /// Gets the champion's active ability.
        /// </summary>
        public required ChampionAbility Ability { get; init; }
    }
}
