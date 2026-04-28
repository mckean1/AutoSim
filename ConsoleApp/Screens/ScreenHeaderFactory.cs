using AutoSim.Domain.Management.Models;

namespace ConsoleApp.Screens
{
    /// <summary>
    /// Builds screen headers for console screens.
    /// </summary>
    internal sealed class ScreenHeaderFactory
    {
        public ScreenHeaderModel BuildHeader(WorldState world, Team humanTeam, League league)
        {
            ArgumentNullException.ThrowIfNull(world);
            ArgumentNullException.ThrowIfNull(humanTeam);
            ArgumentNullException.ThrowIfNull(league);

            LeagueStanding standing = GetStanding(league, humanTeam.Id);
            return new ScreenHeaderModel
            {
                PrimaryLeft = "AutoSim",
                PrimaryRight = $"Week {world.Season.CurrentWeek} | {FormatLeagueName(league)}",
                SecondaryLeft = humanTeam.Name,
                SecondaryRight = $"Record {standing.MatchWins}-{standing.MatchLosses} | Points {FormatPoints(standing.Points)}"
            };
        }

        public ScreenHeaderModel BuildChampionHeader(WorldState? world)
        {
            if (world is null)
            {
                return new ScreenHeaderModel
                {
                    PrimaryLeft = "AutoSim",
                    PrimaryRight = "Champion Reference"
                };
            }

            Team humanTeam = GetHumanTeam(world);
            League league = GetTeamLeague(world, humanTeam);
            return BuildHeader(world, humanTeam, league);
        }

        private static Team GetHumanTeam(WorldState world)
        {
            Coach coach = world.Coaches.Single(currentCoach => currentCoach.IsHuman);
            return world.Tiers
                .SelectMany(tier => tier.Leagues)
                .SelectMany(league => league.Teams)
                .Single(team => team.Id == coach.TeamId);
        }

        private static League GetTeamLeague(WorldState world, Team team) =>
            world.Tiers
                .SelectMany(tier => tier.Leagues)
                .Single(league => league.Id == team.LeagueId);

        private static LeagueStanding GetStanding(League league, string teamId) =>
            league.Standings.FirstOrDefault(standing => string.Equals(standing.TeamId, teamId, StringComparison.Ordinal))
            ?? new LeagueStanding
            {
                TeamId = teamId
            };

        private static string FormatLeagueName(League league) =>
            $"{league.TierName} {league.Region} League";

        private static string FormatPoints(int points) =>
            points >= 0 ? $"+{points}" : points.ToString();
    }
}
