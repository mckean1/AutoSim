using AutoSim.Domain.Management.Models;

namespace AutoSim.Domain.Management.Interfaces
{
    /// <summary>
    /// Selects champions for one round under coach-controlled draft logic.
    /// </summary>
    public interface IRoundDraftService
    {
        /// <summary>
        /// Creates champion selections for one round.
        /// </summary>
        /// <param name="context">The round draft context.</param>
        /// <returns>The selected round draft.</returns>
        RoundDraft DraftRound(RoundDraftContext context);
    }
}
