using AutoSim.Domain.Enums;

namespace AutoSim.Domain.Objects
{
    /// <summary>
    /// Represents one structured event emitted during a simulated round.
    /// </summary>
    public sealed record RoundEvent
    {
        /// <summary>
        /// Gets the round time in seconds when the event occurred.
        /// </summary>
        public required double TimeSeconds { get; init; }

        /// <summary>
        /// Gets the event type.
        /// </summary>
        public required RoundEventType Type { get; init; }

        /// <summary>
        /// Gets the readable event message.
        /// </summary>
        public required string Message { get; init; }

        /// <summary>
        /// Gets the lane associated with the event, when available.
        /// </summary>
        public string? Lane { get; init; }

        /// <summary>
        /// Gets the source team side, when available.
        /// </summary>
        public string? SourceTeamSide { get; init; }

        /// <summary>
        /// Gets the source champion identifier, when available.
        /// </summary>
        public string? SourceChampionId { get; init; }

        /// <summary>
        /// Gets the source champion display name, when available.
        /// </summary>
        public string? SourceChampionName { get; init; }

        /// <summary>
        /// Gets the source player identifier, when available.
        /// </summary>
        public string? SourcePlayerId { get; init; }

        /// <summary>
        /// Gets the target team side, when available.
        /// </summary>
        public string? TargetTeamSide { get; init; }

        /// <summary>
        /// Gets the target champion identifier, when available.
        /// </summary>
        public string? TargetChampionId { get; init; }

        /// <summary>
        /// Gets the target champion display name, when available.
        /// </summary>
        public string? TargetChampionName { get; init; }

        /// <summary>
        /// Gets the target player identifier, when available.
        /// </summary>
        public string? TargetPlayerId { get; init; }

        /// <summary>
        /// Gets the fight identifier, when available.
        /// </summary>
        public Guid? FightId { get; init; }

        /// <summary>
        /// Gets the structured numeric amount for damage, healing, or shielding events.
        /// </summary>
        public int? Amount { get; init; }

        /// <summary>
        /// Gets the team side associated with the event, when available.
        /// </summary>
        public string? TeamSide { get; init; }

        /// <summary>
        /// Gets the source champion identifier, when available.
        /// </summary>
        public string? ChampionId { get; init; }
    }
}
