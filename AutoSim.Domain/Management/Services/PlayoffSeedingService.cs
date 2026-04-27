using AutoSim.Domain.Management.Models;

namespace AutoSim.Domain.Management.Services
{
    /// <summary>
    /// Creates league playoff seeds from division winners and wildcards.
    /// </summary>
    public sealed class PlayoffSeedingService
    {
        private readonly StandingsService _standingsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayoffSeedingService"/> class.
        /// </summary>
        /// <param name="standingsService">The standings service.</param>
        public PlayoffSeedingService(StandingsService? standingsService = null)
        {
            _standingsService = standingsService ?? new StandingsService();
        }

        /// <summary>
        /// Gets the eight playoff qualifiers for a league.
        /// </summary>
        /// <param name="league">The league.</param>
        /// <returns>The seeded team identifiers.</returns>
        public IReadOnlyList<string> GetLeaguePlayoffSeeds(League league)
        {
            ArgumentNullException.ThrowIfNull(league);

            IReadOnlyList<LeagueStanding> sortedStandings = _standingsService.SortStandings(
                league.Standings,
                league.Schedule);
            HashSet<string> divisionWinnerIds = league.Divisions
                .Select(division => sortedStandings.First(standing => division.TeamIds.Contains(standing.TeamId)).TeamId)
                .ToHashSet(StringComparer.Ordinal);
            IReadOnlyList<string> divisionWinners = sortedStandings
                .Where(standing => divisionWinnerIds.Contains(standing.TeamId))
                .Select(standing => standing.TeamId)
                .ToList();
            IReadOnlyList<string> wildcards = sortedStandings
                .Where(standing => !divisionWinnerIds.Contains(standing.TeamId))
                .Take(4)
                .Select(standing => standing.TeamId)
                .ToList();

            return divisionWinners.Concat(wildcards).ToList();
        }
    }
}
