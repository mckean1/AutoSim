using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Management.Models
{
    /// <summary>
    /// Provides draft inputs for one round of a match.
    /// </summary>
    public sealed record RoundDraftContext
    {
        /// <summary>
        /// Gets the available champion catalog.
        /// </summary>
        public IReadOnlyList<ChampionDefinition> ChampionCatalog { get; init; } = [];

        /// <summary>
        /// Gets the blue-side coach.
        /// </summary>
        public required Coach BlueCoach { get; init; }

        /// <summary>
        /// Gets blue-side players.
        /// </summary>
        public IReadOnlyList<Player> BluePlayers { get; init; } = [];

        /// <summary>
        /// Gets the blue team.
        /// </summary>
        public required Team BlueTeam { get; init; }

        /// <summary>
        /// Gets the match being drafted.
        /// </summary>
        public required ScheduledMatch Match { get; init; }

        /// <summary>
        /// Gets the completed previous round results.
        /// </summary>
        public IReadOnlyList<RoundResult> PreviousRounds { get; init; } = [];

        /// <summary>
        /// Gets the red-side coach.
        /// </summary>
        public required Coach RedCoach { get; init; }

        /// <summary>
        /// Gets red-side players.
        /// </summary>
        public IReadOnlyList<Player> RedPlayers { get; init; } = [];

        /// <summary>
        /// Gets the red team.
        /// </summary>
        public required Team RedTeam { get; init; }

        /// <summary>
        /// Gets the round number.
        /// </summary>
        public int RoundNumber { get; init; }

        /// <summary>
        /// Gets the deterministic round seed.
        /// </summary>
        public int Seed { get; init; }
    }
}
