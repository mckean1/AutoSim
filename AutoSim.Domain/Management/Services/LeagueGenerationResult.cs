using AutoSim.Domain.Management.Models;

namespace AutoSim.Domain.Management.Services
{
    /// <summary>
    /// Represents generated league data and related people.
    /// </summary>
    public sealed record LeagueGenerationResult(
        League League,
        IReadOnlyList<Coach> Coaches,
        IReadOnlyList<Player> Players);
}
