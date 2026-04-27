using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;
using ConsoleApp.Services;

namespace ConsoleApp.Tests.Services
{
    internal sealed class RoundSummaryRendererTests
    {
        [Test]
        public void Render_RoundResult_DisplaysWinnerScoreChampionSummariesAndLogPathWithoutEvents()
        {
            RoundResult result = new()
            {
                WinningSide = TeamSide.Blue,
                BlueKills = 2,
                RedKills = 1,
                BlueGold = 10,
                RedGold = 8,
                BlueExperience = 100,
                RedExperience = 80,
                Duration = 30,
                ChampionSummaries =
                [
                    CreateSummary("ironclad", "Ironclad", TeamSide.Blue, kills: 2, deaths: 0),
                    CreateSummary("pyromancer", "Pyromancer", TeamSide.Red, kills: 1, deaths: 2)
                ],
                Events =
                [
                    new RoundEvent
                    {
                        TimeSeconds = 1,
                        Type = RoundEventType.DamageDealt,
                        Message = "This detailed event should not be dumped."
                    }
                ]
            };

            string output = new RoundSummaryRenderer().Render("Blue Team", "Red Team", result, "logs/rounds/test.jsonl");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Winner: Blue"));
                Assert.That(output, Does.Contain("Final Score"));
                Assert.That(output, Does.Contain("Blue: 2 kills"));
                Assert.That(output, Does.Contain("Ironclad"));
                Assert.That(output, Does.Contain("logs/rounds/test.jsonl"));
                Assert.That(output, Does.Contain("Analyze with:"));
                Assert.That(output, Does.Contain("analyze round logs/rounds/test.jsonl"));
                Assert.That(output, Does.Not.Contain("This detailed event should not be dumped."));
            });
        }

        private static ChampionRoundSummary CreateSummary(
            string id,
            string name,
            TeamSide teamSide,
            int kills,
            int deaths) =>
            new()
            {
                ChampionId = id,
                ChampionName = name,
                TeamSide = teamSide,
                Lane = Lane.Top,
                Level = 2,
                Experience = teamSide == TeamSide.Blue ? 100 : 80,
                Gold = teamSide == TeamSide.Blue ? 10 : 8,
                Kills = kills,
                Deaths = deaths,
                FinalHealth = 50,
                MaximumHealth = 100
            };
    }
}
