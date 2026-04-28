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

            IReadOnlyList<RequiredMatchEdge> requiredMatches = CreateRequiredMatches(teams, divisions);
            IReadOnlyList<IReadOnlyList<RequiredMatchEdge>> weeklyMatches = AssignMatchesToWeeks(requiredMatches);
            List<ScheduledMatch> schedule = [];

            for (int weekIndex = 0; weekIndex < weeklyMatches.Count; weekIndex++)
            {
                int week = weekIndex + 1;
                int matchIndex = 1;
                foreach (RequiredMatchEdge match in weeklyMatches[weekIndex])
                {
                    bool flipHomeAway = ((week + matchIndex) % 2) == 0;
                    string homeTeamId = flipHomeAway ? match.TeamTwoId : match.TeamOneId;
                    string awayTeamId = flipHomeAway ? match.TeamOneId : match.TeamTwoId;
                    schedule.Add(new ScheduledMatch
                    {
                        AwayTeamId = awayTeamId,
                        BestOf = 3,
                        HomeTeamId = homeTeamId,
                        Id = $"{leagueId}-regular-week-{week}-match-{matchIndex}",
                        LeagueId = leagueId,
                        MatchType = MatchType.RegularSeason,
                        Week = week
                    });
                    matchIndex++;
                }
            }

            return schedule;
        }

        private static IReadOnlyList<RequiredMatchEdge> CreateRequiredMatches(
            IReadOnlyList<Team> teams,
            IReadOnlyList<Division> divisions)
        {
            Dictionary<string, string> divisionByTeamId = divisions
                .SelectMany(division => division.TeamIds.Select(teamId => new { division.Id, TeamId = teamId }))
                .ToDictionary(entry => entry.TeamId, entry => entry.Id, StringComparer.Ordinal);
            List<RequiredMatchEdge> requiredMatches = [];

            for (int homeIndex = 0; homeIndex < teams.Count; homeIndex++)
            {
                for (int awayIndex = homeIndex + 1; awayIndex < teams.Count; awayIndex++)
                {
                    Team homeTeam = teams[homeIndex];
                    Team awayTeam = teams[awayIndex];
                    bool isDivisionMatch = string.Equals(
                        divisionByTeamId[homeTeam.Id],
                        divisionByTeamId[awayTeam.Id],
                        StringComparison.Ordinal);

                    requiredMatches.Add(new RequiredMatchEdge(
                        homeTeam.Id,
                        awayTeam.Id,
                        isDivisionMatch,
                        $"{homeTeam.Id}|{awayTeam.Id}|1"));

                    if (isDivisionMatch)
                    {
                        requiredMatches.Add(new RequiredMatchEdge(
                            homeTeam.Id,
                            awayTeam.Id,
                            true,
                            $"{homeTeam.Id}|{awayTeam.Id}|2"));
                    }
                }
            }

            return requiredMatches;
        }

        private static IReadOnlyList<IReadOnlyList<RequiredMatchEdge>> AssignMatchesToWeeks(
            IReadOnlyList<RequiredMatchEdge> requiredMatches)
        {
            for (int attempt = 0; attempt < 200; attempt++)
            {
                List<RequiredMatchEdge> remainingMatches = requiredMatches
                    .OrderBy(match => CreateStableScore(match.Id, attempt))
                    .ToList();
                List<IReadOnlyList<RequiredMatchEdge>> weeks = [];

                for (int week = 1; week <= RegularSeasonWeeks; week++)
                {
                    IReadOnlyList<RequiredMatchEdge>? weeklyMatches = FindPerfectWeeklyMatching(
                        remainingMatches,
                        week,
                        attempt);
                    if (weeklyMatches is null)
                    {
                        break;
                    }

                    HashSet<string> weeklyMatchIds = weeklyMatches
                        .Select(match => match.Id)
                        .ToHashSet(StringComparer.Ordinal);
                    remainingMatches = remainingMatches
                        .Where(match => !weeklyMatchIds.Contains(match.Id))
                        .ToList();
                    weeks.Add(weeklyMatches);
                }

                if (weeks.Count == RegularSeasonWeeks && remainingMatches.Count == 0)
                {
                    return weeks;
                }
            }

            throw new InvalidOperationException("Unable to generate a valid 23-week regular-season schedule.");
        }

        private static IReadOnlyList<RequiredMatchEdge>? FindPerfectWeeklyMatching(
            IReadOnlyList<RequiredMatchEdge> remainingMatches,
            int week,
            int attempt)
        {
            IReadOnlyList<string> teamIds = remainingMatches
                .SelectMany(match => new[] { match.TeamOneId, match.TeamTwoId })
                .Distinct(StringComparer.Ordinal)
                .OrderBy(teamId => teamId, StringComparer.Ordinal)
                .ToList();
            HashSet<string> usedTeamIds = new(StringComparer.Ordinal);
            List<RequiredMatchEdge> selectedMatches = [];

            bool wasMatched = TryBuildWeeklyMatching(
                teamIds,
                remainingMatches,
                usedTeamIds,
                selectedMatches,
                week,
                attempt);

            return wasMatched ? selectedMatches.ToList() : null;
        }

        private static bool TryBuildWeeklyMatching(
            IReadOnlyList<string> teamIds,
            IReadOnlyList<RequiredMatchEdge> remainingMatches,
            ISet<string> usedTeamIds,
            IList<RequiredMatchEdge> selectedMatches,
            int week,
            int attempt)
        {
            if (usedTeamIds.Count == teamIds.Count)
            {
                return true;
            }

            string? teamId = null;
            IReadOnlyList<RequiredMatchEdge> candidateMatches = [];
            foreach (string currentTeamId in teamIds.Where(currentTeamId => !usedTeamIds.Contains(currentTeamId)))
            {
                IReadOnlyList<RequiredMatchEdge> currentCandidates = remainingMatches
                    .Where(match => match.IncludesTeam(currentTeamId)
                        && !usedTeamIds.Contains(match.TeamOneId)
                        && !usedTeamIds.Contains(match.TeamTwoId))
                    .OrderByDescending(match => ShouldFavorDivisionMatch(match, week, attempt))
                    .ThenBy(match => CreateStableScore(match.Id, week + attempt))
                    .ToList();

                if (teamId is null || currentCandidates.Count < candidateMatches.Count)
                {
                    teamId = currentTeamId;
                    candidateMatches = currentCandidates;
                    if (candidateMatches.Count == 0)
                    {
                        break;
                    }
                }
            }

            foreach (RequiredMatchEdge match in candidateMatches)
            {
                usedTeamIds.Add(match.TeamOneId);
                usedTeamIds.Add(match.TeamTwoId);
                selectedMatches.Add(match);

                if (TryBuildWeeklyMatching(teamIds, remainingMatches, usedTeamIds, selectedMatches, week, attempt))
                {
                    return true;
                }

                selectedMatches.RemoveAt(selectedMatches.Count - 1);
                usedTeamIds.Remove(match.TeamOneId);
                usedTeamIds.Remove(match.TeamTwoId);
            }

            return false;
        }

        private static int CreateStableScore(string value, int seed)
        {
            int score = seed;
            foreach (char character in value)
            {
                score = unchecked((score * 397) ^ character);
            }

            return score;
        }

        private static bool ShouldFavorDivisionMatch(RequiredMatchEdge match, int week, int attempt) =>
            match.IsDivisionMatch && ((week + attempt) % 3 != 0);

        private sealed record RequiredMatchEdge(
            string TeamOneId,
            string TeamTwoId,
            bool IsDivisionMatch,
            string Id)
        {
            public bool IncludesTeam(string teamId) =>
                string.Equals(TeamOneId, teamId, StringComparison.Ordinal)
                || string.Equals(TeamTwoId, teamId, StringComparison.Ordinal);
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
