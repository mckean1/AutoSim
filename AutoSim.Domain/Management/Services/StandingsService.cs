using AutoSim.Domain.Enums;
using AutoSim.Domain.Management.Models;
using MatchType = AutoSim.Domain.Enums.MatchType;

namespace AutoSim.Domain.Management.Services
{
    /// <summary>
    /// Updates and sorts league standings.
    /// </summary>
    public sealed class StandingsService
    {
        /// <summary>
        /// Applies a regular-season match result to league standings.
        /// </summary>
        /// <param name="league">The league to update.</param>
        /// <param name="match">The completed scheduled match.</param>
        /// <param name="result">The match result.</param>
        /// <returns>The updated league.</returns>
        public League ApplyMatchResult(League league, ScheduledMatch match, MatchResult result)
        {
            ArgumentNullException.ThrowIfNull(league);
            ArgumentNullException.ThrowIfNull(match);
            ArgumentNullException.ThrowIfNull(result);

            if (match.MatchType != MatchType.RegularSeason)
            {
                return league;
            }

            Dictionary<string, LeagueStanding> standings = league.Standings
                .ToDictionary(standing => standing.TeamId, StringComparer.Ordinal);
            LeagueStanding homeStanding = standings[match.HomeTeamId];
            LeagueStanding awayStanding = standings[match.AwayTeamId];
            bool homeWon = string.Equals(result.WinningTeamId, match.HomeTeamId, StringComparison.Ordinal);
            bool isDivisionMatch = IsDivisionMatch(league, match.HomeTeamId, match.AwayTeamId);

            standings[match.HomeTeamId] = AddResult(
                homeStanding,
                homeWon,
                result.HomeRoundWins,
                result.AwayRoundWins,
                isDivisionMatch);
            standings[match.AwayTeamId] = AddResult(
                awayStanding,
                !homeWon,
                result.AwayRoundWins,
                result.HomeRoundWins,
                isDivisionMatch);

            IReadOnlyList<LeagueStanding> sortedStandings = SortStandings(standings.Values, league.Schedule);
            return league with { Standings = sortedStandings };
        }

        /// <summary>
        /// Sorts standings deterministically using the Management Layer tiebreaker order.
        /// </summary>
        /// <param name="standings">The standings to sort.</param>
        /// <param name="schedule">The league schedule for future head-to-head extension.</param>
        /// <returns>The sorted standings.</returns>
        public IReadOnlyList<LeagueStanding> SortStandings(
            IEnumerable<LeagueStanding> standings,
            IReadOnlyList<ScheduledMatch> schedule)
        {
            ArgumentNullException.ThrowIfNull(standings);
            ArgumentNullException.ThrowIfNull(schedule);

            return standings
                .OrderByDescending(standing => standing.MatchWins)
                .ThenByDescending(standing => standing.Points)
                .ThenByDescending(standing => GetHeadToHeadWins(standing.TeamId, schedule))
                .ThenByDescending(standing => GetHeadToHeadPoints(standing.TeamId, schedule))
                .ThenByDescending(standing => standing.DivisionWins)
                .ThenBy(standing => standing.TeamId, StringComparer.Ordinal)
                .ToList();
        }

        private static LeagueStanding AddResult(
            LeagueStanding standing,
            bool wonMatch,
            int roundWins,
            int roundLosses,
            bool isDivisionMatch) =>
            standing with
            {
                DivisionLosses = standing.DivisionLosses + (isDivisionMatch && !wonMatch ? 1 : 0),
                DivisionWins = standing.DivisionWins + (isDivisionMatch && wonMatch ? 1 : 0),
                MatchLosses = standing.MatchLosses + (wonMatch ? 0 : 1),
                MatchWins = standing.MatchWins + (wonMatch ? 1 : 0),
                Points = standing.Points + roundWins - roundLosses,
                RoundLosses = standing.RoundLosses + roundLosses,
                RoundWins = standing.RoundWins + roundWins
            };

        private static int GetHeadToHeadPoints(string teamId, IReadOnlyList<ScheduledMatch> schedule)
        {
            _ = teamId;
            _ = schedule;
            return 0;
        }

        private static int GetHeadToHeadWins(string teamId, IReadOnlyList<ScheduledMatch> schedule)
        {
            _ = teamId;
            _ = schedule;
            return 0;
        }

        private static bool IsDivisionMatch(League league, string homeTeamId, string awayTeamId)
        {
            Dictionary<string, string> divisionByTeamId = league.Divisions
                .SelectMany(division => division.TeamIds.Select(teamId => new { division.Id, TeamId = teamId }))
                .ToDictionary(entry => entry.TeamId, entry => entry.Id, StringComparer.Ordinal);

            return string.Equals(divisionByTeamId[homeTeamId], divisionByTeamId[awayTeamId], StringComparison.Ordinal);
        }
    }
}
