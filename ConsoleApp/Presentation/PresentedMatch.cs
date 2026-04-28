using AutoSim.Domain.Management.Models;

namespace ConsoleApp.Presentation
{
    /// <summary>
    /// Represents a match prepared for presentation.
    /// </summary>
    public sealed record PresentedMatch
    {
        /// <summary>
        /// Gets the match result.
        /// </summary>
        public required MatchResult Result { get; init; }

        /// <summary>
        /// Gets the presented rounds.
        /// </summary>
        public IReadOnlyList<PresentedRound> Rounds { get; init; } = [];
    }
}
