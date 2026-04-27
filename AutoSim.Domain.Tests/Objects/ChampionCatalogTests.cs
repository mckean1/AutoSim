using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Tests.Objects
{
    [TestFixture]
    public sealed class ChampionCatalogTests
    {
        private static readonly string[] ExpectedChampionIds =
        [
            "iron-vanguard",
            "bloodguard",
            "chain-mauler",
            "stonejaw",
            "rift-breaker",
            "quickshot",
            "longshot",
            "volley-hawk",
            "glass-arrow",
            "crosswind",
            "ember-sage",
            "stormcaller",
            "frost-oracle",
            "starbinder",
            "rune-weaver",
            "lifewarden",
            "dawn-keeper",
            "bulwark-medic",
            "pact-seer",
            "field-cleric"
        ];

        private static readonly string[] PureSupportChampionIds =
        [
            "lifewarden",
            "dawn-keeper",
            "bulwark-medic",
            "field-cleric"
        ];

        [Test]
        public void GetDefaultChampions_WhenCalled_ReturnsExactlyTwentyChampions()
        {
            IReadOnlyList<ChampionDefinition> champions = ChampionCatalog.GetDefaultChampions();

            Assert.That(champions, Has.Count.EqualTo(20));
        }

        [Test]
        public void GetDefaultChampions_WhenCalled_ReturnsUniqueIds()
        {
            IReadOnlyList<ChampionDefinition> champions = ChampionCatalog.GetDefaultChampions();
            IReadOnlyList<string> uniqueIds = champions.Select(champion => champion.Id).Distinct().ToList();

            Assert.That(uniqueIds, Has.Count.EqualTo(champions.Count));
        }

        [Test]
        public void GetDefaultChampions_WhenCalled_ReturnsUniqueNames()
        {
            IReadOnlyList<ChampionDefinition> champions = ChampionCatalog.GetDefaultChampions();
            IReadOnlyList<string> uniqueNames = champions.Select(champion => champion.Name).Distinct().ToList();

            Assert.That(uniqueNames, Has.Count.EqualTo(champions.Count));
        }

        [Test]
        public void GetDefaultChampions_WhenCalled_ReturnsDescriptionsForEveryChampion()
        {
            IReadOnlyList<ChampionDefinition> champions = ChampionCatalog.GetDefaultChampions();

            Assert.That(champions, Is.All.Matches<ChampionDefinition>(
                champion => !string.IsNullOrWhiteSpace(champion.Description)));
        }

        [TestCase(ChampionRole.Fighter)]
        [TestCase(ChampionRole.Marksman)]
        [TestCase(ChampionRole.Mage)]
        [TestCase(ChampionRole.Support)]
        public void GetDefaultChampions_RoleRoster_ReturnsFiveChampions(ChampionRole role)
        {
            IReadOnlyList<ChampionDefinition> champions = ChampionCatalog.GetDefaultChampions();

            Assert.That(champions.Count(champion => champion.Role == role), Is.EqualTo(5));
        }

        [Test]
        public void GetDefaultChampions_WhenCalled_ReturnsValidCombatStats()
        {
            IReadOnlyList<ChampionDefinition> champions = ChampionCatalog.GetDefaultChampions();

            Assert.Multiple(() =>
            {
                Assert.That(champions, Is.All.Matches<ChampionDefinition>(champion => champion.Health > 0));
                Assert.That(champions, Is.All.Matches<ChampionDefinition>(champion => champion.AttackPower >= 0));
                Assert.That(champions, Is.All.Matches<ChampionDefinition>(champion => champion.AttackSpeed > 0));
            });
        }

        [Test]
        public void GetDefaultChampions_WhenCalled_ReturnsAttacksAndAbilitiesWithEffects()
        {
            IReadOnlyList<ChampionDefinition> champions = ChampionCatalog.GetDefaultChampions();

            Assert.Multiple(() =>
            {
                Assert.That(champions, Is.All.Matches<ChampionDefinition>(champion => champion.Attack is not null));
                Assert.That(champions, Is.All.Matches<ChampionDefinition>(champion => champion.Attack.Effects.Count > 0));
                Assert.That(champions, Is.All.Matches<ChampionDefinition>(champion => champion.Ability is not null));
                Assert.That(champions, Is.All.Matches<ChampionDefinition>(champion => champion.Ability.Effects.Count > 0));
            });
        }

        [Test]
        public void GetDefaultChampions_WhenCalled_ReturnsFiveIncrementPowerValues()
        {
            IReadOnlyList<ChampionDefinition> champions = ChampionCatalog.GetDefaultChampions();
            IReadOnlyList<int> abilityPowers = champions
                .SelectMany(champion => champion.Ability.Effects)
                .Select(effect => effect.AbilityPower)
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(champions.Select(champion => champion.Health), Is.All.Matches<int>(value => value % 5 == 0));
                Assert.That(
                    champions.Select(champion => champion.AttackPower),
                    Is.All.Matches<int>(value => value % 5 == 0));
                Assert.That(abilityPowers, Is.All.Matches<int>(value => value % 5 == 0));
            });
        }

        [Test]
        public void GetDefaultChampions_ShieldEffects_UseStandardDurationOrExistingDefault()
        {
            IReadOnlyList<ChampionDefinition> champions = ChampionCatalog.GetDefaultChampions();
            IReadOnlyList<double?> durations = champions
                .SelectMany(GetShieldDurations)
                .ToList();

            Assert.That(durations, Is.All.Matches<double?>(duration => duration is null || duration == 5.0));
        }

        [Test]
        public void GetDefaultChampions_PureSupportChampions_DoNotHaveDamageEffects()
        {
            IReadOnlyList<ChampionDefinition> champions = ChampionCatalog.GetDefaultChampions();
            IReadOnlyList<ChampionDefinition> pureSupports = champions
                .Where(champion => PureSupportChampionIds.Contains(champion.Id))
                .ToList();

            Assert.That(pureSupports, Is.All.Matches<ChampionDefinition>(
                champion => GetEffectTypes(champion).All(effectType => effectType != CombatEffectType.Damage)));
        }

        [Test]
        public void GetDefaultChampions_WhenCalled_IncludesExpectedChampionIds()
        {
            IReadOnlyList<ChampionDefinition> champions = ChampionCatalog.GetDefaultChampions();

            Assert.That(champions.Select(champion => champion.Id), Is.EquivalentTo(ExpectedChampionIds));
        }

        [Test]
        public void GetDefaultChampions_FirstBalancePass_ReturnsExpectedAdjustedValues()
        {
            IReadOnlyList<ChampionDefinition> champions = ChampionCatalog.GetDefaultChampions();
            ChampionDefinition emberSage = GetChampion(champions, "ember-sage");
            ChampionDefinition stormcaller = GetChampion(champions, "stormcaller");
            ChampionDefinition glassArrow = GetChampion(champions, "glass-arrow");
            ChampionDefinition runeWeaver = GetChampion(champions, "rune-weaver");

            Assert.Multiple(() =>
            {
                Assert.That(GetAbilityEffect(emberSage, CombatEffectType.Damage).AbilityPower, Is.EqualTo(45));
                Assert.That(emberSage.Ability.Cooldown, Is.EqualTo(9.0));
                Assert.That(stormcaller.Health, Is.EqualTo(95));
                Assert.That(GetAbilityEffect(stormcaller, CombatEffectType.Damage).AbilityPower, Is.EqualTo(40));
                Assert.That(stormcaller.Ability.Cooldown, Is.EqualTo(9.0));
                Assert.That(glassArrow.Health, Is.EqualTo(90));
                Assert.That(glassArrow.Ability.CastTime, Is.EqualTo(1.00));
                Assert.That(runeWeaver.Health, Is.EqualTo(110));
                Assert.That(GetAbilityEffect(runeWeaver, CombatEffectType.Shield).AbilityPower, Is.EqualTo(30));
            });
        }

        private static IEnumerable<CombatEffectType> GetEffectTypes(ChampionDefinition champion)
        {
            foreach (AttackEffect effect in champion.Attack.Effects)
            {
                yield return effect.Type;
            }

            foreach (AbilityEffect effect in champion.Ability.Effects)
            {
                yield return effect.Type;
            }
        }

        private static IEnumerable<double?> GetShieldDurations(ChampionDefinition champion)
        {
            foreach (AttackEffect effect in champion.Attack.Effects.Where(effect => effect.Type == CombatEffectType.Shield))
            {
                yield return effect.Duration;
            }

            foreach (AbilityEffect effect in champion.Ability.Effects.Where(effect => effect.Type == CombatEffectType.Shield))
            {
                yield return effect.Duration;
            }
        }

        private static ChampionDefinition GetChampion(IReadOnlyList<ChampionDefinition> champions, string id) =>
            champions.Single(champion => string.Equals(champion.Id, id, StringComparison.Ordinal));

        private static AbilityEffect GetAbilityEffect(ChampionDefinition champion, CombatEffectType type) =>
            champion.Ability.Effects.Single(effect => effect.Type == type);
    }
}
