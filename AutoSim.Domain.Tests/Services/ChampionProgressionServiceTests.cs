using AutoSim.Domain.Objects;
using AutoSim.Domain.Services;

namespace AutoSim.Domain.Tests.Services
{
    internal sealed class ChampionProgressionServiceTests
    {
        [TestCase(1, 0)]
        [TestCase(2, 100)]
        [TestCase(3, 300)]
        [TestCase(4, 600)]
        [TestCase(5, 1000)]
        [TestCase(6, 1500)]
        [TestCase(7, 2100)]
        [TestCase(8, 2800)]
        [TestCase(9, 3600)]
        [TestCase(10, 4500)]
        public void GetTotalExperienceRequiredForLevel_Level_ReturnsCumulativeThreshold(
            int level,
            int expectedExperience)
        {
            Assert.That(
                ChampionProgressionService.GetTotalExperienceRequiredForLevel(level),
                Is.EqualTo(expectedExperience));
        }

        [TestCase(0, 1)]
        [TestCase(99, 1)]
        [TestCase(100, 2)]
        [TestCase(299, 2)]
        [TestCase(300, 3)]
        [TestCase(600, 4)]
        [TestCase(1500, 6)]
        [TestCase(4500, 10)]
        public void AddExperience_TotalExperience_AppliesCumulativeLevelThresholds(
            int experience,
            int expectedLevel)
        {
            ChampionProgressionService service = new(new RoundSettings());
            ChampionInstance champion = TestChampionFactory.CreateInstance(health: 100, power: 10);

            service.AddExperience(champion, experience);

            Assert.That(champion.Level, Is.EqualTo(expectedLevel));
        }

        [Test]
        public void AddExperience_LargeAward_GainsMultipleLevelsButDoesNotExceedMaxLevel()
        {
            ChampionProgressionService service = new(new RoundSettings());
            ChampionInstance champion = TestChampionFactory.CreateInstance(health: 100, power: 10);
            champion.CurrentHealth = 50;

            service.AddExperience(champion, 10000);

            Assert.Multiple(() =>
            {
                Assert.That(champion.Level, Is.EqualTo(10));
                Assert.That(champion.MaximumHealth, Is.EqualTo(190));
                Assert.That(champion.CurrentHealth, Is.EqualTo(140));
                Assert.That(champion.CurrentPower, Is.EqualTo(28));
            });
        }

        [Test]
        public void ApplyFarming_ThreeHundredSecondsAtDefaultRate_ReachesLevelSix()
        {
            ChampionProgressionService service = new(new RoundSettings());
            ChampionInstance champion = TestChampionFactory.CreateInstance();

            service.ApplyFarming([champion], 300.0);

            Assert.Multiple(() =>
            {
                Assert.That(champion.Experience, Is.EqualTo(1500));
                Assert.That(champion.Level, Is.EqualTo(6));
            });
        }
    }
}
