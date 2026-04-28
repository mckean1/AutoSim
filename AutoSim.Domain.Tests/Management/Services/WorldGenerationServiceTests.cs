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
        public void CreateWorld_CustomHumanNames_UsesProvidedCoachAndTeamNames()
        {
            WorldState world = new WorldGenerationService().CreateWorld(
                seed: 123,
                humanCoachName: "Mina Vale",
                humanTeamName: "Signal Crown");
            Coach humanCoach = world.Coaches.Single(coach => coach.IsHuman);
            Team humanTeam = world.Tiers
                .SelectMany(tier => tier.Leagues)
                .SelectMany(league => league.Teams)
                .Single(team => team.Id == humanCoach.TeamId);

            Assert.Multiple(() =>
            {
                Assert.That(humanCoach.Name, Is.EqualTo("Mina Vale"));
                Assert.That(humanTeam.Name, Is.EqualTo("Signal Crown"));
            });
        }

        [Test]
        public void CreateWorld_NewGame_GeneratesUniqueCoachPlayerAndTeamNames()
        {
            WorldState world = new WorldGenerationService().CreateWorld(seed: 123);
            IReadOnlyList<string> personNames = world.Coaches
                .Select(coach => coach.Name)
                .Concat(world.Players.Select(player => player.Name))
                .ToList();
            IReadOnlyList<string> teamNames = world.Tiers
                .SelectMany(tier => tier.Leagues)
                .SelectMany(league => league.Teams)
                .Select(team => team.Name)
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(personNames, Is.Unique);
                Assert.That(teamNames, Is.Unique);
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
        public void CreateWorld_NewGame_EveryLeagueHasTwentyThreeRegularSeasonWeeks()
        {
            WorldState world = new WorldGenerationService().CreateWorld(seed: 123);
            IReadOnlyList<League> leagues = world.Tiers.SelectMany(tier => tier.Leagues).ToList();

            Assert.That(leagues, Is.All.Matches<League>(league =>
                GetRegularSeasonSchedule(league).Select(match => match.Week).Distinct().Count() == 23));
        }

        [Test]
        public void CreateWorld_NewGame_EveryTeamHasOneRegularSeasonMatchPerWeek()
        {
            WorldState world = new WorldGenerationService().CreateWorld(seed: 123);

            foreach (League league in world.Tiers.SelectMany(tier => tier.Leagues))
            {
                IReadOnlyList<ScheduledMatch> schedule = GetRegularSeasonSchedule(league);
                foreach (Team team in league.Teams)
                {
                    IReadOnlyList<int> weeklyCounts = Enumerable.Range(1, 23)
                        .Select(week => schedule.Count(match => match.Week == week
                            && (match.HomeTeamId == team.Id || match.AwayTeamId == team.Id)))
                        .ToList();

                    Assert.That(
                        weeklyCounts,
                        Is.All.EqualTo(1),
                        $"{league.Id} {team.Id} should play exactly once per regular-season week.");
                }
            }
        }

        [Test]
        public void CreateWorld_NewGame_EachDivisionOpponentIsPlayedTwice()
        {
            WorldState world = new WorldGenerationService().CreateWorld(seed: 123);

            foreach (League league in world.Tiers.SelectMany(tier => tier.Leagues))
            {
                IReadOnlyList<ScheduledMatch> schedule = GetRegularSeasonSchedule(league);
                foreach (Division division in league.Divisions)
                {
                    foreach (string teamId in division.TeamIds)
                    {
                        foreach (string opponentId in division.TeamIds.Where(opponentId => opponentId != teamId))
                        {
                            Assert.That(
                                CountGamesBetween(schedule, teamId, opponentId),
                                Is.EqualTo(2),
                                $"{teamId} should play division opponent {opponentId} twice.");
                        }
                    }
                }
            }
        }

        [Test]
        public void CreateWorld_NewGame_EachOutOfDivisionOpponentIsPlayedOnce()
        {
            WorldState world = new WorldGenerationService().CreateWorld(seed: 123);

            foreach (League league in world.Tiers.SelectMany(tier => tier.Leagues))
            {
                IReadOnlyList<ScheduledMatch> schedule = GetRegularSeasonSchedule(league);
                Dictionary<string, string> divisionByTeamId = league.Divisions
                    .SelectMany(division => division.TeamIds.Select(teamId => new { division.Id, TeamId = teamId }))
                    .ToDictionary(entry => entry.TeamId, entry => entry.Id, StringComparer.Ordinal);

                foreach (Team team in league.Teams)
                {
                    foreach (Team opponent in league.Teams.Where(opponent => opponent.Id != team.Id
                        && divisionByTeamId[opponent.Id] != divisionByTeamId[team.Id]))
                    {
                        Assert.That(
                            CountGamesBetween(schedule, team.Id, opponent.Id),
                            Is.EqualTo(1),
                            $"{team.Id} should play out-of-division opponent {opponent.Id} once.");
                    }
                }
            }
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

        private static int CountGamesBetween(
            IReadOnlyList<ScheduledMatch> schedule,
            string teamId,
            string opponentId) =>
            schedule.Count(match =>
                (match.HomeTeamId == teamId && match.AwayTeamId == opponentId)
                || (match.HomeTeamId == opponentId && match.AwayTeamId == teamId));

        private static IReadOnlyList<ScheduledMatch> GetRegularSeasonSchedule(League league) =>
            league.Schedule
                .Where(match => match.MatchType == MatchType.RegularSeason)
                .ToList();
    }
}
