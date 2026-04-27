using AutoSim.Domain.Management.Models;

namespace AutoSim.Domain.Management.Services
{
    /// <summary>
    /// Represents the result of resolving a season week.
    /// </summary>
    public sealed record SeasonProgressionResult(WorldState World, IReadOnlyList<MatchResult> MatchResults);
}
