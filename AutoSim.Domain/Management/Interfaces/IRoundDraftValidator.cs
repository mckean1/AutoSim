using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Management.Interfaces
{
    /// <summary>
    /// Validates per-round champion selections before simulation.
    /// </summary>
    public interface IRoundDraftValidator
    {
        /// <summary>
        /// Validates a round draft.
        /// </summary>
        /// <param name="draft">The round draft.</param>
        /// <param name="championCatalog">The available champion catalog.</param>
        void Validate(RoundDraft draft, IReadOnlyList<ChampionDefinition> championCatalog);
    }
}
