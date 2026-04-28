using AutoSim.Domain.Management.Models;

namespace ConsoleApp.Presentation
{
    /// <summary>
    /// Represents one round prepared for presentation.
    /// </summary>
    public sealed record PresentedRound
    {
        /// <summary>
        /// Gets the replay messages for this round.
        /// </summary>
        public IReadOnlyList<ReplayMessage> Messages { get; init; } = [];

        /// <summary>
        /// Gets the management round result.
        /// </summary>
        public required RoundResult Result { get; init; }
    }
}
