using AutoSim.Domain.Enums;

namespace AutoSim.Domain.Objects
{
    /// <summary>
    /// Represents final per-champion round summary data.
    /// </summary>
    public sealed record ChampionRoundSummary
    {
        /// <summary>
        /// Gets the stable champion identifier.
        /// </summary>
        public required string ChampionId { get; init; }

        /// <summary>
        /// Gets the champion display name.
        /// </summary>
        public required string ChampionName { get; init; }

        /// <summary>
        /// Gets the champion team side.
        /// </summary>
        public required TeamSide TeamSide { get; init; }

        /// <summary>
        /// Gets the champion lane.
        /// </summary>
        public required Lane Lane { get; init; }

        /// <summary>
        /// Gets the final champion level.
        /// </summary>
        public required int Level { get; init; }

        /// <summary>
        /// Gets the final champion experience.
        /// </summary>
        public required int Experience { get; init; }

        /// <summary>
        /// Gets the final champion gold.
        /// </summary>
        public required int Gold { get; init; }

        /// <summary>
        /// Gets the champion kills.
        /// </summary>
        public required int Kills { get; init; }

        /// <summary>
        /// Gets the champion deaths.
        /// </summary>
        public required int Deaths { get; init; }

        /// <summary>
        /// Gets the final health.
        /// </summary>
        public required int FinalHealth { get; init; }

        /// <summary>
        /// Gets the final maximum health.
        /// </summary>
        public required int MaximumHealth { get; init; }
    }
}
