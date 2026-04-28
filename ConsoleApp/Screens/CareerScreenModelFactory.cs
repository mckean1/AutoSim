using AutoSim.Domain.Management.Models;
using ConsoleApp.Constants;

namespace ConsoleApp.Screens
{
    /// <summary>
    /// Builds screen render models for career and management screens.
    /// </summary>
    internal sealed class CareerScreenModelFactory
    {
        public ScreenRenderModel BuildHomeScreen(
            ScreenHeaderModel header,
            WorldState world,
            Team humanTeam,
            League league,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(world);
            ArgumentNullException.ThrowIfNull(humanTeam);
            ArgumentNullException.ThrowIfNull(league);

            LeagueStanding standing = GetStanding(league, humanTeam.Id);
            Division division = GetDivision(league, humanTeam);
            ScheduledMatch? nextMatch = GetNextHumanMatch(world, humanTeam);

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.ShowTeam,
                    ConsoleConstants.ShowLeague,
                    ConsoleConstants.ShowSchedule,
                    ConsoleConstants.ShowChampions,
                    ConsoleConstants.StartMatch,
                    ConsoleConstants.ViewLastMatch,
                    ConsoleConstants.ViewReplay,
                    ConsoleConstants.ViewRounds,
                    ConsoleConstants.Help
                ],
                ContentLines =
                [
                    $"Next match: {FormatMatch(world, nextMatch)}",
                    string.Empty,
                    "Team snapshot",
                    $"  Coach: {GetHumanCoach(world).Name}",
                    $"  Team: {humanTeam.Name}",
                    $"  Record: {standing.MatchWins}-{standing.MatchLosses}",
                    $"  Points: {FormatPoints(standing.Points)}",
                    string.Empty,
                    $"Current league: {FormatLeagueName(league)}",
                    $"Division: {division.Name} Division",
                    string.Empty,
                    $"Recommended action: {GetRecommendedAction(world, humanTeam)}"
                ],
                Header = header,
                Message = message,
                Title = "Home"
            };
        }

        public ScreenRenderModel BuildTeamScreen(
            ScreenHeaderModel header,
            WorldState world,
            Team team,
            League league,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(world);
            ArgumentNullException.ThrowIfNull(team);
            ArgumentNullException.ThrowIfNull(league);

            Coach coach = world.Coaches.First(currentCoach =>
                string.Equals(currentCoach.Id, team.CoachId, StringComparison.Ordinal));
            Division division = GetDivision(league, team);
            IReadOnlyList<Player> players = world.Players
                .Where(player => team.PlayerIds.Contains(player.Id))
                .OrderBy(player => player.PositionRole)
                .ThenBy(player => player.Name, StringComparer.Ordinal)
                .ToList();

            List<string> lines =
            [
                $"Coach: {coach.Name}",
                $"Team: {team.Name}",
                $"League: {FormatLeagueName(league)}",
                $"Division: {division.Name} Division",
                string.Empty,
                "Roster",
                "Role      Player"
            ];
            lines.AddRange(players.Select(player => $"{player.PositionRole,-9} {player.Name}"));
            lines.Add(string.Empty);
            lines.Add($"Next match: {FormatMatch(world, GetNextHumanMatch(world, team))}");

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Home,
                    ConsoleConstants.ShowLeague,
                    ConsoleConstants.ShowSchedule,
                    ConsoleConstants.ShowChampions,
                    ConsoleConstants.StartMatch,
                    ConsoleConstants.Help
                ],
                ContentLines = lines,
                Header = header,
                Message = message,
                Title = "Team Detail"
            };
        }

        public ScreenRenderModel BuildLeagueScreen(
            ScreenHeaderModel header,
            WorldState world,
            Team humanTeam,
            League league,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(world);
            ArgumentNullException.ThrowIfNull(humanTeam);
            ArgumentNullException.ThrowIfNull(league);

            Division division = GetDivision(league, humanTeam);
            IReadOnlyList<LeagueStanding> standings = GetStandingsForDivision(league, division);
            string standingsTitle = standings.Count > 0 ? $"{division.Name} Division Standings" : "Overall Standings";
            standings = standings.Count > 0 ? standings : league.Standings;

            List<string> lines =
            [
                $"League: {FormatLeagueName(league)}",
                $"Your record: {FormatRecord(GetStanding(league, humanTeam.Id))}",
                $"Your points: {FormatPoints(GetStanding(league, humanTeam.Id).Points)}",
                string.Empty,
                standingsTitle,
                "Rank  Team                         Record  Points"
            ];
            int rank = 1;
            lines.AddRange(standings.Select(standing =>
                $"{rank++,4}  {FormatTeamName(world, standing.TeamId),-27} {FormatRecord(standing),-7} "
                + $"{FormatPoints(standing.Points)}"));

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Home,
                    ConsoleConstants.ShowTeam,
                    ConsoleConstants.ShowSchedule,
                    ConsoleConstants.ShowChampions,
                    ConsoleConstants.Help
                ],
                ContentLines = lines,
                Header = header,
                Message = message,
                Title = "League"
            };
        }

        public ScreenRenderModel BuildScheduleScreen(
            ScreenHeaderModel header,
            WorldState world,
            Team humanTeam,
            League league,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(world);
            ArgumentNullException.ThrowIfNull(humanTeam);
            ArgumentNullException.ThrowIfNull(league);

            IReadOnlyList<ScheduledMatch> matches = league.Schedule
                .Where(match => match.Week == world.Season.CurrentWeek)
                .OrderBy(match => match.MatchType)
                .ThenBy(match => FormatTeamName(world, match.HomeTeamId), StringComparer.Ordinal)
                .ThenBy(match => FormatTeamName(world, match.AwayTeamId), StringComparer.Ordinal)
                .ToList();
            ScheduledMatch? humanMatch = matches.FirstOrDefault(match => IsTeamMatch(match, humanTeam.Id));

            List<string> lines =
            [
                $"Current week: {world.Season.CurrentWeek}",
                $"Human team match: {FormatMatch(world, humanMatch)}",
                string.Empty,
                "Scheduled matches",
                "Match                                      Status"
            ];
            lines.AddRange(matches
                .Take(12)
                .Select(match => $"{FormatMatch(world, match),-42} {FormatMatchStatus(match)}"));

            if (matches.Count > 12)
            {
                lines.Add($"... {matches.Count - 12} more matches");
            }

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Home,
                    ConsoleConstants.StartMatch,
                    ConsoleConstants.ShowTeam,
                    ConsoleConstants.ShowLeague,
                    ConsoleConstants.ShowChampions,
                    ConsoleConstants.Help
                ],
                ContentLines = lines,
                Header = header,
                Message = message,
                Title = "Schedule"
            };
        }

        public ScreenRenderModel BuildPlayerScreen(
            ScreenHeaderModel header,
            Player player,
            Team? team,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(player);

            List<string> lines =
            [
                player.Name,
                string.Empty,
                $"Team: {team?.Name ?? "Free Agent"}",
                $"Position: {player.PositionRole}",
                $"Rating: {GetPlayerRating(player)}",
                $"Traits: {GetPlayerTraits(player)}",
                string.Empty,
                "Contract",
                "Contracts are not implemented yet.",
                string.Empty,
                "Recent Performance",
                "Recent player performance is not available yet.",
                string.Empty,
                "Notes",
                "Future decision-making stats will appear here when player systems expand."
            ];

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Back,
                    ConsoleConstants.ShowTeam,
                    ConsoleConstants.ShowSchedule,
                    ConsoleConstants.Home,
                    ConsoleConstants.Help
                ],
                ContentLines = lines,
                Header = header,
                Message = message,
                Title = "Player Detail"
            };
        }

        public ScreenRenderModel BuildPlayoffPictureScreen(
            ScreenHeaderModel header,
            WorldState world,
            Team humanTeam,
            League league,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(world);
            ArgumentNullException.ThrowIfNull(humanTeam);
            ArgumentNullException.ThrowIfNull(league);

            IReadOnlyList<string> divisionLeaderLines = league.Divisions
                .OrderBy(division => division.Name)
                .Select(division =>
                {
                    LeagueStanding? leader = GetStandingsForDivision(league, division).FirstOrDefault();
                    return leader is null
                        ? $"{division.Name}: standings unavailable"
                        : $"{division.Name}: {FormatTeamName(world, leader.TeamId)} "
                        + $"({FormatRecord(leader)}, {FormatPoints(leader.Points)})";
                })
                .ToList();

            IReadOnlyList<string> wildcardLines = league.Standings
                .Where(standing => !league.Divisions
                    .Select(division => GetStandingsForDivision(league, division).FirstOrDefault()?.TeamId)
                    .Where(teamId => teamId is not null)
                    .Contains(standing.TeamId))
                .OrderByDescending(standing => standing.MatchWins)
                .ThenByDescending(standing => standing.Points)
                .ThenBy(standing => FormatTeamName(world, standing.TeamId), StringComparer.Ordinal)
                .Take(4)
                .Select(standing =>
                    $"{FormatTeamName(world, standing.TeamId)} ({FormatRecord(standing)}, {FormatPoints(standing.Points)})")
                .ToList();

            IReadOnlyList<string> bubbleLines = league.Standings
                .Where(standing => !wildcardLines.Any(line =>
                    line.StartsWith(FormatTeamName(world, standing.TeamId), StringComparison.Ordinal)))
                .OrderByDescending(standing => standing.MatchWins)
                .ThenByDescending(standing => standing.Points)
                .ThenBy(standing => FormatTeamName(world, standing.TeamId), StringComparer.Ordinal)
                .Take(2)
                .Select(standing =>
                    $"{FormatTeamName(world, standing.TeamId)} ({FormatRecord(standing)}, {FormatPoints(standing.Points)})")
                .ToList();

            List<string> lines =
            [
                $"Current league: {FormatLeagueName(league)}",
                $"Current week: {world.Season.CurrentWeek}",
                $"Season status: {GetSeasonStatus(world.Season.CurrentWeek)}",
                string.Empty,
                "Format",
                "Regular season lasts 23 weeks.",
                "Playoffs: 8 teams total.",
                "- 4 division winners",
                "- 4 wildcard teams",
                "Week 24: League Quarterfinals, best-of-5",
                "Week 25: League Semifinals, best-of-5",
                "Week 26: League Finals, best-of-7",
                "World Tier league champions advance to World Championship.",
                "Week 27: World Championship Semifinals, best-of-7",
                "Week 28: World Championship Final, best-of-9",
                string.Empty,
                "Division Leaders",
                .. divisionLeaderLines
            ];

            if (league.Standings.Count == 0)
            {
                lines.Add(string.Empty);
                lines.Add("Playoff picture is not available until standings data is available.");
            }
            else
            {
                lines.Add(string.Empty);
                lines.Add("Wildcard Candidates");
                lines.AddRange(wildcardLines.DefaultIfEmpty(
                    "Playoff picture is not available until standings data is available."));
                lines.Add(string.Empty);
                lines.Add("Bubble Teams");
                lines.AddRange(bubbleLines.DefaultIfEmpty(
                    "Playoff picture is not available until standings data is available."));
            }

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Home,
                    ConsoleConstants.ShowLeague,
                    ConsoleConstants.ShowSchedule,
                    ConsoleConstants.Back,
                    ConsoleConstants.Help
                ],
                ContentLines = lines,
                Header = header,
                Message = message,
                Title = "Playoff Picture"
            };
        }

        private static Coach GetHumanCoach(WorldState world) =>
            world.Coaches.Single(coach => coach.IsHuman);

        private static Division GetDivision(League league, Team team) =>
            league.Divisions.First(division => string.Equals(division.Id, team.DivisionId, StringComparison.Ordinal));

        private static LeagueStanding GetStanding(League league, string teamId) =>
            league.Standings.FirstOrDefault(standing => string.Equals(standing.TeamId, teamId, StringComparison.Ordinal))
            ?? new LeagueStanding
            {
                TeamId = teamId
            };

        private static IReadOnlyList<LeagueStanding> GetStandingsForDivision(League league, Division division)
        {
            HashSet<string> divisionTeamIds = division.TeamIds.ToHashSet(StringComparer.Ordinal);
            return league.Standings
                .Where(standing => divisionTeamIds.Contains(standing.TeamId))
                .OrderByDescending(standing => standing.MatchWins)
                .ThenByDescending(standing => standing.Points)
                .ThenBy(standing => FormatTeamName(league, standing.TeamId), StringComparer.Ordinal)
                .ToList();
        }

        private static ScheduledMatch? GetCurrentWeekHumanMatch(WorldState world, Team humanTeam) =>
            GetTeamLeague(world, humanTeam).Schedule
                .Where(match => match.Week == world.Season.CurrentWeek)
                .Where(match => IsTeamMatch(match, humanTeam.Id))
                .OrderBy(match => match.MatchType)
                .ThenBy(match => match.Id, StringComparer.Ordinal)
                .FirstOrDefault();

        private static ScheduledMatch? GetNextHumanMatch(WorldState world, Team humanTeam) =>
            GetTeamLeague(world, humanTeam).Schedule
                .Where(match => match.Week >= world.Season.CurrentWeek)
                .Where(match => match.Result is null)
                .Where(match => IsTeamMatch(match, humanTeam.Id))
                .OrderBy(match => match.Week)
                .ThenBy(match => match.MatchType)
                .ThenBy(match => match.Id, StringComparer.Ordinal)
                .FirstOrDefault();

        private static League GetTeamLeague(WorldState world, Team team) =>
            world.Tiers
                .SelectMany(tier => tier.Leagues)
                .Single(league => league.Id == team.LeagueId);

        private static bool IsTeamMatch(ScheduledMatch match, string teamId) =>
            string.Equals(match.HomeTeamId, teamId, StringComparison.Ordinal)
            || string.Equals(match.AwayTeamId, teamId, StringComparison.Ordinal);

        private static string FormatTeamName(WorldState world, string teamId) =>
            world.Tiers
                .SelectMany(tier => tier.Leagues)
                .SelectMany(league => league.Teams)
                .FirstOrDefault(team => team.Id == teamId)?.Name ?? teamId;

        private static string FormatTeamName(League league, string teamId) =>
            league.Teams.FirstOrDefault(team => team.Id == teamId)?.Name ?? teamId;

        private static string FormatLeagueName(League league) =>
            $"{league.TierName} {league.Region} League";

        private static string GetSeasonStatus(int week) =>
            week <= 23
                ? "Regular Season"
                : week switch
                {
                    24 => "League Quarterfinals",
                    25 => "League Semifinals",
                    26 => "League Finals",
                    27 => "World Championship Semifinals",
                    28 => "World Championship Final",
                    _ => "Offseason / Future Phase"
                };

        private static int GetPlayerRating(Player player) =>
            45 + (Math.Abs(StringComparer.Ordinal.GetHashCode(player.Name)) % 25);

        private static string GetPlayerTraits(Player player)
        {
            string[] traits = ["Clutch", "Steady", "Aggressive", "Disciplined", "Flexible"];
            return traits[Math.Abs(StringComparer.Ordinal.GetHashCode(player.Name + player.PositionRole)) % traits.Length];
        }

        private static string FormatMatch(WorldState world, ScheduledMatch? match) =>
            match is null
                ? "None scheduled"
                : $"Week {match.Week}: {FormatTeamName(world, match.HomeTeamId)} vs {FormatTeamName(world, match.AwayTeamId)}";

        private static string FormatMatchStatus(ScheduledMatch match) =>
            match.Result is null ? "Scheduled" : "Complete";

        private static string FormatPoints(int points) =>
            points >= 0 ? $"+{points}" : points.ToString();

        private static string FormatRecord(LeagueStanding standing) =>
            $"{standing.MatchWins}-{standing.MatchLosses}";

        private static string GetRecommendedAction(WorldState world, Team humanTeam) =>
            GetCurrentWeekHumanMatch(world, humanTeam) is null ? ConsoleConstants.ShowSchedule : ConsoleConstants.StartMatch;
    }
}
