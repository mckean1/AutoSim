using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;
using AutoSim.Domain.Services;

namespace AutoSim.Domain.Tests.Services
{
    internal sealed class CombatActionServiceTests
    {
        [Test]
        public void IsAbilityUseful_HealEffect_ReturnsTrueWhenAnyValidAllyIsInjured()
        {
            CombatEffect heal = CreateEffect(CombatEffectType.Heal, 100, TargetMode.AllyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("blue", abilityCombatEffects: [heal]);
            ChampionInstance injuredAlly = TestChampionFactory.CreateInstance("blue-ally");
            injuredAlly.TeamSide = TeamSide.Blue;
            injuredAlly.CurrentHealth = injuredAlly.MaximumHealth - 1;
            RoundState state = CreateState([source, injuredAlly], []);

            bool isUseful = CombatActionService.IsAbilityUseful(source, state, [source, injuredAlly]);

            Assert.That(isUseful, Is.True);
        }

        [Test]
        public void IsAbilityUseful_HealEffect_ReturnsFalseWhenAllValidAlliesAreFullHealth()
        {
            CombatEffect heal = CreateEffect(CombatEffectType.Heal, 100, TargetMode.AllyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("blue", abilityCombatEffects: [heal]);
            ChampionInstance ally = TestChampionFactory.CreateInstance("blue-ally");
            ally.TeamSide = TeamSide.Blue;
            RoundState state = CreateState([source, ally], []);

            bool isUseful = CombatActionService.IsAbilityUseful(source, state, [source, ally]);

            Assert.That(isUseful, Is.False);
        }

        [Test]
        public void IsAbilityUseful_DamageEffect_ReturnsTrueWhenAnyValidTargetExists()
        {
            CombatEffect damage = CreateEffect(CombatEffectType.Damage, 100, TargetMode.EnemyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("blue", abilityCombatEffects: [damage]);
            ChampionInstance enemy = TestChampionFactory.CreateInstance("red");
            RoundState state = CreateState([source], [enemy]);

            bool isUseful = CombatActionService.IsAbilityUseful(source, state, [source, enemy]);

            Assert.That(isUseful, Is.True);
        }

        [Test]
        public void IsAbilityUseful_ShieldEffect_ReturnsTrueWhenAnyValidTargetExists()
        {
            CombatEffect shield = CreateEffect(CombatEffectType.Shield, 100, TargetMode.AllyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("blue", abilityCombatEffects: [shield]);
            RoundState state = CreateState([source], []);

            bool isUseful = CombatActionService.IsAbilityUseful(source, state, [source]);

            Assert.That(isUseful, Is.True);
        }

        [Test]
        public void IsAbilityUseful_HybridAbility_ReturnsTrueWhenAnyEffectIsUseful()
        {
            CombatEffect heal = CreateEffect(CombatEffectType.Heal, 100, TargetMode.AllyAny, TargetScope.One);
            CombatEffect damage = CreateEffect(CombatEffectType.Damage, 100, TargetMode.EnemyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("blue", abilityCombatEffects: [heal, damage]);
            ChampionInstance enemy = TestChampionFactory.CreateInstance("red");
            RoundState state = CreateState([source], [enemy]);

            bool isUseful = CombatActionService.IsAbilityUseful(source, state, [source, enemy]);

            Assert.That(isUseful, Is.True);
        }

        [Test]
        public void IsAbilityUseful_TargetScopeOne_DoesNotConsumeRoundRandom()
        {
            CombatEffect heal = CreateEffect(CombatEffectType.Heal, 100, TargetMode.AllyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("blue", abilityCombatEffects: [heal]);
            ChampionInstance fullAlly = TestChampionFactory.CreateInstance("blue-full");
            ChampionInstance injuredAlly = TestChampionFactory.CreateInstance("blue-injured");
            fullAlly.TeamSide = TeamSide.Blue;
            injuredAlly.TeamSide = TeamSide.Blue;
            injuredAlly.CurrentHealth = injuredAlly.MaximumHealth - 1;
            CountingMatchRandom rng = new();
            RoundState state = new(
                new TeamRoundState(TeamSide.Blue, [source, fullAlly, injuredAlly]),
                new TeamRoundState(TeamSide.Red, []),
                rng,
                new RoundSettings());

            bool isUseful = CombatActionService.IsAbilityUseful(source, state, [source, fullAlly, injuredAlly]);

            Assert.Multiple(() =>
            {
                Assert.That(isUseful, Is.True);
                Assert.That(rng.NextCalls, Is.EqualTo(0));
                Assert.That(rng.NextDoubleCalls, Is.EqualTo(0));
            });
        }

        private static RoundState CreateState(
            IReadOnlyList<ChampionInstance> blueChampions,
            IReadOnlyList<ChampionInstance> redChampions) =>
            new(
                new TeamRoundState(TeamSide.Blue, blueChampions),
                new TeamRoundState(TeamSide.Red, redChampions),
                new CountingMatchRandom(),
                new RoundSettings());

        private static CombatEffect CreateEffect(
            CombatEffectType type,
            int value,
            TargetMode targetMode,
            TargetScope targetScope) =>
            TestChampionFactory.CreateEffect(type, value, targetMode, targetScope);
    }
}
