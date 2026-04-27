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
            TeamSide = TeamSide.Blue;
            Lane = Lane.Top;
            Intent = ChampionIntent.Laning;
            Position = definition.DefaultPosition;
            Level = 1;
            MaximumHealth = definition.Health;
            CurrentHealth = MaximumHealth;
            CurrentAttackPower = definition.AttackPower;
            AttackTimer = definition.AttackSpeed > 0 ? 1.0 / definition.AttackSpeed : 0;
            AbilityCooldown = definition.Ability.Cooldown;
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
        /// Gets or sets the champion's team side.
        /// </summary>
        public TeamSide TeamSide { get; set; }

        /// <summary>
        /// Gets or sets the assigned lane.
        /// </summary>
        public Lane Lane { get; set; }

        /// <summary>
        /// Gets or sets the current lane position.
        /// </summary>
        public double LanePosition { get; set; }

        /// <summary>
        /// Gets or sets the champion's current round-level intent.
        /// </summary>
        public ChampionIntent Intent { get; set; }

        /// <summary>
        /// Gets or sets the active fight identifier.
        /// </summary>
        public Guid? FightId { get; set; }

        /// <summary>
        /// Gets or sets the active fight position used for lane-local targeting.
        /// </summary>
        public double? CurrentFightPosition { get; set; }

        /// <summary>
        /// Gets or sets the active fight backline offset used for lane-local targeting.
        /// </summary>
        public double CurrentBacklineOffset { get; set; } = 5.0;

        /// <summary>
        /// Gets or sets the current runtime formation position.
        /// </summary>
        public FormationPosition Position { get; set; }

        /// <summary>
        /// Gets or sets the champion level.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Gets or sets the champion experience.
        /// </summary>
        public int Experience { get; set; }

        /// <summary>
        /// Gets or sets the champion gold.
        /// </summary>
        public int Gold { get; set; }

        /// <summary>
        /// Gets or sets the fractional experience accumulated by time-based rewards.
        /// </summary>
        public double ExperienceProgress { get; set; }

        /// <summary>
        /// Gets or sets the fractional gold accumulated by time-based rewards.
        /// </summary>
        public double GoldProgress { get; set; }

        /// <summary>
        /// Gets or sets the fractional healing accumulated by regeneration.
        /// </summary>
        public double HealingProgress { get; set; }

        /// <summary>
        /// Gets or sets the current runtime maximum health.
        /// </summary>
        public int MaximumHealth { get; set; }

        /// <summary>
        /// Gets or sets the current health.
        /// </summary>
        public int CurrentHealth { get; set; }

        /// <summary>
        /// Gets or sets the current runtime basic attack strength.
        /// </summary>
        public int CurrentAttackPower { get; set; }

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
        /// Gets or sets a value indicating whether the champion respawned during the current tick.
        /// </summary>
        public bool JustRespawned { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether death rewards and respawn setup have already been applied.
        /// </summary>
        public bool IsDeathProcessed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the champion is casting an ability.
        /// </summary>
        public bool IsCasting { get; set; }

        /// <summary>
        /// Gets or sets the remaining cast timer.
        /// </summary>
        public double CastTimer { get; set; }

        /// <summary>
        /// Gets or sets the pending ability being cast.
        /// </summary>
        public ChampionAbility? PendingAbility { get; set; }

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
