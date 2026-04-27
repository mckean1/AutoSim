using AutoSim.Domain.Enums;

namespace AutoSim.Domain.Objects
{
    /// <summary>
    /// Represents the outcome of one simulated round.
    /// </summary>
    public sealed record RoundResult
    {
        /// <summary>
        /// Gets the winning side.
        /// </summary>
        public required TeamSide WinningSide { get; init; }

        /// <summary>
        /// Gets the final blue kill score.
        /// </summary>
        public required int BlueKills { get; init; }

        /// <summary>
        /// Gets the final red kill score.
        /// </summary>
        public required int RedKills { get; init; }

        /// <summary>
        /// Gets the total blue gold.
        /// </summary>
        public required int BlueGold { get; init; }

        /// <summary>
        /// Gets the total red gold.
        /// </summary>
        public required int RedGold { get; init; }

        /// <summary>
        /// Gets the total blue experience.
        /// </summary>
        public required int BlueExperience { get; init; }

        /// <summary>
        /// Gets the total red experience.
        /// </summary>
        public required int RedExperience { get; init; }

        /// <summary>
        /// Gets the round duration in seconds.
        /// </summary>
        public required double Duration { get; init; }

        /// <summary>
        /// Gets the optional round event log.
        /// </summary>
        public IReadOnlyList<string> EventLog { get; init; } = [];
    }
}
