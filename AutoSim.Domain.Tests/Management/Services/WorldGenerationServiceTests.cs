using AutoSim.Domain.Enums;
using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Management.Services;
using MatchType = AutoSim.Domain.Enums.MatchType;

namespace AutoSim.Domain.Tests.Management.Services
{
    internal sealed class WorldGenerationServiceTests
    {
        [Test]
        public void CreateWorld_NewGame_CreatesRequiredTierLeagueDivisionAndTeamCounts()
        {
            WorldState world = new WorldGenerationService().CreateWorld(seed: 123);
            IReadOnlyList<League> leagues = world.Tiers.SelectMany(tier => tier.Leagues).ToList();
            IReadOnlyList<Team> teams = leagues.SelectMany(league => league.Teams).ToList();
            IReadOnlyList<Division> divisions = leagues.SelectMany(league => league.Divisions).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(world.Tiers, Has.Count.EqualTo(3));
                Assert.That(leagues, Has.Count.EqualTo(12));
                Assert.That(teams, Has.Count.EqualTo(240));
                Assert.That(divisions, Has.Count.EqualTo(48));
                Assert.That(
                    world.Tiers.SelectMany(tier => tier.Leagues),
                    Is.All.Matches<League>(league => league.Teams.Count == 20));
                Assert.That(
                    world.Tiers.SelectMany(tier => tier.Leagues),
                    Is.All.Matches<League>(league => league.Divisions.Count == 4));
                Assert.That(
                    world.Tiers.SelectMany(tier => tier.Leagues).SelectMany(league => league.Divisions),
                    Is.All.Matches<Division>(division => division.TeamIds.Count == 5));
            });
        }

        [Test]
        public void CreateWorld_NewGame_PlacesHumanCoachInAmateurTier()
        {
            WorldState world = new WorldGenerationService().CreateWorld(seed: 123);
            Coach humanCoach = world.Coaches.Single(coach => coach.IsHuman);
            League humanLeague = world.Tiers
                .SelectMany(tier => tier.Leagues)
                .Single(league => league.Teams.Any(team => team.Id == humanCoach.TeamId));

            Assert.Multiple(() =>
            {
                Assert.That(humanCoach.Id, Is.EqualTo(world.HumanCoachId));
                Assert.That(humanLeague.TierName, Is.EqualTo(CompetitiveTierName.Amateur));
            });
        }

        [Test]
        public void CreateWorld_NewGame_ExposesIsHumanOnlyOnCoach()
        {
            Type[] managementTypes =
            [
                typeof(WorldState),
                typeof(SeasonState),
                typeof(CompetitiveTier),
                typeof(League),
                typeof(Division),
                typeof(Team),
                typeof(Coach),
                typeof(Player),
                typeof(ScheduledMatch),
                typeof(MatchResult),
                typeof(AutoSim.Domain.Management.Models.RoundResult),
                typeof(LeagueStanding),
                typeof(WorldChampionshipHistory)
            ];

            IReadOnlyList<Type> typesWithIsHuman = managementTypes
                .Where(type => type.GetProperty("IsHuman") is not null)
                .ToList();

            Assert.That(typesWithIsHuman, Is.EqualTo(new[] { typeof(Coach) }));
        }

        [Test]
        public void CreateWorld_NewGame_GeneratesTwentyThreeRegularSeasonMatchesPerTeam()
        {
            WorldState world = new WorldGenerationService().CreateWorld(seed: 123);
            League league = world.Tiers.First().Leagues.First();
            IReadOnlyList<ScheduledMatch> regularSeasonSchedule = league.Schedule
                .Where(match => match.MatchType == MatchType.RegularSeason)
                .ToList();

            IReadOnlyList<int> matchCounts = league.Teams
                .Select(team => regularSeasonSchedule.Count(
                    match => match.HomeTeamId == team.Id || match.AwayTeamId == team.Id))
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(matchCounts, Is.All.EqualTo(23));
                Assert.That(regularSeasonSchedule.Select(match => match.Week), Is.All.InRange(1, 23));
                Assert.That(regularSeasonSchedule, Has.Count.EqualTo(230));
                Assert.That(regularSeasonSchedule, Is.All.Matches<ScheduledMatch>(match => match.BestOf == 3));
            });
        }

        [Test]
        public void CreateWorld_NewGame_ReservesLeaguePlayoffWeeks()
        {
            WorldState world = new WorldGenerationService().CreateWorld(seed: 123);
            League league = world.Tiers.First().Leagues.First();

            Assert.Multiple(() =>
            {
                Assert.That(
                    league.Schedule.Count(match => match.MatchType == MatchType.LeagueQuarterfinal && match.Week == 24),
                    Is.EqualTo(4));
                Assert.That(
                    league.Schedule.Count(match => match.MatchType == MatchType.LeagueSemifinal && match.Week == 25),
                    Is.EqualTo(2));
                Assert.That(
                    league.Schedule.Count(match => match.MatchType == MatchType.LeagueFinal && match.Week == 26),
                    Is.EqualTo(1));
            });
        }

        [Test]
        public void CreateWorld_NewGame_ReservesWorldChampionshipWeeks()
        {
            WorldState world = new WorldGenerationService().CreateWorld(seed: 123);

            Assert.Multiple(() =>
            {
                Assert.That(
                    world.Season.WorldChampionshipSchedule.Count(match => match.Week == 27),
                    Is.EqualTo(2));
                Assert.That(
                    world.Season.WorldChampionshipSchedule.Count(match => match.Week == 28),
                    Is.EqualTo(1));
                Assert.That(
                    world.Season.WorldChampionshipSchedule.Single(match => match.Week == 28).BestOf,
                    Is.EqualTo(9));
            });
        }
    }
}
