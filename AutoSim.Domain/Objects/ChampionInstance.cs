using System.Diagnostics.CodeAnalysis;
using AutoSim.Domain.Enums;

namespace AutoSim.Domain.Objects
{
    /// <summary>
    /// Represents mutable champion state during a match.
    /// </summary>
    public sealed class ChampionInstance
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChampionInstance"/> class.
        /// </summary>
        /// <param name="definition">The immutable champion definition.</param>
        /// <param name="playerId">The owning player identifier.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required parameter is null.</exception>
        [SetsRequiredMembers]
        public ChampionInstance(ChampionDefinition definition, string playerId)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            PlayerId = playerId ?? throw new ArgumentNullException(nameof(playerId));
            Position = definition.DefaultPosition;
            CurrentHealth = definition.Health;
        }

        /// <summary>
        /// Gets the immutable champion definition.
        /// </summary>
        public required ChampionDefinition Definition { get; init; }

        /// <summary>
        /// Gets the owning player identifier.
        /// </summary>
        public required string PlayerId { get; init; }

        /// <summary>
        /// Gets or sets the current runtime formation position.
        /// </summary>
        public FormationPosition Position { get; set; }

        /// <summary>
        /// Gets or sets the current health.
        /// </summary>
        public int CurrentHealth { get; set; }

        /// <summary>
        /// Gets or sets the remaining basic attack timer.
        /// </summary>
        public double AttackTimer { get; set; }

        /// <summary>
        /// Gets or sets the remaining ability cooldown.
        /// </summary>
        public double AbilityCooldown { get; set; }

        /// <summary>
        /// Gets or sets the remaining respawn timer.
        /// </summary>
        public double RespawnTimer { get; set; }

        /// <summary>
        /// Gets the active shields currently protecting the champion.
        /// </summary>
        public List<ActiveShield> Shields { get; } = [];

        /// <summary>
        /// Gets a value indicating whether the champion is alive.
        /// </summary>
        public bool IsAlive => CurrentHealth > 0;
    }
}
