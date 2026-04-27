using AutoSim.Domain.Enums;
using AutoSim.Domain.Management.Interfaces;
using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Management.Services;
using AutoSim.Domain.Objects;
using MatchType = AutoSim.Domain.Enums.MatchType;
using CombatRoundResult = AutoSim.Domain.Objects.RoundResult;

namespace AutoSim.Domain.Tests.Management.Services
{
    internal sealed class MatchEngineWrapperTests
    {
        [TestCase(3, 2)]
        [TestCase(5, 3)]
        [TestCase(7, 4)]
        [TestCase(9, 5)]
        public void Resolve_BestOfMatch_StopsWhenTeamReachesRequiredWins(int bestOf, int expectedRounds)
        {
            CountingDraftService draftService = new();
            SequenceRoundEngine roundEngine = new(Enumerable.Repeat(TeamSide.Blue, expectedRounds).ToList());
            MatchEngineWrapper wrapper = new(draftService, new RoundDraftValidator(), roundEngine);

            MatchResult result = wrapper.Resolve(
                CreateMatch(bestOf),
                CreateTeam("blue"),
                CreateTeam("red"),
                CreateCoach("blue-coach", "blue"),
                CreateCoach("red-coach", "red"),
                [],
                ChampionCatalog.GetDefaultChampions(),
                seed: 123);

            Assert.Multiple(() =>
            {
                Assert.That(result.RoundResults, Has.Count.EqualTo(expectedRounds));
                Assert.That(result.BlueRoundWins, Is.EqualTo(expectedRounds));
                Assert.That(result.RedRoundWins, Is.Zero);
                Assert.That(draftService.CallCount, Is.EqualTo(expectedRounds));
                Assert.That(roundEngine.CallCount, Is.EqualTo(expectedRounds));
            });
        }

        [Test]
        public void Resolve_MixedRoundWinners_AggregatesRoundWinsAndWinner()
        {
            SequenceRoundEngine roundEngine = new([TeamSide.Red, TeamSide.Blue, TeamSide.Red]);
            MatchEngineWrapper wrapper = new(new CountingDraftService(), new RoundDraftValidator(), roundEngine);

            MatchResult result = wrapper.Resolve(
                CreateMatch(bestOf: 3),
                CreateTeam("blue"),
                CreateTeam("red"),
                CreateCoach("blue-coach", "blue"),
                CreateCoach("red-coach", "red"),
                [],
                ChampionCatalog.GetDefaultChampions(),
                seed: 123);

            Assert.Multiple(() =>
            {
                Assert.That(result.BlueRoundWins, Is.EqualTo(1));
                Assert.That(result.RedRoundWins, Is.EqualTo(2));
                Assert.That(result.WinningTeamId, Is.EqualTo("red"));
                Assert.That(result.LosingTeamId, Is.EqualTo("blue"));
                Assert.That(result.RoundResults.Select(round => round.WinningTeamId), Is.EqualTo(new[] { "red", "blue", "red" }));
            });
        }

        [Test]
        public void Resolve_ValidMatch_CallsDraftServiceOncePerRound()
        {
            CountingDraftService draftService = new();
            MatchEngineWrapper wrapper = new(
                draftService,
                new RoundDraftValidator(),
                new SequenceRoundEngine([TeamSide.Blue, TeamSide.Blue]));

            wrapper.Resolve(
                CreateMatch(bestOf: 3),
                CreateTeam("blue"),
                CreateTeam("red"),
                CreateCoach("blue-coach", "blue"),
                CreateCoach("red-coach", "red"),
                [],
                ChampionCatalog.GetDefaultChampions(),
                seed: 123);

            Assert.That(draftService.CallCount, Is.EqualTo(2));
        }

        [Test]
        public void Resolve_ValidMatch_CallsRoundEngineOncePerPlayedRound()
        {
            SequenceRoundEngine roundEngine = new([TeamSide.Blue, TeamSide.Blue]);
            MatchEngineWrapper wrapper = new(new CountingDraftService(), new RoundDraftValidator(), roundEngine);

            wrapper.Resolve(
                CreateMatch(bestOf: 3),
                CreateTeam("blue"),
                CreateTeam("red"),
                CreateCoach("blue-coach", "blue"),
                CreateCoach("red-coach", "red"),
                [],
                ChampionCatalog.GetDefaultChampions(),
                seed: 123);

            Assert.That(roundEngine.CallCount, Is.EqualTo(2));
        }

        [Test]
        public void Resolve_InvalidDraft_DoesNotCallRoundEngine()
        {
            SequenceRoundEngine roundEngine = new([TeamSide.Blue]);
            MatchEngineWrapper wrapper = new(new InvalidDraftService(), new RoundDraftValidator(), roundEngine);

            Assert.Throws<ArgumentException>(() => wrapper.Resolve(
                CreateMatch(bestOf: 3),
                CreateTeam("blue"),
                CreateTeam("red"),
                CreateCoach("blue-coach", "blue"),
                CreateCoach("red-coach", "red"),
                [],
                ChampionCatalog.GetDefaultChampions(),
                seed: 123));

            Assert.That(roundEngine.CallCount, Is.Zero);
        }


        private static ScheduledMatch CreateMatch(int bestOf) =>
            new()
            {
                AwayTeamId = "red",
                BestOf = bestOf,
                HomeTeamId = "blue",
                Id = $"match-best-of-{bestOf}",
                MatchType = MatchType.RegularSeason,
                Week = 1
            };

        private static Coach CreateCoach(string coachId, string teamId) =>
            new()
            {
                Id = coachId,
                Name = coachId,
                TeamId = teamId
            };

        private static Team CreateTeam(string teamId) =>
            new()
            {
                CoachId = $"{teamId}-coach",
                DivisionId = "division",
                Id = teamId,
                LeagueId = "league",
                Name = teamId
            };

        private sealed class CountingDraftService : IRoundDraftService
        {
            public int CallCount { get; private set; }

            public RoundDraft DraftRound(RoundDraftContext context)
            {
                CallCount++;
                IReadOnlyList<ChampionDefinition> catalog = context.ChampionCatalog;
                return new RoundDraft
                {
                    BlueChampions = catalog.Take(5).ToList(),
                    RedChampions = catalog.Skip(5).Take(5).ToList()
                };
            }
        }

        private sealed class SequenceRoundEngine : IRoundEngine
        {
            private readonly Queue<TeamSide> _winners;

            public SequenceRoundEngine(IReadOnlyList<TeamSide> winners)
            {
                _winners = new Queue<TeamSide>(winners);
            }

            public int CallCount { get; private set; }

            public CombatRoundResult Simulate(RoundSetup setup)
            {
                CallCount++;
                return new CombatRoundResult
                {
                    ActiveFightCount = 0,
                    BlueExperience = 0,
                    BlueGold = 0,
                    BlueKills = 0,
                    Duration = 1,
                    RedExperience = 0,
                    RedGold = 0,
                    RedKills = 0,
                    WinningSide = _winners.Dequeue()
                };
            }
        }

        private sealed class InvalidDraftService : IRoundDraftService
        {
            public RoundDraft DraftRound(RoundDraftContext context) =>
                new()
                {
                    BlueChampions = context.ChampionCatalog.Take(4).ToList(),
                    RedChampions = context.ChampionCatalog.Skip(5).Take(5).ToList()
                };
        }
    }
}
