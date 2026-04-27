using AutoSim.Domain.Enums;
using ConsoleApp.Objects;
using ConsoleApp.Services;

namespace ConsoleApp.Tests.Services
{
    internal sealed class RoundAnalysisRendererTests
    {
        [TestCase(0.0, "00:00.0")]
        [TestCase(4.3, "00:04.3")]
        [TestCase(65.8, "01:05.8")]
        [TestCase(300.0, "05:00.0")]
        public void FormatTime_Seconds_ReturnsMinuteSecondFormat(double seconds, string expected)
        {
            string result = RoundAnalysisRenderer.FormatTime(seconds);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Render_Analysis_IncludesMainSections()
        {
            RoundAnalysis analysis = CreateAnalysis();

            string output = new RoundAnalysisRenderer().Render("logs/rounds/test.jsonl", analysis);

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Round Analysis"));
                Assert.That(output, Does.Contain("Winner: Blue"));
                Assert.That(output, Does.Contain("Score: Blue 1 - Red 0"));
                Assert.That(output, Does.Contain("Fight Summary"));
                Assert.That(output, Does.Contain("Team Totals"));
                Assert.That(output, Does.Contain("Champion Performance"));
                Assert.That(output, Does.Contain("Notable"));
            });
        }

        [Test]
        public void Render_NoEvents_PrintsNoEventsFound()
        {
            RoundAnalysis analysis = CreateAnalysis() with { TotalEvents = 0 };

            string output = new RoundAnalysisRenderer().Render("logs/rounds/empty.jsonl", analysis);

            Assert.That(output, Does.Contain("No events found."));
        }

        private static RoundAnalysis CreateAnalysis() =>
            new()
            {
                TotalEvents = 10,
                DurationSeconds = 65.8,
                Winner = TeamSide.Blue,
                BlueKills = 1,
                RedKills = 0,
                FightSummary = new FightAnalysis
                {
                    TotalFights = 1,
                    AverageDurationSeconds = 3,
                    LongestFightSeconds = 3,
                    LongestFightLane = Lane.Top,
                    FightsByLane = new Dictionary<Lane, int>
                    {
                        [Lane.Top] = 1,
                        [Lane.Mid] = 0,
                        [Lane.Bottom] = 0
                    },
                    BlueFightWins = 1,
                    RedFightWins = 0,
                    FightsEndedByRoundEnd = 0
                },
                BlueTeam = new TeamAnalysis
                {
                    TeamSide = TeamSide.Blue,
                    Kills = 1,
                    Deaths = 0,
                    DamageDealt = 90,
                    HealingDone = 0,
                    ShieldingDone = 0,
                    Retreats = 0,
                    Escapes = 0,
                    Respawns = 0
                },
                RedTeam = new TeamAnalysis
                {
                    TeamSide = TeamSide.Red,
                    Kills = 0,
                    Deaths = 1,
                    DamageDealt = 0,
                    HealingDone = 0,
                    ShieldingDone = 0,
                    Retreats = 1,
                    Escapes = 1,
                    Respawns = 0
                },
                Champions =
                [
                    new ChampionAnalysis
                    {
                        ChampionId = "chain-mauler",
                        ChampionName = "Chain Mauler",
                        TeamSide = TeamSide.Blue,
                        Kills = 1,
                        DamageDealt = 90
                    }
                ],
                NotableEvents = ["Most damage: Blue Chain Mauler, 90."]
            };
    }
}
