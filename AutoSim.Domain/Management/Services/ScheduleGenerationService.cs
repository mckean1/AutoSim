using AutoSim.Domain.Enums;
using AutoSim.Domain.Management.Models;
using MatchType = AutoSim.Domain.Enums.MatchType;

namespace AutoSim.Domain.Management.Services
{
    /// <summary>
    /// Generates regular-season and reserved championship schedules.
    /// </summary>
    public sealed class ScheduleGenerationService
    {
        /// <summary>
        /// The final regular-season week.
        /// </summary>
        public const int RegularSeasonWeeks = 23;

        /// <summary>
        /// Generates the regular-season league schedule.
        /// </summary>
        /// <param name="leagueId">The league identifier.</param>
        /// <param name="teams">The teams in the league.</param>
        /// <param name="divisions">The league divisions.</param>
        /// <returns>The generated schedule.</returns>
        public IReadOnlyList<ScheduledMatch> GenerateRegularSeasonSchedule(
            string leagueId,
            IReadOnlyList<Team> teams,
            IReadOnlyList<Division> divisions)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(leagueId);
            ArgumentNullException.ThrowIfNull(teams);
            ArgumentNullException.ThrowIfNull(divisions);

            Dictionary<string, string> divisionByTeamId = divisions
                .SelectMany(division => division.TeamIds.Select(teamId => new { division.Id, TeamId = teamId }))
                .ToDictionary(entry => entry.TeamId, entry => entry.Id, StringComparer.Ordinal);

            List<(Team HomeTeam, Team AwayTeam)> pairings = [];
            for (int homeIndex = 0; homeIndex < teams.Count; homeIndex++)
            {
                for (int awayIndex = homeIndex + 1; awayIndex < teams.Count; awayIndex++)
                {
                    Team homeTeam = teams[homeIndex];
                    Team awayTeam = teams[awayIndex];
                    pairings.Add((homeTeam, awayTeam));

                    if (string.Equals(
                        divisionByTeamId[homeTeam.Id],
                        divisionByTeamId[awayTeam.Id],
                        StringComparison.Ordinal))
                    {
                        pairings.Add((awayTeam, homeTeam));
                    }
                }
            }

            return pairings
                .Select((pairing, index) => new ScheduledMatch
                {
                    AwayTeamId = pairing.AwayTeam.Id,
                    BestOf = 3,
                    HomeTeamId = pairing.HomeTeam.Id,
                    Id = $"{leagueId}-regular-{index + 1}",
                    LeagueId = leagueId,
                    MatchType = MatchType.RegularSeason,
                    Week = (index % RegularSeasonWeeks) + 1
                })
                .OrderBy(match => match.Week)
                .ThenBy(match => match.Id, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Creates placeholder league playoff reserved matches.
        /// </summary>
        /// <param name="leagueId">The league identifier.</param>
        /// <returns>The reserved playoff schedule.</returns>
        public IReadOnlyList<ScheduledMatch> GenerateLeaguePlayoffReservations(string leagueId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(leagueId);

            return
            [
                CreatePlayoffReservation(leagueId, "quarterfinal-1", MatchType.LeagueQuarterfinal, 24, 5),
                CreatePlayoffReservation(leagueId, "quarterfinal-2", MatchType.LeagueQuarterfinal, 24, 5),
                CreatePlayoffReservation(leagueId, "quarterfinal-3", MatchType.LeagueQuarterfinal, 24, 5),
                CreatePlayoffReservation(leagueId, "quarterfinal-4", MatchType.LeagueQuarterfinal, 24, 5),
                CreatePlayoffReservation(leagueId, "semifinal-1", MatchType.LeagueSemifinal, 25, 5),
                CreatePlayoffReservation(leagueId, "semifinal-2", MatchType.LeagueSemifinal, 25, 5),
                CreatePlayoffReservation(leagueId, "final", MatchType.LeagueFinal, 26, 7)
            ];
        }

        /// <summary>
        /// Creates placeholder world championship reserved matches.
        /// </summary>
        /// <returns>The reserved world championship schedule.</returns>
        public IReadOnlyList<ScheduledMatch> GenerateWorldChampionshipReservations() =>
        [
            new ScheduledMatch
            {
                AwayTeamId = "world-champion-placeholder-2",
                BestOf = 7,
                HomeTeamId = "world-champion-placeholder-1",
                Id = "world-championship-semifinal-1",
                MatchType = MatchType.WorldChampionshipSemifinal,
                Week = 27
            },
            new ScheduledMatch
            {
                AwayTeamId = "world-champion-placeholder-4",
                BestOf = 7,
                HomeTeamId = "world-champion-placeholder-3",
                Id = "world-championship-semifinal-2",
                MatchType = MatchType.WorldChampionshipSemifinal,
                Week = 27
            },
            new ScheduledMatch
            {
                AwayTeamId = "world-semifinal-winner-2",
                BestOf = 9,
                HomeTeamId = "world-semifinal-winner-1",
                Id = "world-championship-final",
                MatchType = MatchType.WorldChampionshipFinal,
                Week = 28
            }
        ];

        private static ScheduledMatch CreatePlayoffReservation(
            string leagueId,
            string roundId,
            MatchType matchType,
            int week,
            int bestOf) =>
            new()
            {
                AwayTeamId = $"{leagueId}-{roundId}-away-placeholder",
                BestOf = bestOf,
                HomeTeamId = $"{leagueId}-{roundId}-home-placeholder",
                Id = $"{leagueId}-{roundId}",
                LeagueId = leagueId,
                MatchType = matchType,
                Week = week
            };
    }
}
