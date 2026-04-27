using AutoSim.Domain.Enums;
using ConsoleApp.Objects;
using ConsoleApp.Services;

namespace ConsoleApp.Tests.Services
{
    internal sealed class AggregateRoundAnalyzerTests
    {
        [Test]
        public void Analyze_Rounds_AggregatesWinsKillsFightsAndLanes()
        {
            AggregateRoundAnalysis analysis = new AggregateRoundAnalyzer().Analyze(
                [CreateRound(TeamSide.Blue, 4, 2, 10, 4, 2), CreateRound(TeamSide.Red, 2, 3, 6, 2, 1)],
                totalLogsFound: 2);

            Assert.Multiple(() =>
            {
                Assert.That(analysis.BlueWins, Is.EqualTo(1));
                Assert.That(analysis.RedWins, Is.EqualTo(1));
                Assert.That(analysis.AverageBlueKills, Is.EqualTo(3));
                Assert.That(analysis.AverageRedKills, Is.EqualTo(2.5));
                Assert.That(analysis.AverageFightsPerRound, Is.EqualTo(8));
                Assert.That(analysis.AverageFightDurationSeconds, Is.EqualTo(3));
                Assert.That(analysis.AverageFightsByLane[Lane.Top], Is.EqualTo(3));
            });
        }

        [Test]
        public void Analyze_Rounds_AggregatesTeamAndChampionAverages()
        {
            AggregateRoundAnalysis analysis = new AggregateRoundAnalyzer().Analyze(
                [CreateRound(TeamSide.Blue, 1, 0, 1, 1, 0), CreateRound(null, 2, 1, 1, 1, 0)],
                totalLogsFound: 2);

            ChampionAggregateAnalysis champion = analysis.Champions.Single(item => item.ChampionId == "quickshot");

            Assert.Multiple(() =>
            {
                Assert.That(analysis.UnknownWinners, Is.EqualTo(1));
                Assert.That(analysis.BlueTeam.AverageDamageDealt, Is.EqualTo(100));
                Assert.That(analysis.RedTeam.AverageHealingDone, Is.EqualTo(25));
                Assert.That(champion.Games, Is.EqualTo(2));
                Assert.That(champion.Wins, Is.EqualTo(1));
                Assert.That(champion.WinRate, Is.EqualTo(50));
                Assert.That(champion.AverageKills, Is.EqualTo(1.5));
                Assert.That(champion.AverageDeaths, Is.EqualTo(0.5));
                Assert.That(champion.AverageDamageDealt, Is.EqualTo(100));
            });
        }

        [Test]
        public void Analyze_SkippedLogsAndEmptyInput_ReturnsGracefulAggregate()
        {
            AggregateRoundAnalysis analysis = new AggregateRoundAnalyzer().Analyze(
                [],
                totalLogsFound: 2,
                skippedLogs: ["bad.jsonl: Could not parse round log at line 1."]);

            Assert.Multiple(() =>
            {
                Assert.That(analysis.RoundsAnalyzed, Is.EqualTo(0));
                Assert.That(analysis.LogsSkipped, Is.EqualTo(1));
                Assert.That(analysis.BlueWins, Is.EqualTo(0));
                Assert.That(analysis.AverageBlueKills, Is.EqualTo(0));
            });
        }

        private static RoundAnalysis CreateRound(
            TeamSide? winner,
            int blueKills,
            int redKills,
            int fights,
            int topFights,
            int midFights) =>
            new()
            {
                TotalEvents = 100,
                DurationSeconds = 300,
                Winner = winner,
                BlueKills = blueKills,
                RedKills = redKills,
                FightSummary = new FightAnalysis
                {
                    TotalFights = fights,
                    AverageDurationSeconds = 3,
                    LongestFightSeconds = 8,
                    LongestFightLane = Lane.Bottom,
                    FightsByLane = new Dictionary<Lane, int>
                    {
                        [Lane.Top] = topFights,
                        [Lane.Mid] = midFights,
                        [Lane.Bottom] = fights - topFights - midFights
                    },
                    BlueFightWins = blueKills,
                    RedFightWins = redKills,
                    FightsEndedByRoundEnd = 1
                },
                BlueTeam = new TeamAnalysis
                {
                    TeamSide = TeamSide.Blue,
                    Kills = blueKills,
                    Deaths = redKills,
                    DamageDealt = 100,
                    HealingDone = 10,
                    ShieldingDone = 20,
                    Retreats = 3,
                    Escapes = 2,
                    Respawns = redKills
                },
                RedTeam = new TeamAnalysis
                {
                    TeamSide = TeamSide.Red,
                    Kills = redKills,
                    Deaths = blueKills,
                    DamageDealt = 80,
                    HealingDone = 25,
                    ShieldingDone = 30,
                    Retreats = 2,
                    Escapes = 1,
                    Respawns = blueKills
                },
                Champions =
                [
                    new ChampionAnalysis
                    {
                        ChampionId = "quickshot",
                        ChampionName = "Quickshot",
                        TeamSide = TeamSide.Blue,
                        Kills = blueKills,
                        Deaths = redKills,
                        DamageDealt = 100,
                        Retreats = 1,
                        Escapes = 1
                    }
                ],
                NotableEvents = []
            };
    }
}
