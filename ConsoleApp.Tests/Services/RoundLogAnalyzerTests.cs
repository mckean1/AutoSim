using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;
using ConsoleApp.Objects;
using ConsoleApp.Services;

namespace ConsoleApp.Tests.Services
{
    internal sealed class RoundLogAnalyzerTests
    {
        [Test]
        public void Analyze_RoundEndedEvent_UsesRoundEndedDurationAndWinner()
        {
            RoundAnalysis analysis = Analyze(
                RoundEvent(RoundEventType.RoundStarted, 0),
                RoundEvent(RoundEventType.RoundEnded, 65.8, sourceSide: TeamSide.Red));

            Assert.Multiple(() =>
            {
                Assert.That(analysis.DurationSeconds, Is.EqualTo(65.8));
                Assert.That(analysis.Winner, Is.EqualTo(TeamSide.Red));
            });
        }

        [Test]
        public void Analyze_ChampionKilledEvents_CountsKillsAndDeathsByTeam()
        {
            RoundAnalysis analysis = Analyze(
                KillEvent(1, TeamSide.Blue, "quickshot", "Quickshot", TeamSide.Red, "stonejaw", "Stonejaw"),
                KillEvent(2, null, null, null, TeamSide.Blue, "quickshot", "Quickshot"));

            Assert.Multiple(() =>
            {
                Assert.That(analysis.BlueKills, Is.EqualTo(1));
                Assert.That(analysis.RedKills, Is.EqualTo(1));
                Assert.That(analysis.BlueTeam.Deaths, Is.EqualTo(1));
                Assert.That(analysis.RedTeam.Deaths, Is.EqualTo(1));
            });
        }

        [Test]
        public void Analyze_FightEvents_ComputesLaneCountsDurationsAndRoundEndCounts()
        {
            Guid topFight = Guid.NewGuid();
            Guid bottomFight = Guid.NewGuid();

            RoundAnalysis analysis = Analyze(
                FightEvent(RoundEventType.FightStarted, 1, topFight, Lane.Top),
                FightEvent(RoundEventType.FightEnded, 4, topFight, Lane.Top, TeamSide.Blue),
                FightEvent(RoundEventType.FightStarted, 2, bottomFight, Lane.Bottom),
                FightEvent(RoundEventType.FightEnded, 8, bottomFight, Lane.Bottom, message: "Fight ended because the round ended."));

            Assert.Multiple(() =>
            {
                Assert.That(analysis.FightSummary.TotalFights, Is.EqualTo(2));
                Assert.That(analysis.FightSummary.FightsByLane[Lane.Top], Is.EqualTo(1));
                Assert.That(analysis.FightSummary.FightsByLane[Lane.Bottom], Is.EqualTo(1));
                Assert.That(analysis.FightSummary.AverageDurationSeconds, Is.EqualTo(4.5));
                Assert.That(analysis.FightSummary.LongestFightSeconds, Is.EqualTo(6));
                Assert.That(analysis.FightSummary.BlueFightWins, Is.EqualTo(1));
                Assert.That(analysis.FightSummary.FightsEndedByRoundEnd, Is.EqualTo(1));
            });
        }

        [Test]
        public void Analyze_AmountEvents_SumsTeamTotals()
        {
            RoundAnalysis analysis = Analyze(
                AmountEvent(RoundEventType.DamageDealt, TeamSide.Blue, "chain-mauler", "Chain Mauler", 90),
                AmountEvent(RoundEventType.HealingDone, TeamSide.Blue, "lifewarden", "Lifewarden", 40),
                AmountEvent(RoundEventType.ShieldApplied, TeamSide.Red, "stonejaw", "Stonejaw", 35));

            Assert.Multiple(() =>
            {
                Assert.That(analysis.BlueTeam.DamageDealt, Is.EqualTo(90));
                Assert.That(analysis.BlueTeam.HealingDone, Is.EqualTo(40));
                Assert.That(analysis.RedTeam.ShieldingDone, Is.EqualTo(35));
            });
        }

