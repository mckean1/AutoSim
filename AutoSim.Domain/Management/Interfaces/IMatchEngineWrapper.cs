using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Management.Interfaces
{
    /// <summary>
    /// Resolves scheduled matches by coordinating draft, validation, and round simulation.
    /// </summary>
    public interface IMatchEngineWrapper
    {
        /// <summary>
        /// Resolves a scheduled match.
        /// </summary>
        /// <param name="match">The scheduled match.</param>
        /// <param name="blueTeam">The blue team.</param>
        /// <param name="redTeam">The red team.</param>
        /// <param name="blueCoach">The blue-side coach.</param>
        /// <param name="redCoach">The red-side coach.</param>
        /// <param name="players">The available players.</param>
        /// <param name="championCatalog">The available champion catalog.</param>
        /// <param name="seed">The deterministic match seed.</param>
        /// <returns>The resolved match result.</returns>
        MatchResult Resolve(
            ScheduledMatch match,
            Team blueTeam,
            Team redTeam,
            Coach blueCoach,
            Coach redCoach,
            IReadOnlyList<Player> players,
            IReadOnlyList<ChampionDefinition> championCatalog,
            int seed);
    }
}
