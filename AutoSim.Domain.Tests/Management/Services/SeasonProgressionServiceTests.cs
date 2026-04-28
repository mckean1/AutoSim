using AutoSim.Domain.Management.Interfaces;
using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Management.Services;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Tests.Management.Services
{
    internal sealed class SeasonProgressionServiceTests
    {
        [Test]
        public void ResolveCurrentWeek_WeekOne_ResolvesAllScheduledMatchesForWeek()
        {
            WorldState world = new WorldGenerationService().CreateWorld(seed: 123);
            CountingMatchEngineWrapper matchEngineWrapper = new();
            SeasonProgressionResult result = new SeasonProgressionService(matchEngineWrapper).ResolveCurrentWeek(world);

            Assert.Multiple(() =>
            {
                Assert.That(result.MatchResults, Has.Count.EqualTo(120));
                Assert.That(matchEngineWrapper.CallCount, Is.EqualTo(120));
                Assert.That(result.World.Season.CurrentWeek, Is.EqualTo(2));
            });
        }

        [Test]
        public void ResolveCurrentWeek_WeekOne_IncludesAiMatchesInStandingsUpdates()
        {
            WorldState world = new WorldGenerationService().CreateWorld(seed: 123);
            WorldState updatedWorld = new SeasonProgressionService(new CountingMatchEngineWrapper())
                .ResolveCurrentWeek(world)
                .World;

            IReadOnlyList<LeagueStanding> standings = updatedWorld.Tiers
                .SelectMany(tier => tier.Leagues)
                .SelectMany(league => league.Standings)
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(standings.Sum(standing => standing.MatchWins), Is.EqualTo(120));
                Assert.That(standings.Sum(standing => standing.MatchLosses), Is.EqualTo(120));
                Assert.That(standings.Count(standing => standing.MatchWins + standing.MatchLosses > 0), Is.EqualTo(240));
            });
        }

        private sealed class CountingMatchEngineWrapper : IMatchEngineWrapper
        {
            public int CallCount { get; private set; }

            public MatchResult Resolve(
                ScheduledMatch match,
                Team blueTeam,
                Team redTeam,
                Coach blueCoach,
                Coach redCoach,
                IReadOnlyList<Player> players,
                IReadOnlyList<ChampionDefinition> championCatalog,
                int seed)
            {
                _ = blueCoach;
                _ = redCoach;
                _ = players;
                _ = championCatalog;
                _ = seed;
                CallCount++;
                return new MatchResult
                {
                    BestOf = match.BestOf,
                    BlueRoundWins = 2,
                    BlueTeamId = blueTeam.Id,
                    LosingTeamId = redTeam.Id,
                    MatchId = match.Id,
                    MatchType = match.MatchType,
                    RedRoundWins = 0,
                    RedTeamId = redTeam.Id,
                    RoundResults =
                    [
                        new AutoSim.Domain.Management.Models.RoundResult
                        {
                            BlueTeamId = blueTeam.Id,
                            LosingTeamId = redTeam.Id,
                            RedTeamId = redTeam.Id,
                            RoundNumber = 1,
                            WinningTeamId = blueTeam.Id
                        },
                        new AutoSim.Domain.Management.Models.RoundResult
                        {
                            BlueTeamId = blueTeam.Id,
                            LosingTeamId = redTeam.Id,
                            RedTeamId = redTeam.Id,
                            RoundNumber = 2,
                            WinningTeamId = blueTeam.Id
                        }
                    ],
                    WinningTeamId = blueTeam.Id
                };
            }
        }
    }
}