        [Test]
        public void Analyze_ChampionEvents_GroupsChampionPerformance()
        {
            RoundAnalysis analysis = Analyze(
                AmountEvent(RoundEventType.DamageDealt, TeamSide.Blue, "chain-mauler", "Chain Mauler", 90),
                KillEvent(2, TeamSide.Blue, "chain-mauler", "Chain Mauler", TeamSide.Red, "stonejaw", "Stonejaw"),
                RoundEvent(RoundEventType.ChampionRetreated, 3, sourceSide: TeamSide.Blue, sourceId: "chain-mauler", sourceName: "Chain Mauler"),
                RoundEvent(RoundEventType.ChampionEscaped, 4, sourceSide: TeamSide.Blue, sourceId: "chain-mauler", sourceName: "Chain Mauler"),
                RoundEvent(RoundEventType.ChampionRespawned, 5, sourceSide: TeamSide.Red, sourceId: "stonejaw", sourceName: "Stonejaw"),
                RoundEvent(RoundEventType.ChampionLeveledUp, 6, sourceSide: TeamSide.Blue, sourceId: "chain-mauler", sourceName: "Chain Mauler"));

            ChampionAnalysis chainMauler = analysis.Champions.Single(champion => champion.ChampionId == "chain-mauler");
            ChampionAnalysis stonejaw = analysis.Champions.Single(champion => champion.ChampionId == "stonejaw");

            Assert.Multiple(() =>
            {
                Assert.That(chainMauler.Kills, Is.EqualTo(1));
                Assert.That(chainMauler.DamageDealt, Is.EqualTo(90));
                Assert.That(chainMauler.Retreats, Is.EqualTo(1));
                Assert.That(chainMauler.Escapes, Is.EqualTo(1));
                Assert.That(chainMauler.LevelsGained, Is.EqualTo(1));
                Assert.That(stonejaw.Deaths, Is.EqualTo(1));
                Assert.That(stonejaw.Respawns, Is.EqualTo(1));
            });
        }

        [Test]
        public void Analyze_NotableEvents_ProducesUsefulLines()
        {
            RoundAnalysis analysis = Analyze(
                AmountEvent(RoundEventType.DamageDealt, TeamSide.Blue, "chain-mauler", "Chain Mauler", 90),
                AmountEvent(RoundEventType.HealingDone, TeamSide.Red, "dawn-keeper", "Dawn Keeper", 80));

            Assert.Multiple(() =>
            {
                Assert.That(analysis.NotableEvents, Has.Some.Contains("Most damage"));
                Assert.That(analysis.NotableEvents, Has.Some.Contains("Most healing"));
            });
        }

        [Test]
        public void Analyze_MissingOptionalFields_HandlesGracefully()
        {
            RoundAnalysis analysis = Analyze(
                new RoundEvent
                {
                    TimeSeconds = 1,
                    Type = RoundEventType.DamageDealt,
                    Amount = 10,
                    Message = "Old event."
                });

            Assert.Multiple(() =>
            {
                Assert.That(analysis.TotalEvents, Is.EqualTo(1));
                Assert.That(analysis.BlueTeam.DamageDealt, Is.EqualTo(0));
                Assert.That(analysis.Champions, Is.Empty);
            });
        }

        private static RoundAnalysis Analyze(params RoundEvent[] events) =>
            new RoundLogAnalyzer().Analyze(events);

        private static RoundEvent RoundEvent(
            RoundEventType type,
            double time,
            TeamSide? sourceSide = null,
            string? sourceId = null,
            string? sourceName = null) =>
            new()
            {
                TimeSeconds = time,
                Type = type,
                SourceTeamSide = sourceSide?.ToString(),
                SourceChampionId = sourceId,
                SourceChampionName = sourceName,
                SourcePlayerId = sourceSide?.ToString().ToLowerInvariant(),
                Message = $"{type}."
            };

        private static RoundEvent KillEvent(
            double time,
            TeamSide? sourceSide,
            string? sourceId,
            string? sourceName,
            TeamSide targetSide,
            string targetId,
            string targetName) =>
            new()
            {
                TimeSeconds = time,
                Type = RoundEventType.ChampionKilled,
                SourceTeamSide = sourceSide?.ToString(),
                SourceChampionId = sourceId,
                SourceChampionName = sourceName,
                SourcePlayerId = sourceSide?.ToString().ToLowerInvariant(),
                TargetTeamSide = targetSide.ToString(),
                TargetChampionId = targetId,
                TargetChampionName = targetName,
                TargetPlayerId = targetSide.ToString().ToLowerInvariant(),
                Message = "Champion killed."
            };

        private static RoundEvent FightEvent(
            RoundEventType type,
            double time,
            Guid fightId,
            Lane lane,
            TeamSide? sourceSide = null,
            string? message = null) =>
            new()
            {
                TimeSeconds = time,
                Type = type,
                FightId = fightId,
                Lane = lane.ToString(),
                SourceTeamSide = sourceSide?.ToString(),
                Message = message ?? $"{type}."
            };

        private static RoundEvent AmountEvent(
            RoundEventType type,
            TeamSide sourceSide,
            string sourceId,
            string sourceName,
            int amount) =>
            new()
            {
                TimeSeconds = 1,
                Type = type,
                Amount = amount,
                SourceTeamSide = sourceSide.ToString(),
                SourceChampionId = sourceId,
                SourceChampionName = sourceName,
                SourcePlayerId = sourceSide.ToString().ToLowerInvariant(),
                Message = $"{type}."
            };
    }
}
